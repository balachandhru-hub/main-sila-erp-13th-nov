using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ReverseSilaAdjustment
{
    /// <summary>
    /// Reverses a posted stock adjustment: opposite local stock move + ERP queue.
    /// Waste types post Z02 IN on the waste API; stock-count / manual / opening flip the original ledger direction.
    /// </summary>
    public class ReverseSilaAdjustmentCommandHandler : IRequestHandler<ReverseSilaAdjustmentCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ReverseSilaAdjustmentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Guid> Handle(ReverseSilaAdjustmentCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(ReverseSilaAdjustmentCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<Guid> HandleOnceAsync(ReverseSilaAdjustmentCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            StockAdjustment? original = await _repository.StockAdjustment
                .FindFirstByConditionAsync(x => x.Id == request.AdjustmentId && x.BuyerId == buyer.Id && x.IsActive);
            if (original == null)
            {
                throw new NotFoundCustomException("Adjustment not found.", "Select an active adjustment to reverse.");
            }

            if (!CanReverse(original.AdjustmentType))
            {
                throw new BadRequestCustomException(
                    "This adjustment type cannot be reversed.",
                    "Reverse applies to waste types, stock-count adjustments, manual adjustments and opening stock.");
            }

            InventoryLocation location = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, original.LocationId);
            await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, request.UserId, request.RoleId, location.Id, cancellationToken);

            bool alreadyReversed = await _repository.InventoryWorkflowEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_ADJUSTMENT && x.ReferenceId == original.Id
                    && x.Action == "REVERSE")
                .AnyAsync(cancellationToken);
            if (alreadyReversed)
            {
                throw new BadRequestCustomException("Already reversed.", "This adjustment already has a reverse event.");
            }

            List<StockAdjustmentItem> lines = await _repository.StockAdjustmentItem
                .FindByCondition(x => x.StockAdjustmentId == original.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0)
            {
                throw new BadRequestCustomException("No lines to reverse.", "The original adjustment has no active lines.");
            }

            bool waste = SilaInventorySap.IsWasteAdjustmentType(original.AdjustmentType);
            List<StockAdjustmentItem> targets = waste
                && request.Request.MaterialId != null
                && request.Request.MaterialId != Guid.Empty
                ? lines.Where(x => x.MaterialId == request.Request.MaterialId).ToList()
                : lines;
            if (targets.Count == 0)
            {
                throw new BadRequestCustomException("No line to reverse.", "The original adjustment has no matching material line.");
            }

            List<InventoryTransaction> ledgerLines = await _repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_ADJUSTMENT && x.ReferenceId == original.Id)
                .ToListAsync(cancellationToken);

            string reason = string.IsNullOrWhiteSpace(request.Request.Reason)
                ? $"Reverse {original.AdjustmentNumber}"
                : request.Request.Reason.Trim();
            SilaInputRules.MaxLength(_logger, reason, SilaInputRules.COMMENT_LENGTH, "Reason");

            string adjustmentNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.ADJUSTMENT, 6, cancellationToken);
            StockAdjustment reverse = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                AdjustmentNumber = adjustmentNumber,
                LocationId = location.Id,
                AdjustmentType = original.AdjustmentType,
                Reason = reason,
                PostedBy = request.UserId,
                IsActive = true
            };
            _repository.StockAdjustment.Create(reverse);

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            Dictionary<Guid, ItemBuyerMaster> materials = await SilaAccess.GetMaterialsAsync(
                _repository, _logger, buyer.Id, targets.Select(x => x.MaterialId), cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                _repository, materials.Keys, cancellationToken);

            string? erpMovement = null;
            string? erpApi = null;

            foreach (StockAdjustmentItem line in targets)
            {
                if (!materials.TryGetValue(line.MaterialId, out ItemBuyerMaster? material) || !SilaMaterialRules.HoldsStock(material))
                {
                    throw new BadRequestCustomException("Material is not stocked.", "Choose an active stock inventory material.");
                }

                InventoryTransaction? originalTxn = ledgerLines
                    .Where(x => x.MaterialId == line.MaterialId)
                    .OrderByDescending(x => x.DateCreated)
                    .FirstOrDefault();

                string enteredUom;
                decimal enteredQty;
                decimal baseQuantity;
                string reverseDirection;

                if (waste)
                {
                    enteredUom = string.IsNullOrWhiteSpace(request.Request.Uom)
                        ? (string.IsNullOrWhiteSpace(line.Uom) ? UomConverter.BaseUomOf(material) : line.Uom.Trim().ToUpperInvariant())
                        : request.Request.Uom.Trim().ToUpperInvariant();
                    enteredQty = request.Request.Quantity ?? line.Quantity;
                    if (enteredQty <= 0)
                    {
                        throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a positive reverse quantity.");
                    }

                    if (enteredQty > SilaInputRules.MAX_QUANTITY)
                    {
                        throw new BadRequestCustomException("Quantity is too large.", "Enter a quantity of at most 1,000,000,000.");
                    }

                    baseQuantity = UomConverter.ToBase(_logger, material, enteredQty, enteredUom, conversions);
                    reverseDirection = Common.SILA_DIRECTION_IN;
                    erpMovement = SilaInventorySap.TYPE_Z02;
                    erpApi = SilaInventorySap.API_WASTE;
                }
                else
                {
                    // Stock-count / manual / opening: reverse the ledger variance (not the physical line qty).
                    decimal ledgerQty = originalTxn != null && originalTxn.Quantity > 0
                        ? originalTxn.Quantity
                        : Math.Abs(line.BaseQuantity);
                    if (ledgerQty <= 0)
                    {
                        throw new BadRequestCustomException("Nothing to reverse.", "The original adjustment has no stock quantity for this line.");
                    }

                    enteredUom = originalTxn != null && !string.IsNullOrWhiteSpace(originalTxn.EnteredUom)
                        ? originalTxn.EnteredUom
                        : (string.IsNullOrWhiteSpace(line.Uom) ? UomConverter.BaseUomOf(material) : line.Uom.Trim().ToUpperInvariant());
                    enteredQty = originalTxn != null && originalTxn.EnteredQuantity > 0
                        ? originalTxn.EnteredQuantity
                        : ledgerQty;
                    baseQuantity = ledgerQty;
                    string originalDirection = originalTxn?.Direction ?? Common.SILA_DIRECTION_OUT;
                    reverseDirection = originalDirection == Common.SILA_DIRECTION_IN
                        ? Common.SILA_DIRECTION_OUT
                        : Common.SILA_DIRECTION_IN;
                    // Consumptions Z01 (Goods Issue); reversals Z02 (S4 - Positive Stock Adjustment).
                    SilaInventorySap.Rule rule = SilaInventorySap.ForDirection(reverseDirection);
                    erpMovement = rule.GoodsMovementType;
                    erpApi = rule.ApiName;
                }

                _repository.StockAdjustmentItem.Create(new StockAdjustmentItem
                {
                    Id = Guid.NewGuid(),
                    StockAdjustmentId = reverse.Id,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    MaterialName = material.Description ?? material.MaterialCode ?? string.Empty,
                    Quantity = enteredQty,
                    Uom = enteredUom,
                    BaseQuantity = baseQuantity,
                    IsActive = true
                });

                await ledger.PostAsync(new InventoryMovement
                {
                    LocationId = location.Id,
                    Material = material,
                    Direction = reverseDirection,
                    TransactionType = original.AdjustmentType,
                    BaseQuantity = Math.Abs(baseQuantity),
                    EnteredQuantity = Math.Abs(enteredQty),
                    EnteredUom = enteredUom,
                    ReferenceType = Common.SILA_REF_ADJUSTMENT,
                    ReferenceId = reverse.Id,
                    ReferenceNumber = reverse.AdjustmentNumber,
                    Reason = reason
                }, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(erpMovement))
            {
                InventoryErpPosting posting = ledger.QueueErpPosting(
                    Common.SILA_REF_ADJUSTMENT,
                    reverse.Id,
                    reverse.AdjustmentNumber,
                    location.Id,
                    erpMovement);
                posting.IntegrationSystem = erpApi;
            }

            ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, original.Id, "REVERSE", $"Reversed as {reverse.AdjustmentNumber}: {reason}");
            ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, reverse.Id, "REVERSE", $"{erpMovement ?? "STOCK"} reverse of {original.AdjustmentNumber}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Adjustment reversed. Original: {original.AdjustmentNumber}, ReverseId: {reverse.Id}, Type: {original.AdjustmentType}");
            return reverse.Id;
        }

        private static bool CanReverse(string? adjustmentType)
        {
            if (SilaInventorySap.IsWasteAdjustmentType(adjustmentType))
            {
                return true;
            }

            string type = (adjustmentType ?? string.Empty).Trim().ToUpperInvariant();
            return type is Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT
                or Common.SILA_TXN_MANUAL_ADJUSTMENT
                or Common.SILA_TXN_OPENING_STOCK;
        }
    }
}
