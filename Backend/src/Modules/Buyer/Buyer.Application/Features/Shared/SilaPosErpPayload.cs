using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Ensures the POS transaction tracker always has the SAP request body that was (or would be) sent
    /// via FIVE_POS_UPDATE — including backfill for older failures that only kept the HTTP error text.
    /// </summary>
    public static class SilaPosErpPayload
    {
        /// <summary>
        /// Returns the sanitised request payload, rebuilding and persisting it from ledger lines when missing.
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
                return null;
            }

            string? existing = SilaErpPayload.Sanitise(posting.ErpRequestPayload) ?? posting.ErpRequestPayload;
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            string? rebuilt = await RebuildRequestAsync(repository, buyerId, sale, posting, cancellationToken);
            if (string.IsNullOrWhiteSpace(rebuilt))
            {
                return null;
            }

            posting.ErpRequestPayload = rebuilt;
            if (string.IsNullOrWhiteSpace(posting.ErpResponsePayload))
            {
                posting.ErpResponsePayload = SilaErpPayload.HttpBodyFromError(sale.FailureMessage ?? posting.ErrorMessage);
            }

            repository.InventoryErpPosting.Update(posting);
            await repository.SaveAsync();
            return rebuilt;
        }

        /// <summary>Rebuilds the material-document JSON from RECIPE_CONSUMPTION lines + outlet/plant/CC setup.</summary>
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
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(repository, materialIds, cancellationToken);

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

            string plant = string.IsNullOrWhiteSpace(property?.PlantCode) ? "UNMAPPED" : property!.PlantCode.Trim();
            string storage = !string.IsNullOrWhiteSpace(location?.StorageLocationCode)
                ? location!.StorageLocationCode!.Trim()
                : !string.IsNullOrWhiteSpace(location?.LocationCode)
                    ? location!.LocationCode!.Trim()
                    : "UNMAPPED";
            string? costCenter = string.IsNullOrWhiteSpace(location?.CostCenter) ? "UNMAPPED" : location!.CostCenter!.Trim();

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
            if (problem == null && !string.IsNullOrWhiteSpace(body))
            {
                return SilaErpPayload.Sanitise(body) ?? body;
            }

            // Last resort: still show the lines that went (or would go) to SAP even if conversion failed.
            return SilaErpPayload.Sanitise(BuildFallbackBody(documentDate, postingDate, plant, storage, costCenter, rule, transferLines, location?.GlAccount, posting.ReferenceNumber, sale.SourceTransactionId));
        }

        private static string BuildFallbackBody(
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
                string material = SilaInventorySap.FormatSapMaterial(line.Material.MaterialCode?.Trim() ?? line.Material.Id.ToString("N"));
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

            return JsonSerializer.Serialize(header);
        }
    }
}
