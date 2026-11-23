using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ReverseSilaStockCount
{
    /// <summary>
    /// Reverses a posted stock count: opposite local stock for each variance line, then queues the flipped ERP movement.
    /// </summary>
    public class ReverseSilaStockCountCommandHandler : IRequestHandler<ReverseSilaStockCountCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ReverseSilaStockCountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Guid> Handle(ReverseSilaStockCountCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(ReverseSilaStockCountCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<Guid> HandleOnceAsync(ReverseSilaStockCountCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            StockCount count = await SilaStockCountRules.GetCountAsync(
                _repository, _logger, buyer.Id, request.UserId, request.RoleId, request.StockCountId, cancellationToken);
            if (count.Status != Common.SILA_COUNT_POSTED)
            {
                throw new BadRequestCustomException("Only posted counts can be reversed.", "Approve and post the stock count before reversing.");
            }

            bool alreadyReversed = await _repository.InventoryWorkflowEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_STOCK_COUNT && x.ReferenceId == count.Id
                    && x.Action == "REVERSE")
                .AnyAsync(cancellationToken);
            if (alreadyReversed)
            {
                throw new BadRequestCustomException("Already reversed.", "This stock count already has a reverse event.");
            }

            List<InventoryTransaction> ledgerLines = await _repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_STOCK_COUNT && x.ReferenceId == count.Id)
                .ToListAsync(cancellationToken);
            if (ledgerLines.Count == 0)
            {
                throw new BadRequestCustomException("No stock movements to reverse.", "This stock count has no posted inventory lines.");
            }

            string reason = string.IsNullOrWhiteSpace(request.Reason)
                ? $"Reverse {count.CountNumber}"
                : request.Reason.Trim();
            SilaInputRules.MaxLength(_logger, reason, SilaInputRules.COMMENT_LENGTH, "Reason");

            string adjustmentNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.ADJUSTMENT, 6, cancellationToken);
            StockAdjustment reverse = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                AdjustmentNumber = adjustmentNumber,
                LocationId = count.LocationId,
                AdjustmentType = Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT,
                Reason = reason,
                PostedBy = request.UserId,
                IsActive = true
            };
            _repository.StockAdjustment.Create(reverse);

            Dictionary<Guid, ItemBuyerMaster> materials = await SilaAccess.GetMaterialsAsync(
                _repository, _logger, buyer.Id, ledgerLines.Select(x => x.MaterialId), cancellationToken);
            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            bool hasIn = false;
            bool hasOut = false;

            foreach (InventoryTransaction originalTxn in ledgerLines)
            {
                if (!materials.TryGetValue(originalTxn.MaterialId, out ItemBuyerMaster? material) || !SilaMaterialRules.HoldsStock(material))
                {
                    throw new BadRequestCustomException("Material is not stocked.", "Choose an active stock inventory material.");
                }

                string reverseDirection = originalTxn.Direction == Common.SILA_DIRECTION_IN
                    ? Common.SILA_DIRECTION_OUT
                    : Common.SILA_DIRECTION_IN;
                hasIn |= reverseDirection == Common.SILA_DIRECTION_IN;
                hasOut |= reverseDirection == Common.SILA_DIRECTION_OUT;

                decimal qty = Math.Abs(originalTxn.Quantity);
                string uom = string.IsNullOrWhiteSpace(originalTxn.EnteredUom)
                    ? UomConverter.BaseUomOf(material)
                    : originalTxn.EnteredUom;
                decimal enteredQty = originalTxn.EnteredQuantity > 0 ? Math.Abs(originalTxn.EnteredQuantity) : qty;

                _repository.StockAdjustmentItem.Create(new StockAdjustmentItem
                {
                    Id = Guid.NewGuid(),
                    StockAdjustmentId = reverse.Id,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    MaterialName = material.Description ?? material.MaterialCode ?? string.Empty,
                    Quantity = enteredQty,
                    Uom = uom,
                    BaseQuantity = qty,
                    IsActive = true
                });

                await ledger.PostAsync(new InventoryMovement
                {
                    LocationId = count.LocationId,
                    Material = material,
                    Direction = reverseDirection,
                    TransactionType = Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT,
                    BaseQuantity = qty,
                    EnteredQuantity = enteredQty,
                    EnteredUom = uom,
                    ReferenceType = Common.SILA_REF_STOCK_COUNT,
                    ReferenceId = count.Id,
                    ReferenceNumber = count.CountNumber,
                    Reason = reason,
                    BusinessDate = count.BusinessDate
                }, cancellationToken);
            }

            string movement;
            string apiName;
            if (hasIn && hasOut)
            {
                movement = Common.SILA_MOVEMENT_STOCK_COUNT;
                apiName = $"{SilaInventorySap.API_GOODS_ISSUE}+{SilaInventorySap.API_POSITIVE_ADJUSTMENT}";
            }
            else if (hasIn)
            {
                SilaInventorySap.Rule rule = SilaInventorySap.PositiveAdjustment;
                movement = rule.GoodsMovementType;
                apiName = rule.ApiName;
            }
            else
            {
                SilaInventorySap.Rule rule = SilaInventorySap.GoodsIssue;
                movement = rule.GoodsMovementType;
                apiName = rule.ApiName;
            }

            InventoryErpPosting posting = ledger.QueueErpPosting(
                Common.SILA_REF_STOCK_COUNT,
                count.Id,
                count.CountNumber,
                count.LocationId,
                movement);
            posting.IntegrationSystem = apiName;
            ledger.AddEvent(Common.SILA_REF_STOCK_COUNT, count.Id, "REVERSE", $"Reversed as {reverse.AdjustmentNumber}: {reason}");
            ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, reverse.Id, "REVERSE", $"Stock count reverse of {count.CountNumber}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Stock count reversed. CountId: {count.Id}, ReverseAdj: {reverse.AdjustmentNumber}");
            return reverse.Id;
        }
    }
}
