using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.Integration;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// One SAP stock-posting decision for internal transfers, recipe/POS consumption, stock-count variances and
    /// adjustments. The four API names already exist in Integration Settings. Nothing here chooses another API.
    /// </summary>
    public static class SilaInventorySap
    {
        public const string API_TRANSFER = "FIVE S4 - Transfer Stock";
        /// <summary>Legacy display name still accepted when resolving Transfer Stock.</summary>
        public const string API_TRANSFER_LEGACY = "S4 - Transfer Stock";
        public const string API_PLANT_TRANSFER = "FIVE S4 - Plant Transfer Stock";
        /// <summary>Legacy display name still accepted when resolving Plant Transfer Stock.</summary>
        public const string API_PLANT_TRANSFER_LEGACY = "S4 - Plant Transfer Stock";
        /// <summary>Canonical Integration Setup name for Goods Issue (POS sales consumption uses this with Z01).</summary>
        public const string API_GOODS_ISSUE = "S4 - Goods Issue";
        /// <summary>Legacy display name still accepted when resolving the same Goods Issue connector.</summary>
        public const string API_GOODS_ISSUE_LEGACY = "FIVE S4 - Goods Issue";
        public const string API_POSITIVE_ADJUSTMENT = "FIVE S4 - Positive Stock Adjustment";
        /// <summary>Legacy display name still accepted when resolving Positive Stock Adjustment.</summary>
        public const string API_POSITIVE_ADJUSTMENT_LEGACY = "S4 - Positive Stock Adjustment";
        /// <summary>Waste / damage / breakage / spoilage / expired via Integration Setup FIVE_STOCK_WASTE_UPDATE (Z01 out, Z02 reverse).</summary>
        public const string API_WASTE = "FIVE_STOCK_WASTE_UPDATE";
        /// <summary>SAP goods-movement reason for wastage (required by FIVE_STOCK_WASTE_UPDATE sample).</summary>
        public const string REASON_WASTE = "0001";

        /// <summary>Non-POS goods issue configurations (S4 - Goods Issue). POS sales use <see cref="PosUpdateApiNames"/>.</summary>
        public static readonly string[] GoodsIssueApiNames = { API_GOODS_ISSUE_LEGACY, API_GOODS_ISSUE };
        /// <summary>Positive stock adjustment names (surplus / stock-count IN).</summary>
        public static readonly string[] PositiveAdjustmentApiNames = { API_POSITIVE_ADJUSTMENT, API_POSITIVE_ADJUSTMENT_LEGACY };
        /// <summary>Waste adjustment API names.</summary>
        public static readonly string[] WasteApiNames = { API_WASTE };
        /// <summary>Same-plant transfer (311/312) names.</summary>
        public static readonly string[] TransferApiNames = { API_TRANSFER, API_TRANSFER_LEGACY };
        /// <summary>Plant-to-plant transfer (301/302) names.</summary>
        public static readonly string[] PlantTransferApiNames = { API_PLANT_TRANSFER, API_PLANT_TRANSFER_LEGACY };

        /// <summary>All POS sales postings (consumption and RETURN/VOID) use this Integration Setup name only.</summary>
        public static readonly string[] PosUpdateApiNames = { SilaStockCountSap.API_CONFIG_NAME };

        /// <summary>Configurable POS MaterialDocumentHeaderText prefix (SAP length limit applied later).</summary>
        public const string POS_HEADER_PREFIX = "POSSALES";

        public const string CODE_TRANSFER = "04";
        public const string CODE_TRANSFER_REVERSAL = "06";
        public const string CODE_ISSUE = "03";
        public const string TYPE_311 = "311";
        public const string TYPE_312 = "312";
        public const string TYPE_301 = "301";
        public const string TYPE_302 = "302";
        public const string TYPE_Z02 = "Z02";
        public const string TYPE_Z01 = "Z01";
        /// <summary>SAP inventory difference — surplus (physical &gt; book).</summary>
        public const string TYPE_701 = "701";
        /// <summary>SAP inventory difference — shortage (physical &lt; book).</summary>
        public const string TYPE_702 = "702";

        public const string SOURCE_SAP_LOCATION_MAPPING_MISSING = "SOURCE_SAP_LOCATION_MAPPING_MISSING";
        public const string DESTINATION_SAP_LOCATION_MAPPING_MISSING = "DESTINATION_SAP_LOCATION_MAPPING_MISSING";
        public const string INVENTORY_LOCATION_MAPPING_MISSING = "INVENTORY_LOCATION_MAPPING_MISSING";
        public const string INVALID_TRANSFER_LOCATION = "INVALID_TRANSFER_LOCATION";
        public const string CROSS_COMPANY_TRANSFER_NOT_CONFIGURED = "CROSS_COMPANY_TRANSFER_NOT_CONFIGURED";
        public const string UOM_CONVERSION_MISSING = "UOM_CONVERSION_MISSING";
        public const string MATERIAL_MAPPING_MISSING = "MATERIAL_MAPPING_MISSING";
        public const string MATERIAL_NOT_VALID = "MATERIAL_NOT_VALID";
        public const string SAP_API_CONFIGURATION_NOT_FOUND = "SAP_API_CONFIGURATION_NOT_FOUND";
        public const string SAP_API_CONFIGURATION_INACTIVE = "SAP_API_CONFIGURATION_INACTIVE";
        public const string ALREADY_POSTED = "ALREADY_POSTED";
        public const string COST_CENTER_MISSING = "COST_CENTER_MISSING";
        public const string MATERIAL_DOCUMENT_ENTITY = "A_MaterialDocumentHeader";

        public static readonly Rule SamePlantTransfer = new Rule(API_TRANSFER, CODE_TRANSFER, TYPE_311, false);
        /// <summary>Explicit 312 reversal on FIVE S4 - Transfer Stock (same API as 311).</summary>
        public static readonly Rule SamePlantTransferReversal = new Rule(API_TRANSFER, CODE_TRANSFER_REVERSAL, TYPE_312, false);
        /// <summary>Plant-to-plant (incl. cross-company) via FIVE S4 - Plant Transfer Stock.</summary>
        public static readonly Rule PlantTransfer = new Rule(API_PLANT_TRANSFER, CODE_TRANSFER, TYPE_301, true);
        /// <summary>Explicit 302 reversal on FIVE S4 - Plant Transfer Stock (same API as 301).</summary>
        public static readonly Rule PlantTransferReversal = new Rule(API_PLANT_TRANSFER, CODE_TRANSFER_REVERSAL, TYPE_302, true);
        /// <summary>Non-POS goods issue (stock decrease) via FIVE S4 - Goods Issue with customer Z02 where configured.</summary>
        public static readonly Rule GoodsIssue = new Rule(API_GOODS_ISSUE_LEGACY, CODE_ISSUE, TYPE_Z02, false, increasesStock: false);
        /// <summary>
        /// POS sales consumption via FIVE_POS_UPDATE: GoodsMovementCode 03, GoodsMovementType Z01.
        /// Stock decreases (IncreasesStock = false) even though the movement type is Z01.
        /// </summary>
        public static readonly Rule PosConsumption = new Rule(
            SilaStockCountSap.API_CONFIG_NAME, CODE_ISSUE, TYPE_Z01, false, increasesStock: false);
        /// <summary>POS RETURN/VOID via FIVE_POS_UPDATE restores stock (IN) with Z01.</summary>
        public static readonly Rule PosReversal = new Rule(
            SilaStockCountSap.API_CONFIG_NAME, CODE_ISSUE, TYPE_Z01, false, increasesStock: true);
        /// <summary>Non-POS positive stock adjustments restore stock (IN).</summary>
        public static readonly Rule PositiveAdjustment = new Rule(API_POSITIVE_ADJUSTMENT, CODE_ISSUE, TYPE_Z01, false, increasesStock: true);
        /// <summary>
        /// Waste / damage out via FIVE_STOCK_WASTE_UPDATE (Z01). Stock decreases.
        /// Reason code omitted: SAP M7/146 rejects GoodsMovementReasonCode 0001 for Z01 on this tenant.
        /// </summary>
        public static readonly Rule WasteIssue = new Rule(
            API_WASTE, CODE_ISSUE, TYPE_Z01, false, increasesStock: false);
        /// <summary>Partial/full reverse of waste via FIVE_STOCK_WASTE_UPDATE (Z02). Stock increases.</summary>
        public static readonly Rule WasteReversal = new Rule(
            API_WASTE, CODE_ISSUE, TYPE_Z02, false, increasesStock: true);
        /// <summary>
        /// Live Inventory stock-count surplus (701). Credentials from FIVE S4 - Positive Stock Adjustment;
        /// posting uses API_PHYSICAL_INVENTORY_DOC_SRV (Material Document API rejects 701 with M7/096).
        /// Prefer <see cref="ForDirection"/> (Z01/Z02) for Live Inventory stock count when 701/702 is unavailable.
        /// </summary>
        public static readonly Rule InventoryDifferenceSurplus = new Rule(API_POSITIVE_ADJUSTMENT, CODE_ISSUE, TYPE_701, false, increasesStock: true);
        /// <summary>
        /// Live Inventory stock-count shortage (702). Same Physical Inventory Document path as 701.
        /// </summary>
        public static readonly Rule InventoryDifferenceShortage = new Rule(API_POSITIVE_ADJUSTMENT, CODE_ISSUE, TYPE_702, false, increasesStock: false);

        /// <summary>OUT uses GoodsIssue (Z02) for non-POS; IN uses PositiveAdjustment (Z01).</summary>
        public static Rule ForDirection(string? direction)
        {
            return direction == Common.SILA_DIRECTION_IN ? PositiveAdjustment : GoodsIssue;
        }

        /// <summary>Stock-count physical variance: positive → 701, negative → 702.</summary>
        public static Rule ForInventoryDifference(string? direction)
        {
            return direction == Common.SILA_DIRECTION_IN ? InventoryDifferenceSurplus : InventoryDifferenceShortage;
        }

        public static bool IsInventoryDifference(string? movementType)
        {
            return movementType == TYPE_701 || movementType == TYPE_702;
        }

        public static bool IsWasteApi(string? apiName)
        {
            return !string.IsNullOrWhiteSpace(apiName)
                && WasteApiNames.Any(x => string.Equals(x, apiName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsWasteAdjustmentType(string? adjustmentType)
        {
            string type = (adjustmentType ?? string.Empty).Trim().ToUpperInvariant();
            return type is "WASTE" or "DAMAGE" or "BREAKAGE" or "SPOILAGE" or "EXPIRED";
        }

        /// <summary>Ledger direction for a posting rule (honours IncreasesStock for Z01 waste / Z02 reverse).</summary>
        public static string DirectionForRule(Rule rule)
        {
            return rule.IncreasesStock ? Common.SILA_DIRECTION_IN : Common.SILA_DIRECTION_OUT;
        }

        public static string DirectionForMovement(string? movementType)
        {
            if (movementType == TYPE_Z01 || movementType == TYPE_701)
            {
                return Common.SILA_DIRECTION_IN;
            }

            return Common.SILA_DIRECTION_OUT;
        }

        /// <summary>API name aliases used when resolving the rule’s Integration Setup row.</summary>
        public static IReadOnlyList<string> ApiNamesFor(Rule rule)
        {
            if (IsWasteApi(rule.ApiName))
            {
                return WasteApiNames;
            }

            if (string.Equals(rule.ApiName, API_POSITIVE_ADJUSTMENT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.ApiName, API_POSITIVE_ADJUSTMENT_LEGACY, StringComparison.OrdinalIgnoreCase))
            {
                return PositiveAdjustmentApiNames;
            }

            if (GoodsIssueApiNames.Any(x => string.Equals(x, rule.ApiName, StringComparison.OrdinalIgnoreCase)))
            {
                return GoodsIssueApiNames;
            }

            if (TransferApiNames.Any(x => string.Equals(x, rule.ApiName, StringComparison.OrdinalIgnoreCase)))
            {
                return TransferApiNames;
            }

            if (PlantTransferApiNames.Any(x => string.Equals(x, rule.ApiName, StringComparison.OrdinalIgnoreCase)))
            {
                return PlantTransferApiNames;
            }

            return new[] { rule.ApiName };
        }

        /// <summary>
        /// POS SALE consumption is Z01 (via S4 - Goods Issue). Negative quantity / legacy rows that used Z02 keep their stored type.
        /// Prefer the queued MovementType when already set.
        /// </summary>
        public static string MovementForSale(string? movementType, decimal quantitySold)
        {
            if (movementType == TYPE_Z01 || movementType == TYPE_Z02)
            {
                return movementType;
            }

            // User-confirmed: POS consumption posts as Z01 (not Z02).
            return TYPE_Z01;
        }

        /// <summary>Build SAP ReferenceDocument e.g. POS-FL10-100046 (max 16 chars).</summary>
        public static string BuildPosReferenceDocument(string? outletOrStorageCode, string? sourceTransactionId)
        {
            string outlet = string.IsNullOrWhiteSpace(outletOrStorageCode) ? "POS" : outletOrStorageCode.Trim().ToUpperInvariant();
            string txn = string.IsNullOrWhiteSpace(sourceTransactionId) ? "REF" : sourceTransactionId.Trim();
            string value = $"POS-{outlet}-{txn}";
            return value.Length > 16 ? value[..16] : value;
        }

        /// <summary>Build MaterialDocumentHeaderText e.g. POSSALES2 (max 25 chars).</summary>
        public static string BuildPosHeaderText(string? batchOrSequenceHint = null)
        {
            string suffix = string.IsNullOrWhiteSpace(batchOrSequenceHint)
                ? "2"
                : new string(batchOrSequenceHint.Trim().Where(char.IsLetterOrDigit).Take(8).ToArray());
            if (string.IsNullOrEmpty(suffix))
            {
                suffix = "2";
            }

            string value = POS_HEADER_PREFIX + suffix;
            return value.Length > 25 ? value[..25] : value;
        }

        /// <summary>User-facing Cost Center missing error for a plant / storage location pair.</summary>
        public static string CostCenterMissingMessage(string? plant, string? storageLocation)
        {
            string plantText = string.IsNullOrWhiteSpace(plant) ? "(unknown)" : plant.Trim();
            string storageText = string.IsNullOrWhiteSpace(storageLocation) ? "(unknown)" : storageLocation.Trim();
            return $"Cost Center missing for Plant {plantText} / Storage Location {storageText}. Maintain it in Storage Location Setup.";
        }

        public static string MovementForDirection(string? direction)
        {
            return ForDirection(direction).GoodsMovementType;
        }

        /// <summary>
        /// Null when the locations can be posted. Same plant and a different storage location is 311
        /// (FIVE S4 - Transfer Stock). A different plant is 301 (FIVE S4 - Plant Transfer Stock), including
        /// cross-company plant-to-plant. Same-plant cross-company is refused.
        /// </summary>
        public static string? DecideTransfer(
            string? fromPlant,
            string? fromStorage,
            string? toPlant,
            string? toStorage,
            string? fromCompany,
            string? toCompany,
            out Rule? rule)
        {
            rule = null;
            if (string.IsNullOrWhiteSpace(fromPlant) || string.IsNullOrWhiteSpace(fromStorage))
            {
                return SOURCE_SAP_LOCATION_MAPPING_MISSING;
            }

            if (string.IsNullOrWhiteSpace(toPlant) || string.IsNullOrWhiteSpace(toStorage))
            {
                return DESTINATION_SAP_LOCATION_MAPPING_MISSING;
            }

            if (!Same(fromPlant, toPlant))
            {
                rule = PlantTransfer;
                return null;
            }

            bool companiesKnown = !string.IsNullOrWhiteSpace(fromCompany) && !string.IsNullOrWhiteSpace(toCompany);
            if (companiesKnown && !Same(fromCompany!, toCompany!))
            {
                return CROSS_COMPANY_TRANSFER_NOT_CONFIGURED;
            }

            if (Same(fromStorage, toStorage))
            {
                return INVALID_TRANSFER_LOCATION;
            }

            rule = SamePlantTransfer;
            return null;
        }

        public static string? EntryUnitOf(string? baseUnitOfMeasure)
        {
            string unit = (baseUnitOfMeasure ?? string.Empty).Trim();
            return unit.Length == 0 ? null : unit;
        }

        public static string FormatQuantity(decimal quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "QuantityInEntryUnit must be positive.");
            }

            return quantity.ToString("0.000################", CultureInfo.InvariantCulture);
        }

        public static string FormatDate(DateTime value)
        {
            return value.Date.ToString("yyyy-MM-dd'T'00:00:00", CultureInfo.InvariantCulture);
        }

        public static string Describe(string code)
        {
            return code switch
            {
                SOURCE_SAP_LOCATION_MAPPING_MISSING => "The source location has no SAP plant or storage location.",
                DESTINATION_SAP_LOCATION_MAPPING_MISSING => "The destination location has no SAP plant or storage location.",
                INVENTORY_LOCATION_MAPPING_MISSING => "The inventory location has no SAP plant or storage location.",
                INVALID_TRANSFER_LOCATION => "The source and destination storage locations are the same.",
                CROSS_COMPANY_TRANSFER_NOT_CONFIGURED => "Same-plant transfers across different company codes are not posted. Use plant-to-plant (301) when plants differ.",
                UOM_CONVERSION_MISSING => "The material has no SAP unit of measure, or the quantity cannot be converted to it.",
                MATERIAL_MAPPING_MISSING => "The material is not an active stock item with a material number.",
                MATERIAL_NOT_VALID => "The material is not an active stock item.",
                SAP_API_CONFIGURATION_NOT_FOUND => "The SAP Goods Issue API is not configured under Integration Setup. No other API is used.",
                SAP_API_CONFIGURATION_INACTIVE => "The SAP Goods Issue API exists but is not ACTIVE under Integration Setup.",
                ALREADY_POSTED => "This stock movement already has an SAP material document.",
                COST_CENTER_MISSING => "Cost Center missing for the resolved plant / storage location. Maintain Cost Center in Storage Location Setup before posting POS consumption.",
                _ => "The stock movement cannot be posted."
            };
        }

        public static string Failure(string code)
        {
            return code + ". " + Describe(code);
        }

        public static string MissingApi(string apiName)
        {
            return SAP_API_CONFIGURATION_NOT_FOUND + ". Activate " + apiName + " under Integration Setup. No other API is used for POS consumption.";
        }

        public static string InactiveApi(string apiName)
        {
            return SAP_API_CONFIGURATION_INACTIVE + ". Activate " + apiName + " under Integration Setup. POS consumption does not use another API.";
        }

        /// <summary>
        /// Explicit reversal for a prior transfer: 311 → 312 (FIVE S4 - Transfer Stock) or 301 → 302
        /// (FIVE S4 - Plant Transfer Stock). Stock effect: decrease original destination, increase original source.
        /// </summary>
        public static string? TryBuildTransferReversal(
            DateTime businessDate,
            string? originalFromPlant,
            string? originalFromStorage,
            string? originalToPlant,
            string? originalToStorage,
            string? fromCompany,
            string? toCompany,
            string? referenceDocument,
            IReadOnlyList<TransferLine> lines,
            Dictionary<Guid, List<MaterialUomConversion>> conversions,
            out string? body,
            out Rule? rule)
        {
            body = null;
            rule = null;
            string? problem = DecideTransfer(
                originalFromPlant, originalFromStorage, originalToPlant, originalToStorage, fromCompany, toCompany, out Rule? originalRule);
            if (problem != null)
            {
                return problem;
            }

            if (originalRule == null
                || (originalRule.GoodsMovementType != TYPE_311 && originalRule.GoodsMovementType != TYPE_301))
            {
                return INVALID_TRANSFER_LOCATION;
            }

            bool plantTransfer = originalRule.GoodsMovementType == TYPE_301;
            rule = plantTransfer ? PlantTransferReversal : SamePlantTransferReversal;
            string sourcePlant = originalFromPlant!.Trim();
            string destinationPlant = originalToPlant!.Trim();
            string originalSource = originalFromStorage!.Trim();
            string originalDestination = originalToStorage!.Trim();
            List<TransferQuantity> ready = new List<TransferQuantity>();
            foreach (TransferLine line in lines.OrderBy(x => x.Material.MaterialCode))
            {
                if (line.Quantity <= 0)
                {
                    continue;
                }

                string? convertedProblem = ConvertLine(line.Material, line.Quantity, line.QuantityUom, conversions, out string material, out string unit, out decimal quantity);
                if (convertedProblem != null)
                {
                    return convertedProblem;
                }

                ready.Add(new TransferQuantity(material, unit, quantity));
            }

            if (ready.Count == 0)
            {
                rule = null;
                return null;
            }

            string postingDate = FormatDate(businessDate);
            string headerText = string.IsNullOrWhiteSpace(referenceDocument) ? "SILA ITO REVERSAL" : ("SILA ITO REV " + referenceDocument.Trim());
            if (headerText.Length > 25)
            {
                headerText = headerText[..25];
            }

            string? reference = string.IsNullOrWhiteSpace(referenceDocument) ? null : referenceDocument.Trim();
            if (reference != null && reference.Length > 16)
            {
                reference = reference[..16];
            }

            string goodsMovementCode = rule.GoodsMovementCode;
            string goodsMovementType = rule.GoodsMovementType;
            // OData V2 deep-insert: to_MaterialDocumentItem.results[...] (matches Integration Setup sample).
            List<Dictionary<string, object?>> items;
            if (plantTransfer)
            {
                // 302: stock leaves original destination plant and returns to original source plant.
                items = ready.Select(line => new Dictionary<string, object?>
                {
                    ["Material"] = line.Material,
                    ["Plant"] = destinationPlant,
                    ["StorageLocation"] = originalDestination,
                    ["GoodsMovementType"] = goodsMovementType,
                    ["QuantityInEntryUnit"] = FormatQuantity(line.Quantity),
                    ["EntryUnit"] = line.EntryUnit,
                    ["IssuingOrReceivingPlant"] = sourcePlant,
                    ["IssuingOrReceivingStorageLoc"] = originalSource
                }).ToList();
            }
            else
            {
                items = ready.Select(line => new Dictionary<string, object?>
                {
                    ["Material"] = line.Material,
                    ["Plant"] = sourcePlant,
                    ["StorageLocation"] = originalDestination,
                    ["GoodsMovementType"] = goodsMovementType,
                    ["QuantityInEntryUnit"] = FormatQuantity(line.Quantity),
                    ["EntryUnit"] = line.EntryUnit,
                    ["IssuingOrReceivingStorageLoc"] = originalSource
                }).ToList();
            }

            Dictionary<string, object?> header = new Dictionary<string, object?>
            {
                ["PostingDate"] = postingDate,
                ["DocumentDate"] = postingDate,
                ["GoodsMovementCode"] = goodsMovementCode,
                ["MaterialDocumentHeaderText"] = headerText,
                ["to_MaterialDocumentItem"] = new Dictionary<string, object?> { ["results"] = items }
            };
            if (reference != null)
            {
                header["ReferenceDocument"] = reference;
            }

            body = JsonSerializer.Serialize(header);
            return null;
        }

        /// <summary>One transfer, one material document. 311 stays in the source plant. 301 names the receiving plant.</summary>
        public static string? TryBuildTransfer(
            DateTime businessDate,
            string? fromPlant,
            string? fromStorage,
            string? toPlant,
            string? toStorage,
            string? fromCompany,
            string? toCompany,
            IReadOnlyList<TransferLine> lines,
            Dictionary<Guid, List<MaterialUomConversion>> conversions,
            out string? body,
            out Rule? rule,
            string? referenceDocument = null)
        {
            body = null;
            string? problem = DecideTransfer(fromPlant, fromStorage, toPlant, toStorage, fromCompany, toCompany, out rule);
            if (problem != null || rule == null)
            {
                return problem;
            }

            string sourcePlant = fromPlant!.Trim();
            string sourceStorage = fromStorage!.Trim();
            string destinationPlant = toPlant!.Trim();
            string destinationStorage = toStorage!.Trim();
            List<TransferQuantity> ready = new List<TransferQuantity>();
            foreach (TransferLine line in lines.OrderBy(x => x.Material.MaterialCode))
            {
                if (line.Quantity <= 0)
                {
                    continue;
                }

                string? convertedProblem = ConvertLine(line.Material, line.Quantity, line.QuantityUom, conversions, out string material, out string unit, out decimal quantity);
                if (convertedProblem != null)
                {
                    return convertedProblem;
                }

                ready.Add(new TransferQuantity(material, unit, quantity));
            }

            if (ready.Count == 0)
            {
                rule = null;
                return null;
            }

            string postingDate = FormatDate(businessDate);
            Rule decided = rule;
            string headerText = string.IsNullOrWhiteSpace(referenceDocument)
                ? "SILA Storage Location Transfer"
                : referenceDocument.Trim();
            if (headerText.Length > 25)
            {
                headerText = headerText[..25];
            }

            string? reference = string.IsNullOrWhiteSpace(referenceDocument) ? null : referenceDocument.Trim();
            if (reference != null && reference.Length > 16)
            {
                reference = reference[..16];
            }

            // OData V2 deep-insert: to_MaterialDocumentItem.results[...] (matches Integration Setup sample).
            List<Dictionary<string, object?>> items;
            if (decided.ReceivingPlant)
            {
                items = ready.Select(line => new Dictionary<string, object?>
                {
                    ["Material"] = line.Material,
                    ["Plant"] = sourcePlant,
                    ["StorageLocation"] = sourceStorage,
                    ["GoodsMovementType"] = decided.GoodsMovementType,
                    ["QuantityInEntryUnit"] = FormatQuantity(line.Quantity),
                    ["EntryUnit"] = line.EntryUnit,
                    ["IssuingOrReceivingPlant"] = destinationPlant,
                    ["IssuingOrReceivingStorageLoc"] = destinationStorage
                }).ToList();
            }
            else
            {
                items = ready.Select(line => new Dictionary<string, object?>
                {
                    ["Material"] = line.Material,
                    ["Plant"] = sourcePlant,
                    ["StorageLocation"] = sourceStorage,
                    ["GoodsMovementType"] = decided.GoodsMovementType,
                    ["QuantityInEntryUnit"] = FormatQuantity(line.Quantity),
                    ["EntryUnit"] = line.EntryUnit,
                    ["IssuingOrReceivingStorageLoc"] = destinationStorage
                }).ToList();
            }

            Dictionary<string, object?> header = new Dictionary<string, object?>
            {
                ["PostingDate"] = postingDate,
                ["DocumentDate"] = postingDate,
                ["GoodsMovementCode"] = decided.GoodsMovementCode,
                ["MaterialDocumentHeaderText"] = headerText,
                ["to_MaterialDocumentItem"] = new Dictionary<string, object?> { ["results"] = items }
            };
            if (reference != null)
            {
                header["ReferenceDocument"] = reference;
            }

            body = JsonSerializer.Serialize(header);
            return null;
        }

        /// <summary>
        /// Goods issue or positive adjustment. Compatible lines (same material and SAP unit) are summed.
        /// The quantity is always positive. Header text and cost center are required by SAP for Z01/Z02 account assignment.
        /// When <paramref name="keepEntryUnit"/> is true (POS recipe explosion), QuantityInEntryUnit / EntryUnit stay in the
        /// recipe ingredient UOM (e.g. ML) instead of being forced to the material base UOM.
        /// <paramref name="postingDate"/> defaults to <paramref name="documentDate"/>; POS sets posting to the current date.
        /// </summary>
        public static string? TryBuildEffect(
            DateTime documentDate,
            string? plant,
            string? storageLocation,
            Rule rule,
            IReadOnlyList<TransferLine> lines,
            Dictionary<Guid, List<MaterialUomConversion>> conversions,
            out string? body,
            string? headerText = null,
            string? costCenter = null,
            string? referenceDocument = null,
            string? glAccount = null,
            DateTime? postingDate = null,
            bool keepEntryUnit = false,
            bool allowMissingConversion = false)
        {
            body = null;
            if (string.IsNullOrWhiteSpace(plant) || string.IsNullOrWhiteSpace(storageLocation))
            {
                return INVENTORY_LOCATION_MAPPING_MISSING;
            }

            bool needsCostCenter = rule.GoodsMovementType == TYPE_Z01 || rule.GoodsMovementType == TYPE_Z02;
            if (needsCostCenter && string.IsNullOrWhiteSpace(costCenter))
            {
                return COST_CENTER_MISSING;
            }

            string sapPlant = plant.Trim();
            string sapStorage = storageLocation.Trim();
            string? sapCostCenter = string.IsNullOrWhiteSpace(costCenter) ? null : costCenter.Trim();
            string? sapGlAccount = string.IsNullOrWhiteSpace(glAccount) ? null : glAccount.Trim();
            Dictionary<string, decimal> totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, (string Material, string Unit)> keys = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            foreach (TransferLine line in lines)
            {
                if (line.Quantity <= 0)
                {
                    continue;
                }

                string? convertedProblem = ConvertLine(
                    line.Material,
                    line.Quantity,
                    line.QuantityUom,
                    conversions,
                    out string material,
                    out string unit,
                    out decimal quantity,
                    keepEntryUnit,
                    allowMissingConversion);
                if (convertedProblem != null)
                {
                    return convertedProblem == MATERIAL_NOT_VALID ? MATERIAL_MAPPING_MISSING : convertedProblem;
                }

                string key = material + "|" + unit;
                totals[key] = totals.GetValueOrDefault(key) + quantity;
                keys[key] = (material, unit);
            }

            if (totals.Count == 0)
            {
                return null;
            }

            string sapPostingDate = FormatDate(postingDate ?? documentDate);
            string sapDocumentDate = FormatDate(documentDate);
            string materialDocumentHeaderText = string.IsNullOrWhiteSpace(headerText)
                ? $"SILA {rule.GoodsMovementType} {sapPostingDate[..10]}"
                : headerText.Trim();
            if (materialDocumentHeaderText.Length > 25)
            {
                materialDocumentHeaderText = materialDocumentHeaderText[..25];
            }

            List<Dictionary<string, object?>> items = totals
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x =>
                {
                    Dictionary<string, object?> item = new Dictionary<string, object?>
                    {
                        ["Plant"] = sapPlant,
                        ["StorageLocation"] = sapStorage,
                        ["Material"] = keys[x.Key].Material,
                        ["GoodsMovementType"] = rule.GoodsMovementType,
                        ["QuantityInEntryUnit"] = FormatQuantity(x.Value),
                        ["EntryUnit"] = keys[x.Key].Unit
                    };
                    if (!string.IsNullOrWhiteSpace(rule.GoodsMovementReasonCode))
                    {
                        item["GoodsMovementReasonCode"] = rule.GoodsMovementReasonCode.Trim();
                    }

                    if (sapCostCenter != null)
                    {
                        item["CostCenter"] = sapCostCenter;
                    }

                    if (sapGlAccount != null)
                    {
                        item["GLAccount"] = sapGlAccount;
                    }

                    return item;
                })
                .ToList();

            // OData V2 deep-insert: to_MaterialDocumentItem.results[...]
            Dictionary<string, object?> header = new Dictionary<string, object?>
            {
                ["PostingDate"] = sapPostingDate,
                ["DocumentDate"] = sapDocumentDate,
                ["GoodsMovementCode"] = rule.GoodsMovementCode,
                ["MaterialDocumentHeaderText"] = materialDocumentHeaderText,
                ["to_MaterialDocumentItem"] = new Dictionary<string, object?> { ["results"] = items }
            };
            if (!string.IsNullOrWhiteSpace(referenceDocument))
            {
                string reference = referenceDocument.Trim();
                header["ReferenceDocument"] = reference.Length > 16 ? reference[..16] : reference;
            }

            body = JsonSerializer.Serialize(header);
            return null;
        }

        /// <summary>
        /// SAP Material Document deep-insert must target A_MaterialDocumentHeader. Configurations that only store the
        /// service root still post to the entity set.
        /// </summary>
        public static string? EnsureMaterialDocumentPath(string? resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            string path = resourcePath.Trim().TrimEnd('/');
            if (path.Contains(MATERIAL_DOCUMENT_ENTITY, StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (path.EndsWith("API_MATERIAL_DOCUMENT_SRV", StringComparison.OrdinalIgnoreCase))
            {
                return path + "/" + MATERIAL_DOCUMENT_ENTITY;
            }

            return path;
        }

        public static string? ReadMaterialDocument(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using JsonDocument json = JsonDocument.Parse(payload);
                JsonElement root = json.RootElement;
                if (root.ValueKind == JsonValueKind.Object
                    && root.TryGetProperty("d", out JsonElement wrapped)
                    && wrapped.ValueKind == JsonValueKind.Object)
                {
                    root = wrapped;
                }

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                string? number = Text(root, "MaterialDocument")
                    ?? Text(root, "materialDocument")
                    ?? Text(root, "materialDocumentNumber");
                if (string.IsNullOrWhiteSpace(number))
                {
                    return null;
                }

                string? year = Text(root, "MaterialDocumentYear") ?? Text(root, "materialDocumentYear");
                return string.IsNullOrWhiteSpace(year) ? number : number + "/" + year;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>True when this movement's material document was already stored on the posting.</summary>
        public static bool ReferenceHas(string? erpReference, string movementType)
        {
            if (string.IsNullOrWhiteSpace(erpReference))
            {
                return false;
            }

            return erpReference.Contains(movementType + ":", StringComparison.OrdinalIgnoreCase);
        }

        public static string JoinReference(string? existing, string movementType, string document)
        {
            string piece = movementType + ":" + document;
            if (string.IsNullOrWhiteSpace(existing))
            {
                return piece;
            }

            if (existing.Contains(piece, StringComparison.OrdinalIgnoreCase))
            {
                return existing;
            }

            return existing + "; " + piece;
        }

        /// <summary>The named active API for the company code, otherwise ALL. No other API is returned.</summary>
        public static async Task<ApiIntegrationConfiguration?> ResolveAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            string apiName,
            string? companyCode,
            CancellationToken cancellationToken)
        {
            return await ResolveAnyAsync(repository, organizationId, new[] { apiName }, companyCode, cancellationToken);
        }

        /// <summary>
        /// Resolves the first ACTIVE configuration whose name matches any of <paramref name="apiNames"/> (case-insensitive),
        /// preferring the company code then ALL.
        /// </summary>
        public static async Task<ApiIntegrationConfiguration?> ResolveAnyAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            IReadOnlyList<string> apiNames,
            string? companyCode,
            CancellationToken cancellationToken)
        {
            List<ApiIntegrationConfiguration> matches = await LoadNamedAsync(repository, organizationId, apiNames, cancellationToken);
            List<ApiIntegrationConfiguration> active = matches
                .Where(x => x.Status == IntegrationConfigurationStatus.ACTIVE)
                .ToList();
            return PickForCompany(active, companyCode);
        }

        /// <summary>
        /// Transfer posting resolver: name aliases first (FIVE / S4), then process-type fallback
        /// (POST_TRANSFER_311 / POST_TRANSFER_301) so rename drift cannot block ITO posting.
        /// </summary>
        public static async Task<ApiIntegrationConfiguration?> ResolveForRuleAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            Rule rule,
            string? companyCode,
            CancellationToken cancellationToken)
        {
            ApiIntegrationConfiguration? byName = await ResolveAnyAsync(
                repository, organizationId, ApiNamesFor(rule), companyCode, cancellationToken);
            if (byName != null)
            {
                return byName;
            }

            string? processType = ProcessTypeFor(rule);
            if (processType == null)
            {
                return null;
            }

            List<ApiIntegrationConfiguration> rows = await repository.ApiIntegrationConfiguration
                .FindByCondition(x => IntegrationLookup.BuyerIdsOf(repository, organizationId).Contains(x.BuyerId)
                    && x.IsActive
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.ProcessType == processType)
                .ToListAsync(cancellationToken);
            return PickForCompany(rows, companyCode);
        }

        /// <summary>Integration Setup process_type for transfer rules (311/312 or 301/302).</summary>
        public static string? ProcessTypeFor(Rule rule)
        {
            if (TransferApiNames.Any(x => string.Equals(x, rule.ApiName, StringComparison.OrdinalIgnoreCase))
                || string.Equals(rule.GoodsMovementType, TYPE_311, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.GoodsMovementType, TYPE_312, StringComparison.OrdinalIgnoreCase))
            {
                return "POST_TRANSFER_311";
            }

            if (PlantTransferApiNames.Any(x => string.Equals(x, rule.ApiName, StringComparison.OrdinalIgnoreCase))
                || string.Equals(rule.GoodsMovementType, TYPE_301, StringComparison.OrdinalIgnoreCase)
                || string.Equals(rule.GoodsMovementType, TYPE_302, StringComparison.OrdinalIgnoreCase))
            {
                return "POST_TRANSFER_301";
            }

            return null;
        }

        /// <summary>
        /// Like <see cref="ResolveAnyAsync"/> but also returns an inactive match so the caller can report
        /// SAP_API_CONFIGURATION_INACTIVE instead of NOT_FOUND.
        /// </summary>
        public static async Task<(ApiIntegrationConfiguration? Active, ApiIntegrationConfiguration? Inactive)> ResolveWithInactiveAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            IReadOnlyList<string> apiNames,
            string? companyCode,
            CancellationToken cancellationToken)
        {
            List<ApiIntegrationConfiguration> matches = await LoadNamedAsync(repository, organizationId, apiNames, cancellationToken);
            ApiIntegrationConfiguration? active = PickForCompany(
                matches.Where(x => x.Status == IntegrationConfigurationStatus.ACTIVE).ToList(),
                companyCode);
            if (active != null)
            {
                return (active, null);
            }

            ApiIntegrationConfiguration? inactive = PickForCompany(matches, companyCode)
                ?? matches.OrderBy(x => x.DateCreated).FirstOrDefault();
            return (null, inactive);
        }

        private static async Task<List<ApiIntegrationConfiguration>> LoadNamedAsync(
            IRepositoryWrapper repository,
            Guid organizationId,
            IReadOnlyList<string> apiNames,
            CancellationToken cancellationToken)
        {
            if (apiNames.Count == 0)
            {
                return new List<ApiIntegrationConfiguration>();
            }

            HashSet<string> wanted = apiNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<ApiIntegrationConfiguration> rows = await repository.ApiIntegrationConfiguration
                .FindByCondition(x => IntegrationLookup.BuyerIdsOf(repository, organizationId).Contains(x.BuyerId) && x.IsActive)
                .ToListAsync(cancellationToken);
            return rows.Where(x => wanted.Contains(x.Name)).ToList();
        }

        private static ApiIntegrationConfiguration? PickForCompany(List<ApiIntegrationConfiguration> named, string? companyCode)
        {
            return named.FirstOrDefault(x => companyCode != null && x.EntityCode.Equals(companyCode, StringComparison.OrdinalIgnoreCase))
                ?? named.FirstOrDefault(x => x.EntityCode.Equals(IntegrationConstants.ENTITY_CODE_ALL, StringComparison.OrdinalIgnoreCase))
                ?? (companyCode == null ? named.OrderBy(x => x.DateCreated).FirstOrDefault() : null);
        }

        private static string? ConvertLine(
            ItemBuyerMaster material,
            decimal quantity,
            string? quantityUom,
            Dictionary<Guid, List<MaterialUomConversion>> conversions,
            out string materialCode,
            out string entryUnit,
            out decimal converted,
            bool keepEntryUnit = false,
            bool allowMissingConversion = false)
        {
            materialCode = string.Empty;
            entryUnit = string.Empty;
            converted = 0;
            if (!material.IsActive || !SilaMaterialRules.HoldsStock(material) || string.IsNullOrWhiteSpace(material.MaterialCode))
            {
                return MATERIAL_NOT_VALID;
            }

            string? baseUnit = EntryUnitOf(material.BaseUnitOfMeasure);
            if (baseUnit == null && !allowMissingConversion)
            {
                return UOM_CONVERSION_MISSING;
            }

            decimal? baseQuantity = UomConverter.TryToBase(material, quantity, quantityUom, conversions);
            if ((baseQuantity == null || baseQuantity.Value <= 0) && !(keepEntryUnit && allowMissingConversion && quantity > 0))
            {
                return UOM_CONVERSION_MISSING;
            }

            materialCode = FormatSapMaterial(material.MaterialCode.Trim());
            if (keepEntryUnit)
            {
                // POS recipe explosion: keep the recipe ingredient UOM/qty (e.g. 30 ML) for SAP.
                string? requested = EntryUnitOf(quantityUom) ?? baseUnit ?? "EA";
                entryUnit = requested;
                converted = Math.Abs(quantity);
                return null;
            }

            entryUnit = baseUnit ?? "EA";
            converted = baseQuantity!.Value;
            return null;
        }

        /// <summary>SAP material numbers are often 18-character zero-padded numerics.</summary>
        public static string FormatSapMaterial(string materialCode)
        {
            string code = materialCode.Trim();
            if (code.Length > 0 && code.Length < 18 && code.All(char.IsDigit))
            {
                return code.PadLeft(18, '0');
            }

            return code;
        }

        private static string? Text(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out JsonElement value) || value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
            {
                return null;
            }

            string text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static bool Same(string left, string right)
        {
            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public sealed class Rule
        {
            public Rule(
                string apiName,
                string goodsMovementCode,
                string goodsMovementType,
                bool receivingPlant,
                bool increasesStock = false,
                string? goodsMovementReasonCode = null)
            {
                ApiName = apiName;
                GoodsMovementCode = goodsMovementCode;
                GoodsMovementType = goodsMovementType;
                ReceivingPlant = receivingPlant;
                IncreasesStock = increasesStock;
                GoodsMovementReasonCode = goodsMovementReasonCode;
            }

            public string ApiName { get; }
            public string GoodsMovementCode { get; }
            public string GoodsMovementType { get; }
            public bool ReceivingPlant { get; }
            /// <summary>True when the movement restores/increases stock (ledger IN). False for consumption/issue (ledger OUT).</summary>
            public bool IncreasesStock { get; }
            /// <summary>Optional SAP GoodsMovementReasonCode (e.g. 0001 for wastage).</summary>
            public string? GoodsMovementReasonCode { get; }
        }

        public readonly record struct TransferLine(ItemBuyerMaster Material, decimal Quantity, string? QuantityUom);

        private readonly record struct TransferQuantity(string Material, string EntryUnit, decimal Quantity);
    }
}
