using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// POS → FIVE_POS_UPDATE request for the Transaction Tracker.
    /// Prefer HTTP-captured body; else rebuild OData JSON from RECIPE_CONSUMPTION ledger lines;
    /// else explode the approved recipe. Never returns SILA sale-metadata (saleId/posCode) as the SAP body.
    /// </summary>
    public static class SilaPosErpPayload
    {
        public const string NOT_CAPTURED = "Actual SAP request was not captured for this attempt.";

        /// <summary>
        /// Returns SAP request JSON for the tracker. Works even when <paramref name="posting"/> is null.
        /// </summary>
        public static async Task<string> EnsureRequestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            PosSalesTransaction sale,
            InventoryErpPosting? posting,
            CancellationToken cancellationToken)
        {
            if (posting != null && IsCapturedSapBody(posting.ErpRequestPayload))
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
            if (posting != null)
            {
                posting.ErpRequestPayload = stored;
                posting.MovementType = SilaInventorySap.TYPE_Z01;
                posting.IntegrationSystem = SilaStockCountSap.API_CONFIG_NAME;
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
                        tracked.MovementType = SilaInventorySap.TYPE_Z01;
                        tracked.IntegrationSystem = SilaStockCountSap.API_CONFIG_NAME;
                        if (!string.IsNullOrWhiteSpace(posting.ErpResponsePayload)
                            && string.IsNullOrWhiteSpace(tracked.ErpResponsePayload))
                        {
                            tracked.ErpResponsePayload = posting.ErpResponsePayload;
                        }

                        repository.InventoryErpPosting.Update(tracked);
                    }
                    else
                    {
                        repository.InventoryErpPosting.Update(posting);
                    }

                    await repository.SaveAsync();
                }
                catch (Exception)
                {
                    // Still return the rebuilt body for download.
                }
            }

            return stored;
        }

        public static string ReadCapturedRequest(InventoryErpPosting? posting)
        {
            string? stored = posting?.ErpRequestPayload;
            if (IsCapturedSapBody(stored))
            {
                return Store(stored!) ?? stored!.Trim();
            }

            return NOT_CAPTURED;
        }

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

            if (text.Contains("\"saleId\"", StringComparison.Ordinal)
                && text.Contains("\"posCode\"", StringComparison.Ordinal)
                && !text.Contains("to_MaterialDocumentItem", StringComparison.Ordinal))
            {
                return false;
            }

            return text.Contains("to_MaterialDocumentItem", StringComparison.Ordinal)
                && text.Contains("GoodsMovementCode", StringComparison.Ordinal);
        }

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
        /// Builds FIVE_POS_UPDATE OData JSON from ledger lines, or recipe explosion when ledger is empty.
        /// </summary>
        public static async Task<string?> RebuildRequestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            PosSalesTransaction sale,
            InventoryErpPosting? posting,
            CancellationToken cancellationToken)
        {
            List<SilaInventorySap.TransferLine> transferLines = await LoadLedgerTransferLinesAsync(
                repository, buyerId, sale.Id, cancellationToken);

            if (transferLines.Count == 0)
            {
                (List<SilaInventorySap.TransferLine> exploded, string? explodeError) =
                    await SilaPosProcessing.ExplodeTransferLinesAsync(repository, buyerId, sale, cancellationToken);
                _ = explodeError;
                transferLines = exploded;
            }

            if (transferLines.Count == 0)
            {
                return null;
            }

            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                repository,
                transferLines.Select(x => x.Material.Id).Distinct().ToList(),
                cancellationToken);

            InventoryLocation? location = null;
            if (posting?.LocationId != null)
            {
                location = await repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == posting.LocationId.Value);
            }

            if (location == null && sale.OutletLocationId != null)
            {
                location = await repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == sale.OutletLocationId.Value);
            }

            if (location == null && !string.IsNullOrWhiteSpace(sale.OutletCode))
            {
                string outlet = sale.OutletCode.Trim();
                location = await repository.InventoryLocation
                    .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                        && (x.LocationCode == outlet || x.StorageLocationCode == outlet))
                    .OrderByDescending(x => x.StorageLocationCode == outlet)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            BuyerProperty? property = location == null
                ? null
                : await repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == location.PropertyId);

            string plant = string.IsNullOrWhiteSpace(property?.PlantCode) ? "UNMAPPED" : property!.PlantCode.Trim();
            string storage = !string.IsNullOrWhiteSpace(location?.StorageLocationCode)
                ? location!.StorageLocationCode!.Trim()
                : !string.IsNullOrWhiteSpace(location?.LocationCode)
                    ? location!.LocationCode!.Trim()
                    : !string.IsNullOrWhiteSpace(sale.OutletCode)
                        ? sale.OutletCode.Trim()
                        : "UNMAPPED";
            string costCenter = string.IsNullOrWhiteSpace(location?.CostCenter) ? "UNMAPPED" : location!.CostCenter!.Trim();

            bool isReversal = SilaPosReversal.IsReversal(sale.TransactionType);
            SilaInventorySap.Rule rule = isReversal ? SilaInventorySap.PosReversal : SilaInventorySap.PosConsumption;
            DateTime documentDate = sale.BusinessDate == default ? DateTime.UtcNow.Date : sale.BusinessDate.Date;
            DateTime postingDate = posting != null && posting.DateUpdated != default
                ? posting.DateUpdated.Date
                : DateTime.UtcNow.Date;
            string? referenceNumber = posting?.ReferenceNumber ?? sale.SourceTransactionId;

            string? problem = SilaInventorySap.TryBuildEffect(
                documentDate,
                plant,
                storage,
                rule,
                transferLines,
                conversions,
                out string? body,
                headerText: SilaInventorySap.BuildPosHeaderText(referenceNumber),
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
                plant,
                storage,
                costCenter,
                rule,
                transferLines,
                location?.GlAccount,
                referenceNumber,
                sale.SourceTransactionId);
        }

        private static async Task<List<SilaInventorySap.TransferLine>> LoadLedgerTransferLinesAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid saleId,
            CancellationToken cancellationToken)
        {
            // Include inactive rows — failed/reprocessed sales may soft-delete ledger lines.
            List<InventoryTransaction> lines = await repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyerId
                    && x.ReferenceType == Common.SILA_REF_POS_SALE
                    && x.ReferenceId == saleId
                    && (x.Quantity > 0 || x.EnteredQuantity > 0))
                .ToListAsync(cancellationToken);
            if (lines.Count == 0)
            {
                return new List<SilaInventorySap.TransferLine>();
            }

            List<Guid> materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
            Dictionary<Guid, MaterialEntity> materials = await repository.ItemBuyerMaster
                .FindByCondition(x => materialIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

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
                else if (line.Quantity > 0)
                {
                    transferLines.Add(new SilaInventorySap.TransferLine(material, line.Quantity, line.BaseUom));
                }
            }

            return transferLines;
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
