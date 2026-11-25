using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Rules;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Applies the records of a GET_MATERIAL pull (target fields Material.*) to the material master: unknown codes become
    /// new materials, known ones get their description and group updated, and a price that differs from the approved one
    /// becomes a price change request (approval) — the approved price is never overwritten. The base unit of an existing
    /// material is not changed (stock is kept in it). When the SAP Product payload includes to_Valuation /
    /// to_ProductUnitsOfMeasure, plant valuation (S→StandardPrice, V→MovingAveragePrice ÷ PriceUnitQty), UOM conversions
    /// and barcodes are applied. The caller saves.
    /// </summary>
    public static class SilaMaterialErpSync
    {
        public const string SOURCE_ERP = "ERP";
        private const int MAX_CODE_LENGTH = 40;
        private const int MAX_FAILURES = 200;

        public static async Task ApplyAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid userId,
            string companyCode,
            List<ApiFieldMapping> mappings,
            List<JsonElement> records,
            SilaMaterialErpPullResultDto result,
            CancellationToken cancellationToken)
        {
            string? plantOrCompany = string.IsNullOrWhiteSpace(companyCode) || companyCode.Trim().ToUpperInvariant() == "ALL"
                ? null
                : companyCode.Trim();

            List<(JsonElement Record, string Code)> prepared = new List<(JsonElement, string)>();
            int rawIndex = 0;
            foreach (JsonElement record in records)
            {
                rawIndex++;
                string code = ReadCode(mappings, record);
                if (code.Length == 0 || code.Length > MAX_CODE_LENGTH)
                {
                    result.Read++;
                    Fail(result, $"Record {rawIndex}: the material code is missing or longer than {MAX_CODE_LENGTH} characters.");
                    continue;
                }

                // Plant extract: keep only products with a valuation area for this plant/company.
                if (plantOrCompany != null && !HasValuationForArea(record, plantOrCompany))
                {
                    continue;
                }

                prepared.Add((record, code));
            }

            List<string> codes = prepared.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Dictionary<string, ItemBuyerMaster> materials = new Dictionary<string, ItemBuyerMaster>(StringComparer.OrdinalIgnoreCase);
            foreach (ItemBuyerMaster material in await repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && codes.Contains(x.MaterialCode))
                .ToListAsync(cancellationToken))
            {
                materials.TryAdd(material.MaterialCode.Trim(), material);
            }

            Dictionary<Guid, MaterialPriceChange> pending = await SilaMaterialRules.GetPendingChangesAsync(
                repository, buyerId, materials.Values.Select(x => x.Id), cancellationToken);
            ApprovalScope scope = await SilaMaterialPricing.GetScopeAsync(repository, buyerId, cancellationToken);
            bool flowExists = await FlowExistsAsync(repository, logger, buyerId, scope, cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> existingConversions = await LoadConversionsAsync(
                repository, materials.Values.Select(x => x.Id), cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> noConversions = new Dictionary<Guid, List<MaterialUomConversion>>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string reason = $"ERP material pull, company code {companyCode}";
            string? syncedCompanyCode = plantOrCompany;

            foreach ((JsonElement record, string code) in prepared)
            {
                result.Read++;
                if (!seen.Add(code))
                {
                    Fail(result, $"{code}: appears more than once in the ERP response; only the first record was used.");
                    continue;
                }

                string? description = Clean(IntegrationRecordReader.Read(mappings, "Material.Description", record), 500)
                    ?? Clean(ReadProductDescription(record), 500);
                string? baseUom = Clean(IntegrationRecordReader.Read(mappings, "Material.BaseUom", record), 20)?.ToUpperInvariant()
                    ?? Clean(ReadString(record, "BaseUnit"), 20)?.ToUpperInvariant();
                string? group = Clean(IntegrationRecordReader.Read(mappings, "Material.MaterialGroup", record), 100)
                    ?? Clean(ReadString(record, "ProductGroup"), 100);
                string? productType = Clean(ReadString(record, "ProductType"), 40);

                string? priceText = IntegrationRecordReader.Read(mappings, "Material.UnitPrice", record);
                decimal? price = IntegrationRecordReader.ParseDecimal(priceText);
                string? currency = Clean(IntegrationRecordReader.Read(mappings, "Material.Currency", record), 3)?.ToUpperInvariant();
                decimal? standardPrice = null;
                decimal? movingAveragePrice = null;
                string? valuationClass = null;

                if (TryReadPlantValuation(record, plantOrCompany, out ValuationPrice valuation))
                {
                    standardPrice = valuation.StandardPrice;
                    movingAveragePrice = valuation.MovingAveragePrice;
                    valuationClass = valuation.ValuationClass;
                    currency ??= valuation.Currency;
                    // S → StandardPrice; V → MovingAveragePrice; divide by PriceUnitQty for per-1 price.
                    decimal? raw = valuation.Procedure == "S" ? valuation.StandardPrice
                        : valuation.Procedure == "V" ? valuation.MovingAveragePrice
                        : valuation.StandardPrice ?? valuation.MovingAveragePrice;
                    if (raw != null)
                    {
                        decimal divisor = valuation.PriceUnitQty is > 0 ? valuation.PriceUnitQty.Value : 1m;
                        price = raw.Value / divisor;
                    }
                }

                if (!string.IsNullOrWhiteSpace(priceText) && price == null && !HasExpandedValuation(record))
                {
                    Fail(result, $"{code}: the unit price '{priceText}' is not a number.");
                    continue;
                }

                string? barcode = ReadPreferredBarcode(record, baseUom);

                bool isNew = !materials.TryGetValue(code, out ItemBuyerMaster? material);
                bool changed = false;
                if (material == null)
                {
                    if (string.IsNullOrEmpty(baseUom))
                    {
                        Fail(result, $"{code}: the base unit of measure is missing; a new material needs one.");
                        continue;
                    }

                    material = NewMaterial(buyerId, code, description, baseUom, group, currency);
                    material.CompanyCode = syncedCompanyCode;
                    material.ProductType = productType ?? string.Empty;
                    material.ValuationClass = valuationClass ?? string.Empty;
                    material.StandardPrice = standardPrice;
                    material.MovingAveragePrice = movingAveragePrice;
                    material.Barcode = barcode;
                    if (price != null)
                    {
                        material.UnitCost = price;
                    }

                    repository.ItemBuyerMaster.Create(material);
                    materials[code] = material;
                    existingConversions[material.Id] = new List<MaterialUomConversion>();
                }
                else
                {
                    changed = Update(material, description, group);
                    if (syncedCompanyCode != null && material.CompanyCode != syncedCompanyCode)
                    {
                        material.CompanyCode = syncedCompanyCode;
                        changed = true;
                    }

                    if (productType != null && productType != material.ProductType)
                    {
                        material.ProductType = productType;
                        changed = true;
                    }

                    if (valuationClass != null && valuationClass != material.ValuationClass)
                    {
                        material.ValuationClass = valuationClass;
                        changed = true;
                    }

                    if (standardPrice != null && material.StandardPrice != standardPrice)
                    {
                        material.StandardPrice = standardPrice;
                        changed = true;
                    }

                    if (movingAveragePrice != null && material.MovingAveragePrice != movingAveragePrice)
                    {
                        material.MovingAveragePrice = movingAveragePrice;
                        changed = true;
                    }

                    if (barcode != null && barcode != material.Barcode)
                    {
                        material.Barcode = barcode;
                        changed = true;
                    }

                    if (currency != null && currency != material.Currency)
                    {
                        material.Currency = currency;
                        changed = true;
                    }

                    if (changed)
                    {
                        repository.ItemBuyerMaster.Update(material);
                    }
                }

                bool conversionsChanged = await ApplyConversionsAsync(
                    repository, buyerId, material, baseUom ?? material.BaseUnitOfMeasure, record,
                    existingConversions.TryGetValue(material.Id, out List<MaterialUomConversion>? list) ? list : new List<MaterialUomConversion>(),
                    cancellationToken);
                changed = changed || conversionsChanged;

                bool priceRequested = false;
                // Zero / missing ERP prices are stored on StandardPrice / MovingAveragePrice only; UnitCost needs a positive amount.
                if (price != null && price.Value > 0 && price.Value != material.UnitCost)
                {
                    if (pending.ContainsKey(material.Id))
                    {
                        Note(result, $"{code}: ERP price {price} not requested, a price change is already waiting for approval.");
                    }
                    else if (!flowExists)
                    {
                        // No approval flow: write the ERP unit price onto the master (plant extract).
                        material.UnitCost = price;
                        repository.ItemBuyerMaster.Update(material);
                        changed = true;
                    }
                    else
                    {
                        List<string> errors = SilaMaterialPricing.Validate(
                            logger, material, noConversions, price.Value, currency, null, reason, out _, out _);
                        if (errors.Count > 0)
                        {
                            Note(result, $"{code}: ERP price not requested. {string.Join(" ", errors)}");
                        }
                        else
                        {
                            await SilaMaterialPricing.CreateAsync(
                                repository, logger, buyerId, material, noConversions, price.Value, currency, null, null, reason, userId, scope, cancellationToken);
                            priceRequested = true;
                            result.PriceChanges++;
                        }
                    }
                }
                else if (price != null && price.Value > 0 && price.Value == material.UnitCost)
                {
                    // already aligned
                }
                else if (price != null && price.Value <= 0 && material.UnitCost != price)
                {
                    // Persist zero unit cost from ERP when master has none yet (valuation fields already saved above).
                    if (material.UnitCost == null)
                    {
                        material.UnitCost = price;
                        repository.ItemBuyerMaster.Update(material);
                        changed = true;
                    }
                }

                CountOutcome(result, isNew, changed || priceRequested);
            }
        }

        private static string ReadCode(List<ApiFieldMapping> mappings, JsonElement record)
        {
            string? mapped = IntegrationRecordReader.Read(mappings, "Material.Code", record)?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(mapped))
            {
                return mapped;
            }

            return Clean(ReadString(record, "Product"), MAX_CODE_LENGTH)?.ToUpperInvariant() ?? string.Empty;
        }

        private static async Task<Dictionary<Guid, List<MaterialUomConversion>>> LoadConversionsAsync(
            IRepositoryWrapper repository, IEnumerable<Guid> materialIds, CancellationToken cancellationToken)
        {
            List<Guid> ids = materialIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<Guid, List<MaterialUomConversion>>();
            }

            List<MaterialUomConversion> rows = await repository.MaterialUomConversion
                .FindByCondition(x => ids.Contains(x.MaterialId) && x.IsActive)
                .ToListAsync(cancellationToken);
            return rows.GroupBy(x => x.MaterialId).ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// AlternativeUnit × QuantityDenominator = BaseUnit × QuantityNumerator
        /// → 1 AlternativeUnit = (QuantityNumerator / QuantityDenominator) BaseUnit.
        /// </summary>
        private static async Task<bool> ApplyConversionsAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            ItemBuyerMaster material,
            string baseUom,
            JsonElement record,
            List<MaterialUomConversion> existing,
            CancellationToken cancellationToken)
        {
            if (!TryGetResults(record, "to_ProductUnitsOfMeasure", out List<JsonElement> units) || units.Count == 0)
            {
                return false;
            }

            bool changed = false;
            string baseNorm = baseUom.Trim().ToUpperInvariant();
            foreach (JsonElement unit in units)
            {
                string? alt = Clean(ReadString(unit, "AlternativeUnit"), 20)?.ToUpperInvariant();
                if (string.IsNullOrEmpty(alt) || string.Equals(alt, baseNorm, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                decimal? numerator = ReadDecimal(unit, "QuantityNumerator");
                decimal? denominator = ReadDecimal(unit, "QuantityDenominator");
                if (numerator is null or <= 0 || denominator is null or <= 0)
                {
                    continue;
                }

                decimal factor = numerator.Value / denominator.Value;
                MaterialUomConversion? row = existing.FirstOrDefault(x =>
                    string.Equals(x.FromUom, alt, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.ToUom, baseNorm, StringComparison.OrdinalIgnoreCase));
                if (row == null)
                {
                    row = new MaterialUomConversion
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        MaterialId = material.Id,
                        FromUom = alt,
                        ToUom = baseNorm,
                        Factor = factor,
                        IsActive = true
                    };
                    repository.MaterialUomConversion.Create(row);
                    existing.Add(row);
                    changed = true;
                }
                else if (row.Factor != factor)
                {
                    row.Factor = factor;
                    repository.MaterialUomConversion.Update(row);
                    changed = true;
                }
            }

            await Task.CompletedTask;
            return changed;
        }

        private static bool HasExpandedValuation(JsonElement record) => TryGetResults(record, "to_Valuation", out _);

        private static bool HasValuationForArea(JsonElement record, string area)
        {
            if (!TryGetResults(record, "to_Valuation", out List<JsonElement> rows))
            {
                return false;
            }

            return rows.Any(row => string.Equals(ReadString(row, "ValuationArea"), area, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryReadPlantValuation(JsonElement record, string? area, out ValuationPrice valuation)
        {
            valuation = default;
            if (!TryGetResults(record, "to_Valuation", out List<JsonElement> rows) || rows.Count == 0)
            {
                return false;
            }

            JsonElement chosen = rows[0];
            if (!string.IsNullOrWhiteSpace(area))
            {
                int index = rows.FindIndex(row =>
                    string.Equals(ReadString(row, "ValuationArea"), area, StringComparison.OrdinalIgnoreCase));
                if (index < 0)
                {
                    return false;
                }

                chosen = rows[index];
            }

            string? procedure = Clean(ReadString(chosen, "InventoryValuationProcedure"), 10)?.ToUpperInvariant();
            valuation = new ValuationPrice
            {
                Procedure = procedure ?? string.Empty,
                StandardPrice = ReadDecimal(chosen, "StandardPrice"),
                MovingAveragePrice = ReadDecimal(chosen, "MovingAveragePrice"),
                PriceUnitQty = ReadDecimal(chosen, "PriceUnitQty"),
                Currency = Clean(ReadString(chosen, "Currency"), 3)?.ToUpperInvariant(),
                ValuationClass = Clean(ReadString(chosen, "ValuationClass"), 40)
            };
            return true;
        }

        private static string? ReadProductDescription(JsonElement record)
        {
            if (!TryGetResults(record, "to_Description", out List<JsonElement> rows) || rows.Count == 0)
            {
                return null;
            }

            int enIndex = rows.FindIndex(row =>
                string.Equals(ReadString(row, "Language"), "EN", StringComparison.OrdinalIgnoreCase));
            return ReadString(enIndex >= 0 ? rows[enIndex] : rows[0], "ProductDescription");
        }

        private static string? ReadPreferredBarcode(JsonElement record, string? baseUom)
        {
            if (!TryGetResults(record, "to_ProductUnitsOfMeasure", out List<JsonElement> units))
            {
                return null;
            }

            string? baseNorm = baseUom?.Trim().ToUpperInvariant();
            string? fallback = null;
            foreach (JsonElement unit in units)
            {
                string? alt = Clean(ReadString(unit, "AlternativeUnit"), 20)?.ToUpperInvariant();
                if (!TryGetResults(unit, "to_InternationalArticleNumber", out List<JsonElement> numbers))
                {
                    continue;
                }

                foreach (JsonElement number in numbers)
                {
                    string? ean = Clean(ReadString(number, "InternationalArticleNumber"), 40);
                    if (string.IsNullOrEmpty(ean))
                    {
                        continue;
                    }

                    if (baseNorm != null && string.Equals(alt, baseNorm, StringComparison.OrdinalIgnoreCase))
                    {
                        return ean;
                    }

                    fallback ??= ean;
                }
            }

            return fallback;
        }

        private static bool TryGetResults(JsonElement parent, string property, out List<JsonElement> results)
        {
            results = new List<JsonElement>();
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(property, out JsonElement node))
            {
                return false;
            }

            if (node.ValueKind == JsonValueKind.Array)
            {
                results = node.EnumerateArray().Select(x => x.Clone()).ToList();
                return results.Count > 0;
            }

            if (node.ValueKind == JsonValueKind.Object
                && node.TryGetProperty("results", out JsonElement nested)
                && nested.ValueKind == JsonValueKind.Array)
            {
                results = nested.EnumerateArray().Select(x => x.Clone()).ToList();
                return results.Count > 0;
            }

            return false;
        }

        private static string? ReadString(JsonElement element, string property)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out JsonElement value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static decimal? ReadDecimal(JsonElement element, string property)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out JsonElement value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                return decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)
                    ? parsed
                    : null;
            }

            return null;
        }

        private static async Task<bool> FlowExistsAsync(
            IRepositoryWrapper repository, ILoggerManager logger, Guid buyerId, ApprovalScope scope, CancellationToken cancellationToken)
        {
            try
            {
                _ = await SilaApprovals.ResolveFlowAsync(repository, logger, buyerId, Common.SILA_APPROVAL_TYPE_MATERIAL_PRICE, scope, cancellationToken);
                return true;
            }
            catch (BadRequestCustomException)
            {
                return false;
            }
        }

        private static ItemBuyerMaster NewMaterial(Guid buyerId, string code, string? description, string baseUom, string? group, string? currency)
        {
            return new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialCode = code,
                Description = description ?? code,
                MaterialGroup = group ?? string.Empty,
                ProductType = string.Empty,
                BaseUnitOfMeasure = baseUom,
                OrderUnitOfMeasure = baseUom,
                AlternateUnitOfMeasure = string.Empty,
                ValuationClass = string.Empty,
                UnitOfMeasureMapping = string.Empty,
                Currency = currency,
                Source = SOURCE_ERP,
                IsActive = true
            };
        }

        private static bool Update(ItemBuyerMaster material, string? description, string? group)
        {
            bool changed = false;
            if (description != null && description != material.Description)
            {
                material.Description = description;
                changed = true;
            }

            if (group != null && group != material.MaterialGroup)
            {
                material.MaterialGroup = group;
                changed = true;
            }

            return changed;
        }

        private static void CountOutcome(SilaMaterialErpPullResultDto result, bool isNew, bool changed)
        {
            if (isNew)
            {
                result.New++;
            }
            else if (changed)
            {
                result.Changed++;
            }
            else
            {
                result.Unchanged++;
            }
        }

        private static void Fail(SilaMaterialErpPullResultDto result, string message)
        {
            result.Failed++;
            Note(result, message);
        }

        /// <summary>A message about a record that was still applied (e.g. its price was not requested).</summary>
        private static void Note(SilaMaterialErpPullResultDto result, string message)
        {
            if (result.Failures.Count < MAX_FAILURES)
            {
                result.Failures.Add(message);
            }
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
        }

        private readonly struct ValuationPrice
        {
            public string Procedure { get; init; }
            public decimal? StandardPrice { get; init; }
            public decimal? MovingAveragePrice { get; init; }
            public decimal? PriceUnitQty { get; init; }
            public string? Currency { get; init; }
            public string? ValuationClass { get; init; }
        }
    }
}
