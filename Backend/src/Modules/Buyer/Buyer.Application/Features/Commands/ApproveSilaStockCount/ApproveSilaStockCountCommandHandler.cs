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
            int lineCount = 0;
            if (variances.Count > 0)
            {
                // One local adjustment document + one ERP posting for the whole count (not per line).
                string adjustmentNumber = await DocumentNumber.NextAsync(
                    _repository, buyer.Id, DocumentNumber.ADJUSTMENT, 6, cancellationToken);
                StockAdjustment adjustment = new StockAdjustment
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    AdjustmentNumber = adjustmentNumber,
                    LocationId = count.LocationId,
                    AdjustmentType = Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT,
                    Reason = $"Stock count {count.CountNumber}: {variances.Count} variance line(s)",
                    PostedBy = request.UserId,
                    IsActive = true
                };
                _repository.StockAdjustment.Create(adjustment);

                bool hasIn = false;
                bool hasOut = false;
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
                    hasIn |= direction == Common.SILA_DIRECTION_IN;
                    hasOut |= direction == Common.SILA_DIRECTION_OUT;
                    string reason = $"Stock count {count.CountNumber}: {item.MaterialCode} physical {physical:0.####} variance {variance:0.####}";

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

                    // Ledger references the STOCK COUNT so one ERP posting covers every variance line together.
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
                        ReferenceType = Common.SILA_REF_STOCK_COUNT,
                        ReferenceId = count.Id,
                        ReferenceNumber = count.CountNumber,
                        Reason = reason,
                        BusinessDate = count.BusinessDate
                    }, cancellationToken);
                    lineCount++;
                }

                // Z01/Z02 Material Document (Goods Issue / Positive Adjustment). Mixed directions use STOCK_COUNT
                // so PostStockEffect posts both movements against the same posting record.
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
                ledger.AddEvent(
                    Common.SILA_REF_STOCK_COUNT,
                    count.Id,
                    "STOCK_COUNT_ERP",
                    $"{movement}: lines={lineCount} adj={adjustment.AdjustmentNumber}");
            }

            count.Status = Common.SILA_COUNT_POSTED;
            count.ApprovedBy = request.UserId;
            count.ApprovedOn = DateTime.UtcNow;
            ledger.AddEvent(Common.SILA_REF_STOCK_COUNT, count.Id, SilaStockCountRules.EVENT_APPROVED, $"Lines={lineCount}");
            await SilaPhysicalInventoryRules.CloseForCountAsync(_repository, ledger, count, Common.SILA_PI_COMPLETED, cancellationToken);
            await _repository.SaveAsync();

            _logger.LogInfo($"Stock count approved and posted together. StockCountId: {count.Id}, Lines: {lineCount}");
            return Unit.Value;
        }
    }
}
