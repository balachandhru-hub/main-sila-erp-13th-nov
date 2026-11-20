using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// POS → FIVE_POS_UPDATE exchange for the Transaction Tracker.
    /// Prefer the HTTP-captured body; when missing, rebuild the same OData material-document JSON from RECIPE_CONSUMPTION
    /// ledger lines (never SILA sale-metadata objects with saleId/posCode).
    /// </summary>
    public static class SilaPosErpPayload
    {
        /// <summary>Shown only when neither capture nor ledger rebuild can produce a SAP material-document body.</summary>
        public const string NOT_CAPTURED = "Actual SAP request was not captured for this attempt.";

        /// <summary>
        /// Returns the SAP request JSON: captured HTTP body first, otherwise rebuilt from ledger and persisted.
        /// </summary>
        public static async Task<string?> EnsureRequestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            PosSalesTransaction sale,
            InventoryErpPosting? posting,
            CancellationToken cancellationToken)
        {
            if (posting == null)
            {
                return NOT_CAPTURED;
            }

            if (IsCapturedSapBody(posting.ErpRequestPayload))
            {
                return Store(posting.ErpRequestPayload!) ?? posting.ErpRequestPayload!.Trim();
            }

            string? rebuilt;
            try
            {
                rebuilt = await RebuildRequestAsync(repository, buyerId, sale, posting, cancellationToken);
            }
            catch (Exception)
            {
                rebuilt = null;
            }

            if (!IsCapturedSapBody(rebuilt))
            {
                return NOT_CAPTURED;
            }

            string stored = Store(rebuilt!) ?? rebuilt!.Trim();
            posting.ErpRequestPayload = stored;
            if (string.IsNullOrWhiteSpace(posting.ErpResponsePayload))
            {
                string? response = SilaErpPayload.HttpBodyFromError(sale.FailureMessage ?? posting.ErrorMessage);
                if (!string.IsNullOrWhiteSpace(response))
                {
                    posting.ErpResponsePayload = response;
                }
            }

            try
            {
                InventoryErpPosting? tracked = await repository.InventoryErpPosting
                    .FindFirstByConditionAsync(x => x.Id == posting.Id && x.BuyerId == buyerId);
                if (tracked != null)
                {
                    tracked.ErpRequestPayload = stored;
                    if (!string.IsNullOrWhiteSpace(posting.ErpResponsePayload)
                        && string.IsNullOrWhiteSpace(tracked.ErpResponsePayload))
                    {
                        tracked.ErpResponsePayload = posting.ErpResponsePayload;
                    }

                    tracked.MovementType = SilaInventorySap.TYPE_Z01;
                    tracked.IntegrationSystem = SilaStockCountSap.API_CONFIG_NAME;
                    repository.InventoryErpPosting.Update(tracked);
                }
                else
                {
                    posting.MovementType = SilaInventorySap.TYPE_Z01;
                    posting.IntegrationSystem = SilaStockCountSap.API_CONFIG_NAME;
                    repository.InventoryErpPosting.Update(posting);
                }

                await repository.SaveAsync();
            }
            catch (Exception)
            {
                // Still return the rebuilt SAP body for download even if persistence fails.
            }

            return stored;
        }

        /// <summary>Sync read of stored capture only (no rebuild).</summary>
        public static string ReadCapturedRequest(InventoryErpPosting? posting)
        {
            string? stored = posting?.ErpRequestPayload;
            if (IsCapturedSapBody(stored))
            {
                return Store(stored!) ?? stored!.Trim();
            }

            return NOT_CAPTURED;
        }

        /// <summary>
        /// True when the text is OData material-document JSON — not SILA diagnostic / UI-synthesized metadata.
        /// </summary>
        public static bool IsCapturedSapBody(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            string text = body.Trim();
            if (string.Equals(text, NOT_CAPTURED, StringComparison.Ordinal))
            {
                return false;
            }

            if (text.Contains("SILA built this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA UI built this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA rebuilt this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA recorded a diagnostic", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Reject SILA-only metadata objects that lack the OData deep-insert shape.
            if (text.Contains("\"saleId\"", StringComparison.Ordinal)
                && text.Contains("\"posCode\"", StringComparison.Ordinal)
                && !text.Contains("to_MaterialDocumentItem", StringComparison.Ordinal))
            {
                return false;
            }

            return text.Contains("to_MaterialDocumentItem", StringComparison.Ordinal)
                && text.Contains("GoodsMovementCode", StringComparison.Ordinal);
        }

        /// <summary>
        /// Persists the exact body that is about to be (or was) sent to SAP.
        /// Empty / non-SAP bodies become <see cref="NOT_CAPTURED"/>.
        /// </summary>
        public static string RememberRequest(InventoryErpPosting posting, string? requestBody)
        {
            if (IsCapturedSapBody(requestBody))
            {
                string stored = Store(requestBody!) ?? requestBody!.Trim();
                posting.ErpRequestPayload = stored;
                return stored;
            }

            posting.ErpRequestPayload = NOT_CAPTURED;
            return NOT_CAPTURED;
        }

        /// <summary>
        /// Rebuilds the FIVE_POS_UPDATE material-document JSON from RECIPE_CONSUMPTION lines + outlet/plant/CC setup.
        /// Returns pure SAP OData JSON only.
        /// </summary>
        public static async Task<string?> RebuildRequestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            PosSalesTransaction sale,
            InventoryErpPosting posting,
            CancellationToken cancellationToken)
        {
            List<InventoryTransaction> lines = await repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_POS_SALE
                    && x.ReferenceId == sale.Id
                    && x.Quantity > 0)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0)
            {
                return null;
            }

            List<Guid> materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
            Dictionary<Guid, MaterialEntity> materials = await repository.ItemBuyerMaster
                .FindByCondition(x => materialIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> conversions =
                await UomConverter.GetConversionsAsync(repository, materialIds, cancellationToken);

            List<SilaInventorySap.TransferLine> transferLines = new List<SilaInventorySap.TransferLine>();
            foreach (InventoryTransaction line in lines)
            {
                if (!materials.TryGetValue(line.MaterialId, out MaterialEntity? material))
                {
                    continue;
                }

                if (line.EnteredQuantity > 0 && !string.IsNullOrWhiteSpace(line.EnteredUom))
                {
                    transferLines.Add(new SilaInventorySap.TransferLine(material, line.EnteredQuantity, line.EnteredUom));
                }
                else
                {
                    transferLines.Add(new SilaInventorySap.TransferLine(material, line.Quantity, line.BaseUom));
                }
            }

            if (transferLines.Count == 0)
            {
                return null;
            }

            InventoryLocation? location = posting.LocationId == null
                ? (sale.OutletLocationId == null
                    ? null
                    : await repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == sale.OutletLocationId.Value))
                : await repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == posting.LocationId.Value);
            BuyerProperty? property = location == null
                ? null
                : await repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == location.PropertyId);

            string? plant = string.IsNullOrWhiteSpace(property?.PlantCode) ? null : property!.PlantCode.Trim();
            string? storage = !string.IsNullOrWhiteSpace(location?.StorageLocationCode)
                ? location!.StorageLocationCode!.Trim()
                : location?.LocationCode?.Trim();
            string? costCenter = string.IsNullOrWhiteSpace(location?.CostCenter) ? null : location!.CostCenter!.Trim();
            if (string.IsNullOrWhiteSpace(plant) || string.IsNullOrWhiteSpace(storage) || string.IsNullOrWhiteSpace(costCenter))
            {
                // Still build a SAP-shaped body for tracker download when mappings are incomplete —
                // use placeholders only inside the SAP structure (not SILA sale metadata).
                plant = string.IsNullOrWhiteSpace(plant) ? "UNMAPPED" : plant;
                storage = string.IsNullOrWhiteSpace(storage) ? "UNMAPPED" : storage;
                costCenter = string.IsNullOrWhiteSpace(costCenter) ? "UNMAPPED" : costCenter;
            }

            bool isReversal = SilaPosReversal.IsReversal(sale.TransactionType);
            SilaInventorySap.Rule rule = isReversal ? SilaInventorySap.PosReversal : SilaInventorySap.PosConsumption;
            DateTime documentDate = sale.BusinessDate == default ? DateTime.UtcNow.Date : sale.BusinessDate.Date;
            DateTime postingDate = posting.DateUpdated != default ? posting.DateUpdated.Date : DateTime.UtcNow.Date;

            string? problem = SilaInventorySap.TryBuildEffect(
                documentDate,
                plant,
                storage,
                rule,
                transferLines,
                conversions,
                out string? body,
                headerText: SilaInventorySap.BuildPosHeaderText(posting.ReferenceNumber),
                costCenter: costCenter,
                referenceDocument: SilaInventorySap.BuildPosReferenceDocument(storage, sale.SourceTransactionId),
                glAccount: location?.GlAccount,
                postingDate: postingDate,
                keepEntryUnit: true,
                allowMissingConversion: true);
            if (problem == null && IsCapturedSapBody(body))
            {
                return Store(body) ?? body;
            }

            return BuildFallbackBody(
                documentDate,
                postingDate,
                plant!,
                storage!,
                costCenter,
                rule,
                transferLines,
                location?.GlAccount,
                posting.ReferenceNumber,
                sale.SourceTransactionId);
        }

        public static string? Store(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return SilaErpPayload.SanitiseForPersist(body) ?? body.Trim();
        }

        public static string? StoreResponse(string? body) => Store(body);

        private static string? BuildFallbackBody(
            DateTime documentDate,
            DateTime postingDate,
            string plant,
            string storage,
            string? costCenter,
            SilaInventorySap.Rule rule,
            IReadOnlyList<SilaInventorySap.TransferLine> lines,
            string? glAccount,
            string? referenceNumber,
            string? sourceTransactionId)
        {
            List<Dictionary<string, object?>> items = new List<Dictionary<string, object?>>();
            foreach (SilaInventorySap.TransferLine line in lines.Where(x => x.Quantity > 0))
            {
                string material = SilaInventorySap.FormatSapMaterial(
                    line.Material.MaterialCode?.Trim() ?? line.Material.Id.ToString("N"));
                string unit = string.IsNullOrWhiteSpace(line.QuantityUom)
                    ? (line.Material.BaseUnitOfMeasure ?? "EA")
                    : line.QuantityUom.Trim().ToUpperInvariant();
                Dictionary<string, object?> item = new Dictionary<string, object?>
                {
                    ["Plant"] = plant,
                    ["StorageLocation"] = storage,
                    ["Material"] = material,
                    ["GoodsMovementType"] = rule.GoodsMovementType,
                    ["QuantityInEntryUnit"] = SilaInventorySap.FormatQuantity(Math.Abs(line.Quantity)),
                    ["EntryUnit"] = unit
                };
                if (!string.IsNullOrWhiteSpace(costCenter))
                {
                    item["CostCenter"] = costCenter;
                }

                if (!string.IsNullOrWhiteSpace(glAccount))
                {
                    item["GLAccount"] = glAccount.Trim();
                }

                items.Add(item);
            }

            if (items.Count == 0)
            {
                return null;
            }

            string headerText = SilaInventorySap.BuildPosHeaderText(referenceNumber);
            if (headerText.Length > 25)
            {
                headerText = headerText[..25];
            }

            Dictionary<string, object?> header = new Dictionary<string, object?>
            {
                ["PostingDate"] = SilaInventorySap.FormatDate(postingDate),
                ["DocumentDate"] = SilaInventorySap.FormatDate(documentDate),
                ["GoodsMovementCode"] = rule.GoodsMovementCode,
                ["MaterialDocumentHeaderText"] = headerText,
                ["to_MaterialDocumentItem"] = new Dictionary<string, object?> { ["results"] = items }
            };
            string reference = SilaInventorySap.BuildPosReferenceDocument(storage, sourceTransactionId);
            if (!string.IsNullOrWhiteSpace(reference))
            {
                header["ReferenceDocument"] = reference.Length > 16 ? reference[..16] : reference;
            }

            string json = JsonSerializer.Serialize(header);
            return IsCapturedSapBody(json) ? json : null;
        }
    }
}
