using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ApproveSilaStockCount
{
    public class ApproveSilaStockCountCommandHandler : IRequestHandler<ApproveSilaStockCountCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ApproveSilaStockCountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Unit> Handle(ApproveSilaStockCountCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(ApproveSilaStockCountCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<Unit> HandleOnceAsync(ApproveSilaStockCountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Approving stock count. StockCountId: {request.StockCountId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            StockCount count = await SilaStockCountRules.GetCountAsync(
                _repository, _logger, buyer.Id, request.UserId, request.RoleId, request.StockCountId, cancellationToken);
            if (count.Status != Common.SILA_COUNT_SUBMITTED && count.Status != Common.SILA_COUNT_ENQUIRY_PENDING)
            {
                _logger.LogError($"Stock count cannot be approved. StockCountId: {count.Id}, Status: {count.Status}");
                throw new BadRequestCustomException("Stock count cannot be approved.", $"Count {count.CountNumber} is {count.Status}. Only a submitted count can be approved.");
            }

            int openEnquiries = await _repository.StockShortageEnquiry
                .FindByCondition(x => x.StockCountId == count.Id
                    && x.IsActive
                    && x.Status != Common.SILA_ENQUIRY_ACCEPTED
                    && x.Status != Common.SILA_ENQUIRY_REJECTED)
                .CountAsync(cancellationToken);
            if (openEnquiries > 0)
            {
                _logger.LogError($"Stock count has open enquiries. StockCountId: {count.Id}, Open: {openEnquiries}");
                throw new BadRequestCustomException(
                    $"{openEnquiries} shortage enquiries are still open.",
                    "Accept or reject every shortage enquiry of this count before approving it.");
            }

            bool chained = await _repository.SilaApprovalStep
                .FindByCondition(x => x.ReferenceType == Common.SILA_REF_STOCK_COUNT && x.ReferenceId == count.Id && x.Version == 1 && x.IsActive)
                .AnyAsync(cancellationToken);
            if (chained)
            {
                ApprovalDecision decision = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_STOCK_COUNT, count.Id, 1, request.UserId, true, null, cancellationToken,
                    SilaAccess.IsAdministrator(request.RoleId));
                if (decision.Outcome == SilaApprovals.OUTCOME_PENDING)
                {
                    InventoryLedger waiting = new InventoryLedger(_repository, buyer.Id, request.UserId);
                    waiting.AddEvent(Common.SILA_REF_STOCK_COUNT, count.Id, "APPROVAL_LEVEL", $"Level {decision.Level} approved.");
                    await _repository.SaveAsync();
                    _logger.LogInfo($"Stock count approval advanced. StockCountId: {count.Id}, Level: {decision.Level}");
                    return Unit.Value;
                }
            }

            // Lines the cost controller rejected are not posted; every other variance is accepted with the count.
            List<StockCountItem> variances = await _repository.StockCountItem
                .FindByCondition(x => x.StockCountId == count.Id
                    && x.IsActive
                    && x.VarianceQty != null
                    && x.VarianceQty != 0
                    && (x.ReviewStatus == null || x.ReviewStatus != SilaStockCountRules.LINE_REVIEW_REJECTED))
                .ToListAsync(cancellationToken);
            List<Guid> materialIds = variances.Select(x => x.MaterialId).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && materialIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            int adjustmentCount = 0;
            foreach (StockCountItem item in variances)
            {
                if (!materials.TryGetValue(item.MaterialId, out ItemBuyerMaster? material))
                {
                    _logger.LogError($"Material of the count line not found. MaterialId: {item.MaterialId}, StockCountId: {count.Id}");
                    throw new NotFoundCustomException("Material not found.", $"Material {item.MaterialCode} no longer exists in the Item Master.");
                }

                decimal variance = item.VarianceQty ?? 0;
                decimal physical = item.CountedQty ?? (item.SystemQty + variance);
                string direction = variance > 0 ? Common.SILA_DIRECTION_IN : Common.SILA_DIRECTION_OUT;
                SilaInventorySap.Rule rule = SilaInventorySap.ForInventoryDifference(direction);
                string reason = $"Stock count {count.CountNumber}: {item.MaterialCode} physical {physical:0.####} variance {variance:0.####}";

                // Same posting path as Live Inventory createLiveStockCount: StockAdjustment + SAP 701/702.
                string adjustmentNumber = await DocumentNumber.NextAsync(
                    _repository, buyer.Id, DocumentNumber.ADJUSTMENT, 6, cancellationToken);
                StockAdjustment adjustment = new StockAdjustment
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
                _repository.StockAdjustment.Create(adjustment);
                _repository.StockAdjustmentItem.Create(new StockAdjustmentItem
                {
                    Id = Guid.NewGuid(),
                    StockAdjustmentId = adjustment.Id,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? item.MaterialCode ?? string.Empty,
                    MaterialName = material.Description ?? material.MaterialCode ?? string.Empty,
                    Quantity = physical,
                    Uom = string.IsNullOrWhiteSpace(item.BaseUom) ? UomConverter.BaseUomOf(material) : item.BaseUom,
                    BaseQuantity = physical,
                    IsActive = true
                });

                await ledger.PostAsync(new InventoryMovement
                {
                    LocationId = count.LocationId,
                    Material = material,
                    Direction = direction,
                    TransactionType = Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT,
                    BaseQuantity = Math.Abs(variance),
                    EnteredQuantity = Math.Abs(variance),
                    EnteredUom = item.BaseUom,
                    UnitCost = item.UnitCost,
                    ReferenceType = Common.SILA_REF_ADJUSTMENT,
                    ReferenceId = adjustment.Id,
                    ReferenceNumber = adjustment.AdjustmentNumber,
                    Reason = reason,
                    BusinessDate = count.BusinessDate
                }, cancellationToken);

                InventoryErpPosting posting = ledger.QueueErpPosting(
                    Common.SILA_REF_ADJUSTMENT,
                    adjustment.Id,
                    adjustment.AdjustmentNumber,
                    count.LocationId,
                    rule.GoodsMovementType);
                posting.IntegrationSystem = rule.ApiName;
                ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, adjustment.Id, "STOCK_COUNT", $"{rule.GoodsMovementType}: {variance:0.####}");
                adjustmentCount++;
            }

            count.Status = Common.SILA_COUNT_POSTED;
            count.ApprovedBy = request.UserId;
            count.ApprovedOn = DateTime.UtcNow;
            ledger.AddEvent(Common.SILA_REF_STOCK_COUNT, count.Id, SilaStockCountRules.EVENT_APPROVED, $"Adjustments={adjustmentCount}");
            await SilaPhysicalInventoryRules.CloseForCountAsync(_repository, ledger, count, Common.SILA_PI_COMPLETED, cancellationToken);
            await _repository.SaveAsync();

            _logger.LogInfo($"Stock count approved and posted. StockCountId: {count.Id}, Adjustments: {adjustmentCount}");
            return Unit.Value;
        }
    }
}
