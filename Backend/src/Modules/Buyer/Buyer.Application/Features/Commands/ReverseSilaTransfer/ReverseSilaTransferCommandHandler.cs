using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Entities;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ReverseSilaTransfer
{
    /// <summary>
    /// Queues an explicit SAP reversal for a successfully posted ITO:
    /// 311 → 312 via FIVE S4 - Transfer Stock, or 301 → 302 via FIVE S4 - Plant Transfer Stock.
    /// Local stock moves only after confirmed SAP success in PostPendingErpPostings.
    /// </summary>
    public class ReverseSilaTransferCommandHandler : IRequestHandler<ReverseSilaTransferCommand, SilaTransferReverseResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ReverseSilaTransferCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaTransferReverseResultDto> Handle(ReverseSilaTransferCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(ReverseSilaTransferCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<SilaTransferReverseResultDto> HandleOnceAsync(ReverseSilaTransferCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Request.Reason))
            {
                throw new BadRequestCustomException("Reversal reason is required.", "Enter a reversal reason before posting movement type 312 or 302.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            InternalTransferOrder transfer = await SilaTransferRules.GetTransferAsync(_repository, _logger, buyer.Id, request.TransferId);
            List<Guid> locationIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, request.UserId, request.RoleId, cancellationToken);
            if (!locationIds.Contains(transfer.FromLocationId) && !locationIds.Contains(transfer.ToLocationId))
            {
                throw new ForBiddenCustomException("No access to this transfer.", "Reverse transfers only for locations you are assigned to.");
            }

            if (!SilaTransferSap.MovesStock(transfer.Status) && transfer.Status != Common.SILA_ITO_PARTIALLY_REVERSED)
            {
                throw new BadRequestCustomException(
                    "Transfer is not posted.",
                    "Unposted ITOs must be cancelled. Reversal applies only to successful 311 or 301 postings.");
            }

            if (string.IsNullOrWhiteSpace(transfer.SapMaterialDocument))
            {
                InventoryErpPosting? originalPosted = await _repository.InventoryErpPosting
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                        && x.ReferenceType == Common.SILA_REF_ITO && x.ReferenceId == transfer.Id
                        && x.Status == Common.SILA_POSTING_POSTED
                        && (x.MovementType == SilaInventorySap.TYPE_311 || x.MovementType == SilaInventorySap.TYPE_301))
                    .OrderByDescending(x => x.PostedOn)
                    .FirstOrDefaultAsync(cancellationToken);
                if (originalPosted == null || string.IsNullOrWhiteSpace(originalPosted.ErpReference))
                {
                    throw new BadRequestCustomException(
                        "Original SAP material document missing.",
                        "A confirmed 311 or 301 material document is required before reversal.");
                }

                transfer.SapMaterialDocument = originalPosted.ErpReference;
            }

            if (transfer.ReversalStatus == Common.SILA_ITO_REVERSAL_PENDING
                || transfer.ReversalStatus == Common.SILA_ITO_REVERSAL_UNKNOWN)
            {
                throw new BadRequestCustomException(
                    "Reversal already in progress.",
                    "Reconcile the pending or unknown reversal before requesting another.");
            }

            List<InternalTransferOrderItem> items = await _repository.InternalTransferOrderItem
                .FindByCondition(x => x.InternalTransferOrderId == transfer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, decimal> requested = request.Request.Items?
                .Where(x => x.Quantity > 0)
                .GroupBy(x => x.ItemId)
                .ToDictionary(x => x.Key, x => x.Sum(i => i.Quantity))
                ?? new Dictionary<Guid, decimal>();

            List<(InternalTransferOrderItem Item, decimal Quantity)> lines = new();
            foreach (InternalTransferOrderItem item in items)
            {
                decimal postedQty = item.ReceivedQty > 0 ? item.ReceivedQty : item.DispatchedQty;
                decimal remaining = Math.Max(0, postedQty - item.ReversedQty - item.PendingReverseQty);
                if (remaining <= 0)
                {
                    continue;
                }

                decimal qty = requested.Count == 0 ? remaining : requested.GetValueOrDefault(item.Id);
                if (qty <= 0)
                {
                    continue;
                }

                if (qty > remaining + 0.0001m)
                {
                    throw new BadRequestCustomException(
                        "Reversal quantity too high.",
                        $"Only {remaining} {item.Uom} remains reversible for {item.MaterialCode}.");
                }

                InventoryBalance? destBalance = await _repository.InventoryBalance
                    .FindFirstByConditionAsync(x => x.LocationId == transfer.ToLocationId && x.MaterialId == item.MaterialId && x.IsActive);
                if (destBalance == null || destBalance.OnHandQty + 0.0001m < qty)
                {
                    throw new BadRequestCustomException(
                        "Insufficient destination stock.",
                        $"{item.MaterialCode} does not have enough stock at the original destination to reverse {qty} {item.Uom}.");
                }

                lines.Add((item, qty));
            }

            if (lines.Count == 0)
            {
                throw new BadRequestCustomException("Nothing to reverse.", "No remaining reversible quantity on this ITO.");
            }

            InventoryLocation? from = await _repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == transfer.FromLocationId);
            InventoryLocation? to = await _repository.InventoryLocation.FindFirstByConditionAsync(x => x.Id == transfer.ToLocationId);
            BuyerProperty? fromProperty = from == null ? null : await _repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == from.PropertyId);
            BuyerProperty? toProperty = to == null ? null : await _repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == to.PropertyId);
            List<Guid> materialIds = lines.Select(x => x.Item.MaterialId).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => materialIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(_repository, materialIds, cancellationToken);
            DateTime postingDate = request.Request.PostingDate?.Date ?? DateTime.UtcNow.Date;
            string? problem = SilaTransferSap.TryPrepareReversal(
                transfer, from, to, fromProperty, toProperty, lines, materials, conversions, postingDate, out string? body, out SilaInventorySap.Rule? rule);
            if (problem != null || body == null || rule == null)
            {
                throw new BadRequestCustomException(problem ?? "REVERSAL_BUILD_FAILED", SilaTransferSap.Describe(problem ?? "REVERSAL_BUILD_FAILED"));
            }

            string? companyCode = string.IsNullOrWhiteSpace(fromProperty?.CompanyCode) ? null : fromProperty.CompanyCode.Trim();
            // Resolve FIVE / S4 Transfer Stock aliases the same way as queue + ERP posting (mobile and cloud).
            ApiIntegrationConfiguration? api = await SilaInventorySap.ResolveForRuleAsync(
                _repository, request.OrganizationId, rule, companyCode, cancellationToken);
            if (api == null)
            {
                throw new BadRequestCustomException(
                    SilaInventorySap.SAP_API_CONFIGURATION_NOT_FOUND,
                    SilaInventorySap.MissingApi(rule.ApiName));
            }

            foreach ((InternalTransferOrderItem item, decimal quantity) in lines)
            {
                item.PendingReverseQty = quantity;
                _repository.InternalTransferOrderItem.Update(item);
            }

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            InventoryErpPosting posting = ledger.QueueErpPosting(
                Common.SILA_REF_ITO_REVERSAL,
                transfer.Id,
                transfer.ItoNumber + "-R",
                transfer.ToLocationId,
                rule.GoodsMovementType);
            posting.CompanyCode = companyCode;
            posting.IntegrationSystem = api.Name;
            posting.ErpRequestPayload = SilaErpPayload.Sanitise(body);
            posting.Status = Common.SILA_POSTING_PENDING;

            transfer.ReversalStatus = Common.SILA_ITO_REVERSAL_PENDING;
            transfer.ReversalReason = request.Request.Reason.Trim();
            transfer.ReversedBy = request.UserId;
            transfer.ReversedOn = postingDate;
            _repository.InternalTransferOrder.Update(transfer);
            ledger.AddEvent(Common.SILA_REF_ITO, transfer.Id, "REVERSAL_REQUESTED", $"{rule.GoodsMovementType}: {transfer.ReversalReason}");
            await _repository.SaveAsync();

            _logger.LogInfo($"ITO reversal queued. TransferId: {transfer.Id}, PostingId: {posting.Id}, Movement: {rule.GoodsMovementType}, Lines: {lines.Count}");
            return new SilaTransferReverseResultDto
            {
                TransferId = transfer.Id,
                ItoNumber = transfer.ItoNumber,
                ReversalStatus = transfer.ReversalStatus,
                PostingId = posting.Id,
                Message = $"Reversal queued for {api.Name} (movement type {rule.GoodsMovementType}). SAP is not called until the ERP posting worker runs."
            };
        }
    }
}
