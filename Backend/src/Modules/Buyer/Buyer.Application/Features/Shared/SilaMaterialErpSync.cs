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
    /// Applies GET_MATERIAL pull records to Material Master (header upsert), optional plant/SLoc assignments,
    /// material-specific UOM conversions, and valuation fields. Never overwrites approved UnitCost (price-change request).
    /// Never mutates InventoryBalance / InventoryLedger. Never deletes materials missing from a partial pull.
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
            CancellationToken cancellationToken,
            IReadOnlyCollection<string>? plantFilter = null,
            IReadOnlyDictionary<string, string>? descriptionsByProduct = null)
        {
            HashSet<string>? plants = NormalizePlantFilter(plantFilter);
            List<JsonElement> scoped = FilterByPlant(records, mappings, plants, result);

            List<string> codes = scoped
                .Select(record => IntegrationRecordReader.Read(mappings, "Material.Code", record)?.Trim().ToUpperInvariant())
                .Where(code => !string.IsNullOrEmpty(code))
                .Select(code => code!)
                .Distinct()
                .ToList();
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
            Dictionary<Guid, List<MaterialUomConversion>> conversionsByMaterial = await UomConverter.GetConversionsAsync(
                repository, materials.Values.Select(x => x.Id).ToList(), cancellationToken);
            Dictionary<string, Guid> locationByPlantSloc = await LoadLocationMapAsync(repository, buyerId, cancellationToken);
            Dictionary<string, string> propertyByPlant = await LoadPropertyNamesAsync(repository, buyerId, cancellationToken);
            _ = propertyByPlant;

            List<MaterialPlantAssignment> plantRows = await repository.MaterialPlantAssignment
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && materials.Values.Select(m => m.Id).Contains(x.MaterialId))
                .ToListAsync(cancellationToken);
            Dictionary<(Guid, string), MaterialPlantAssignment> plantIndex = plantRows
                .ToDictionary(x => (x.MaterialId, x.PlantCode.ToUpperInvariant()), x => x);

            List<MaterialStorageLocationAssignment> slocRows = await repository.MaterialStorageLocationAssignment
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && materials.Values.Select(m => m.Id).Contains(x.MaterialId))
                .ToListAsync(cancellationToken);
            Dictionary<(Guid, string, string), MaterialStorageLocationAssignment> slocIndex = slocRows
                .ToDictionary(x => (x.MaterialId, x.PlantCode.ToUpperInvariant(), x.StorageLocationCode.ToUpperInvariant()), x => x);

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string reason = $"ERP material pull, company code {companyCode}";
            string? syncedCompanyCode = string.IsNullOrWhiteSpace(companyCode) || companyCode.Trim().ToUpperInvariant() == "ALL" ? null : companyCode.Trim();
            DateTime now = DateTime.UtcNow;

            int missingDescriptionNotes = 0;
            foreach (JsonElement record in scoped)
            {
                result.Read++;
                string code = IntegrationRecordReader.Read(mappings, "Material.Code", record)?.Trim().ToUpperInvariant() ?? string.Empty;
                // Ignore mapped values that are just the material number (common bad Hub map of Description → Product).
                string? description = Clean(IntegrationRecordReader.Read(mappings, "Material.Description", record), 500);
                if (IsBlankOrMaterialCode(description, code))
                {
                    description = Clean(IntegrationRecordReader.ReadSapProductDescription(record), 500);
                }

                // Authoritative EN text from A_ProductDescription?$filter=Product eq '…' and Language eq 'EN'
                if (IsBlankOrMaterialCode(description, code)
                    && descriptionsByProduct != null
                    && descriptionsByProduct.TryGetValue(code, out string? fromDescriptionEntity))
                {
                    description = Clean(fromDescriptionEntity, 500);
                }

                if (IsBlankOrMaterialCode(description, code))
                {
                    description = null;
                }
                string? baseUom = Clean(IntegrationRecordReader.Read(mappings, "Material.BaseUom", record), 20)?.ToUpperInvariant();
                string? group = Clean(IntegrationRecordReader.Read(mappings, "Material.MaterialGroup", record), 100);
                string? materialType = Clean(IntegrationRecordReader.Read(mappings, "Material.MaterialType", record), 100);
                string? priceText = IntegrationRecordReader.Read(mappings, "Material.UnitPrice", record);
                decimal? price = IntegrationRecordReader.ParseDecimal(priceText);
                string? currency = Clean(IntegrationRecordReader.Read(mappings, "Material.Currency", record), 3)?.ToUpperInvariant();
                string? plant = Clean(IntegrationRecordReader.Read(mappings, "Material.Plant", record), 50)?.ToUpperInvariant();
                string? plantStatus = Clean(IntegrationRecordReader.Read(mappings, "Material.PlantStatus", record), 40);
                string? purchasingGroup = Clean(IntegrationRecordReader.Read(mappings, "Material.PurchasingGroup", record), 40);
                string? mrpType = Clean(IntegrationRecordReader.Read(mappings, "Material.MrpType", record), 40);
                string? procurementType = Clean(IntegrationRecordReader.Read(mappings, "Material.ProcurementType", record), 40);
                decimal? reorderPoint = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.ReorderPoint", record));
                decimal? safetyStock = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.SafetyStock", record));
                string? storageLocation = Clean(IntegrationRecordReader.Read(mappings, "Material.StorageLocation", record), 50)?.ToUpperInvariant();
                string? altUom = Clean(IntegrationRecordReader.Read(mappings, "Material.AltUom", record), 20)?.ToUpperInvariant();
                decimal? altNum = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.AltUomNumerator", record));
                decimal? altDen = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.AltUomDenominator", record));
                string? valuationClass = Clean(IntegrationRecordReader.Read(mappings, "Material.ValuationClass", record), 40);
                string? priceControl = Clean(IntegrationRecordReader.Read(mappings, "Material.PriceControl", record), 10)?.ToUpperInvariant();
                decimal? standardPrice = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.StandardPrice", record));
                decimal? movingAverage = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.MovingAveragePrice", record));
                decimal? priceUnit = IntegrationRecordReader.ParseDecimal(IntegrationRecordReader.Read(mappings, "Material.PriceUnit", record));

                if (code.Length == 0 || code.Length > MAX_CODE_LENGTH)
                {
                    Fail(result, $"Record {result.Read}: the material code is missing or longer than {MAX_CODE_LENGTH} characters.");
                    continue;
                }

                string dedupeKey = $"{code}|{plant ?? ""}|{storageLocation ?? ""}|{altUom ?? ""}";
                if (!seen.Add(dedupeKey) && string.IsNullOrEmpty(plant) && string.IsNullOrEmpty(storageLocation) && string.IsNullOrEmpty(altUom))
                {
                    Fail(result, $"{code}: appears more than once in the ERP response; only the first header record was used.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(priceText) && price == null)
                {
                    Fail(result, $"{code}: the unit price '{priceText}' is not a number.");
                    continue;
                }

                bool isNew = !materials.TryGetValue(code, out ItemBuyerMaster? material);
                bool changed = false;
                if (material == null)
                {
                    if (string.IsNullOrEmpty(baseUom))
                    {
                        Fail(result, $"{code}: the base unit of measure is missing; a new material needs one.");
                        continue;
                    }

                    // Create even when ERP description expand is missing — list shows "Missing description"
                    // instead of blocking the whole pull. Do not store the material number as Description.
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        if (missingDescriptionNotes < 3)
                        {
                            Note(result, $"{code}: saved without description (map Material.Description / $expand=to_Description).");
                        }
                        else if (missingDescriptionNotes == 3)
                        {
                            Note(result, "Further materials were also saved without description.");
                        }

                        missingDescriptionNotes++;
                    }

                    material = NewMaterial(buyerId, code, description, baseUom, group, currency, materialType);
                    material.CompanyCode = syncedCompanyCode;
                    ApplyValuation(material, valuationClass, priceControl, standardPrice, movingAverage, priceUnit, out _);
                    repository.ItemBuyerMaster.Create(material);
                    materials[code] = material;
                    conversionsByMaterial[material.Id] = new List<MaterialUomConversion>();
                }
                else
                {
                    changed = Update(material, description, group, materialType);
                    if (syncedCompanyCode != null && material.CompanyCode != syncedCompanyCode)
                    {
                        material.CompanyCode = syncedCompanyCode;
                        changed = true;
                    }

                    if (ApplyValuation(material, valuationClass, priceControl, standardPrice, movingAverage, priceUnit, out bool valuationChanged))
                    {
                        changed = changed || valuationChanged;
                    }

                    if (changed)
                    {
                        repository.ItemBuyerMaster.Update(material);
                    }
                }

                // When Material.Plant is unmapped (typical A_Product header), still assign the plants the operator selected.
                List<string> plantsToAssign = !string.IsNullOrEmpty(plant)
                    ? new List<string> { plant }
                    : plants != null
                        ? plants.OrderBy(x => x).ToList()
                        : new List<string>();

                foreach (string assignPlant in plantsToAssign)
                {
                    changed |= UpsertPlant(
                        repository, buyerId, material.Id, assignPlant, plantStatus, purchasingGroup, mrpType, procurementType, reorderPoint, safetyStock, plantIndex, now);
                }

                string? slocPlant = plant ?? (plantsToAssign.Count == 1 ? plantsToAssign[0] : null);
                if (!string.IsNullOrEmpty(slocPlant) && !string.IsNullOrEmpty(storageLocation))
                {
                    changed |= await UpsertStorageLocationAsync(
                        repository, buyerId, material, code, slocPlant, storageLocation, locationByPlantSloc, slocIndex, now, result, cancellationToken);
                }

                if (!string.IsNullOrEmpty(altUom) && !string.IsNullOrEmpty(baseUom ?? material.BaseUnitOfMeasure))
                {
                    string baseUnit = (baseUom ?? material.BaseUnitOfMeasure).ToUpperInvariant();
                    changed |= UpsertUom(repository, buyerId, material.Id, baseUnit, altUom, altNum, altDen, conversionsByMaterial, result, code);
                }

                bool priceRequested = false;
                if (price != null && price.Value != material.UnitCost)
                {
                    if (pending.ContainsKey(material.Id))
                    {
                        Note(result, $"{code}: ERP price {price} not requested, a price change is already waiting for approval.");
                    }
                    else if (!flowExists)
                    {
                        Note(result, $"{code}: ERP price {price} not requested, no {Common.SILA_APPROVAL_TYPE_MATERIAL_PRICE} approval flow exists (material fields were saved).");
                    }
                    else
                    {
                        List<string> errors = SilaMaterialPricing.Validate(
                            logger, material, conversionsByMaterial, price.Value, currency, null, reason, out _, out _);
                        if (errors.Count > 0)
                        {
                            Note(result, $"{code}: ERP price not requested. {string.Join(" ", errors)}");
                        }
                        else
                        {
                            await SilaMaterialPricing.CreateAsync(
                                repository, logger, buyerId, material, conversionsByMaterial, price.Value, currency, null, null, reason, userId, scope, cancellationToken);
                            priceRequested = true;
                            result.PriceChanges++;
                        }
                    }
                }

                CountOutcome(result, isNew, changed || priceRequested);
            }
        }

        private static HashSet<string>? NormalizePlantFilter(IReadOnlyCollection<string>? plantFilter)
        {
            if (plantFilter == null || plantFilter.Count == 0) return null;
            HashSet<string> plants = new HashSet<string>(
                plantFilter.Select(x => x.Trim().ToUpperInvariant()).Where(x => x.Length > 0),
                StringComparer.OrdinalIgnoreCase);
            return plants.Count == 0 ? null : plants;
        }

        private static List<JsonElement> FilterByPlant(
            List<JsonElement> records,
            List<ApiFieldMapping> mappings,
            HashSet<string>? plants,
            SilaMaterialErpPullResultDto result)
        {
            if (plants == null) return records;
            bool hasPlantMapping = mappings.Any(x => x.TargetField.Equals("Material.Plant", StringComparison.OrdinalIgnoreCase));
            if (!hasPlantMapping)
            {
                Note(result, "Plant filter requested but Material.Plant is not mapped; applying all returned header records.");
                return records;
            }

            List<JsonElement> filtered = new List<JsonElement>();
            foreach (JsonElement record in records)
            {
                string? plant = Clean(IntegrationRecordReader.Read(mappings, "Material.Plant", record), 50)?.ToUpperInvariant();
                if (plant == null || plants.Contains(plant))
                {
                    filtered.Add(record);
                }
            }

            return filtered;
        }

        private static async Task<Dictionary<string, Guid>> LoadLocationMapAsync(
            IRepositoryWrapper repository, Guid buyerId, CancellationToken cancellationToken)
        {
            Dictionary<Guid, string> plants = (await repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.PlantCode.Trim().ToUpperInvariant());
            List<InventoryLocation> locations = await repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.StorageLocationCode != null)
                .ToListAsync(cancellationToken);
            Dictionary<string, Guid> map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (InventoryLocation location in locations)
            {
                if (!plants.TryGetValue(location.PropertyId, out string? plant) || string.IsNullOrWhiteSpace(location.StorageLocationCode)) continue;
                string key = $"{plant}|{location.StorageLocationCode.Trim().ToUpperInvariant()}";
                map.TryAdd(key, location.Id);
            }

            return map;
        }

        private static async Task<Dictionary<string, string>> LoadPropertyNamesAsync(
            IRepositoryWrapper repository, Guid buyerId, CancellationToken cancellationToken)
        {
            return (await repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.PlantCode.Trim().ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.First().PropertyName, StringComparer.OrdinalIgnoreCase);
        }

        private static bool UpsertPlant(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid materialId,
            string plant,
            string? plantStatus,
            string? purchasingGroup,
            string? mrpType,
            string? procurementType,
            decimal? reorderPoint,
            decimal? safetyStock,
            Dictionary<(Guid, string), MaterialPlantAssignment> plantIndex,
            DateTime now)
        {
            var key = (materialId, plant);
            if (plantIndex.TryGetValue(key, out MaterialPlantAssignment? row))
            {
                bool changed = false;
                if (plantStatus != null && row.PlantStatus != plantStatus) { row.PlantStatus = plantStatus; changed = true; }
                if (purchasingGroup != null && row.PurchasingGroup != purchasingGroup) { row.PurchasingGroup = purchasingGroup; changed = true; }
                if (mrpType != null && row.MrpType != mrpType) { row.MrpType = mrpType; changed = true; }
                if (procurementType != null && row.ProcurementType != procurementType) { row.ProcurementType = procurementType; changed = true; }
                if (reorderPoint != null && row.ReorderPoint != reorderPoint) { row.ReorderPoint = reorderPoint; changed = true; }
                if (safetyStock != null && row.SafetyStock != safetyStock) { row.SafetyStock = safetyStock; changed = true; }
                row.LastSyncedOn = now;
                row.SourceSystem = SOURCE_ERP;
                if (changed) repository.MaterialPlantAssignment.Update(row);
                return changed;
            }

            MaterialPlantAssignment created = new MaterialPlantAssignment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialId = materialId,
                PlantCode = plant,
                PlantStatus = plantStatus,
                PurchasingGroup = purchasingGroup,
                MrpType = mrpType,
                ProcurementType = procurementType,
                ReorderPoint = reorderPoint,
                SafetyStock = safetyStock,
                LastSyncedOn = now,
                SourceSystem = SOURCE_ERP,
                IsActive = true
            };
            repository.MaterialPlantAssignment.Create(created);
            plantIndex[key] = created;
            return true;
        }

        private static async Task<bool> UpsertStorageLocationAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            ItemBuyerMaster material,
            string code,
            string plant,
            string storageLocation,
            Dictionary<string, Guid> locationByPlantSloc,
            Dictionary<(Guid, string, string), MaterialStorageLocationAssignment> slocIndex,
            DateTime now,
            SilaMaterialErpPullResultDto result,
            CancellationToken cancellationToken)
        {
            var key = (material.Id, plant, storageLocation);
            string mapKey = $"{plant}|{storageLocation}";
            Guid? locationId = locationByPlantSloc.TryGetValue(mapKey, out Guid found) ? found : null;
            if (locationId == null)
            {
                Note(result, $"{code}: SAP storage location {plant}/{storageLocation} is not mapped to a SILA location (recorded in exception worklist).");
                await RecordUnmappedSlocAsync(repository, buyerId, material.Id, code, plant, storageLocation, cancellationToken);
            }

            if (slocIndex.TryGetValue(key, out MaterialStorageLocationAssignment? row))
            {
                bool changed = row.InventoryLocationId != locationId;
                row.InventoryLocationId = locationId;
                row.AssignmentStatus = locationId == null ? "UNMAPPED" : "MAPPED";
                row.LastSyncedOn = now;
                row.SourceSystem = SOURCE_ERP;
                if (changed) repository.MaterialStorageLocationAssignment.Update(row);
                return changed;
            }

            MaterialStorageLocationAssignment created = new MaterialStorageLocationAssignment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialId = material.Id,
                PlantCode = plant,
                StorageLocationCode = storageLocation,
                InventoryLocationId = locationId,
                AssignmentStatus = locationId == null ? "UNMAPPED" : "MAPPED",
                LastSyncedOn = now,
                SourceSystem = SOURCE_ERP,
                IsActive = true
            };
            repository.MaterialStorageLocationAssignment.Create(created);
            slocIndex[key] = created;
            return true;
        }

        private static async Task RecordUnmappedSlocAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid materialId,
            string code,
            string plant,
            string storageLocation,
            CancellationToken cancellationToken)
        {
            bool exists = await repository.MaterialIntegrationException
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && !x.Resolved
                    && x.ExceptionType == "UNMAPPED_STORAGE_LOCATION"
                    && x.MaterialId == materialId
                    && x.PlantCode == plant
                    && x.StorageLocationCode == storageLocation)
                .AnyAsync(cancellationToken);
            if (exists) return;
            repository.MaterialIntegrationException.Create(new MaterialIntegrationException
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialId = materialId,
                MaterialCode = code,
                ExceptionType = "UNMAPPED_STORAGE_LOCATION",
                PlantCode = plant,
                StorageLocationCode = storageLocation,
                Message = $"SAP storage location {plant}/{storageLocation} has no SILA InventoryLocation mapping.",
                Resolved = false,
                IsActive = true
            });
        }

        private static bool UpsertUom(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid materialId,
            string baseUom,
            string altUom,
            decimal? numerator,
            decimal? denominator,
            Dictionary<Guid, List<MaterialUomConversion>> conversionsByMaterial,
            SilaMaterialErpPullResultDto result,
            string code)
        {
            if (altUom == baseUom) return false;
            if (numerator == null || denominator == null || numerator <= 0 || denominator <= 0)
            {
                Note(result, $"{code}: alternate UOM {altUom} skipped — numerator/denominator missing or invalid.");
                return false;
            }

            // Factor: 1 AltUom = (numerator/denominator) BaseUom — material-specific.
            decimal factor = numerator.Value / denominator.Value;
            if (!conversionsByMaterial.TryGetValue(materialId, out List<MaterialUomConversion>? list))
            {
                list = new List<MaterialUomConversion>();
                conversionsByMaterial[materialId] = list;
            }

            MaterialUomConversion? existing = list.FirstOrDefault(x =>
                x.FromUom.Equals(altUom, StringComparison.OrdinalIgnoreCase)
                && x.ToUom.Equals(baseUom, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (existing.Factor == factor) return false;
                existing.Factor = factor;
                repository.MaterialUomConversion.Update(existing);
                return true;
            }

            MaterialUomConversion created = new MaterialUomConversion
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialId = materialId,
                FromUom = altUom,
                ToUom = baseUom,
                Factor = factor,
                IsActive = true
            };
            repository.MaterialUomConversion.Create(created);
            list.Add(created);
            return true;
        }

        /// <summary>Updates Standard/MovingAverage/ValuationClass only — never UnitCost (approval path).</summary>
        private static bool ApplyValuation(
            ItemBuyerMaster material,
            string? valuationClass,
            string? priceControl,
            decimal? standardPrice,
            decimal? movingAverage,
            decimal? priceUnit,
            out bool changed)
        {
            changed = false;
            if (valuationClass != null && material.ValuationClass != valuationClass)
            {
                material.ValuationClass = valuationClass;
                changed = true;
            }

            // Normalize when price unit &gt; 1: store per single base unit.
            decimal divisor = priceUnit is > 0 ? priceUnit.Value : 1m;
            if (standardPrice != null)
            {
                decimal normalized = standardPrice.Value / divisor;
                if (material.StandardPrice != normalized)
                {
                    material.StandardPrice = normalized;
                    changed = true;
                }
            }

            if (movingAverage != null)
            {
                decimal normalized = movingAverage.Value / divisor;
                if (material.MovingAveragePrice != normalized)
                {
                    material.MovingAveragePrice = normalized;
                    changed = true;
                }
            }

            _ = priceControl; // Informational; PriceControl on DTO is derived from which prices are set.
            return changed;
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

        private static bool IsBlankOrMaterialCode(string? value, string materialCode)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            return value.Trim().Equals(materialCode, StringComparison.OrdinalIgnoreCase);
        }

        private static ItemBuyerMaster NewMaterial(
            Guid buyerId, string code, string? description, string baseUom, string? group, string? currency, string? materialType)
        {
            return new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialCode = code,
                Description = description ?? string.Empty,
                MaterialGroup = group ?? string.Empty,
                ProductType = materialType ?? string.Empty,
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

        private static bool Update(ItemBuyerMaster material, string? description, string? group, string? materialType)
        {
            bool changed = false;
            // Replace blank / material-number placeholders when a real description arrives.
            if (!string.IsNullOrWhiteSpace(description)
                && !IsBlankOrMaterialCode(description, material.MaterialCode ?? string.Empty)
                && description != material.Description)
            {
                material.Description = description;
                changed = true;
            }

            if (group != null && group != material.MaterialGroup)
            {
                material.MaterialGroup = group;
                changed = true;
            }

            // Do not overwrite locally owned ProductType when ERP type is empty; only set when provided.
            if (!string.IsNullOrWhiteSpace(materialType) && material.ProductType != materialType)
            {
                material.ProductType = materialType;
                changed = true;
            }

            return changed;
        }

        private static void CountOutcome(SilaMaterialErpPullResultDto result, bool isNew, bool changed)
        {
            if (isNew) result.New++;
            else if (changed) result.Changed++;
            else result.Unchanged++;
        }

        private static void Fail(SilaMaterialErpPullResultDto result, string message)
        {
            result.Failed++;
            Note(result, message);
        }

        private static void Note(SilaMaterialErpPullResultDto result, string message)
        {
            if (result.Failures.Count < MAX_FAILURES) result.Failures.Add(message);
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string trimmed = value.Trim();
            return trimmed.Length > maxLength ? trimmed.Substring(0, maxLength) : trimmed;
        }
    }
}
