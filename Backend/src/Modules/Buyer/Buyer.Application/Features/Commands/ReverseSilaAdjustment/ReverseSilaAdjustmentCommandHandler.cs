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
    /// Reverses part or all of a waste/damage adjustment: local stock IN, SAP Z02 on FIVE_STOCK_WASTE_UPDATE.
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
                throw new NotFoundCustomException("Adjustment not found.", "Select an active waste or damage adjustment to reverse.");
            }

            if (!SilaInventorySap.IsWasteAdjustmentType(original.AdjustmentType))
            {
                throw new BadRequestCustomException(
                    "Only waste adjustments can be reversed this way.",
                    "Reverse applies to WASTE, DAMAGE, BREAKAGE, SPOILAGE or EXPIRED.");
            }

            InventoryLocation location = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, original.LocationId);
            await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, request.UserId, request.RoleId, location.Id, cancellationToken);

            List<StockAdjustmentItem> lines = await _repository.StockAdjustmentItem
                .FindByCondition(x => x.StockAdjustmentId == original.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            StockAdjustmentItem? line = request.Request.MaterialId != null && request.Request.MaterialId != Guid.Empty
                ? lines.FirstOrDefault(x => x.MaterialId == request.Request.MaterialId)
                : lines.FirstOrDefault();
            if (line == null)
            {
                throw new BadRequestCustomException("No line to reverse.", "The original adjustment has no matching material line.");
            }

            ItemBuyerMaster? material = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x => x.Id == line.MaterialId && x.BuyerId == buyer.Id && x.IsActive);
            if (material == null || !SilaMaterialRules.HoldsStock(material))
            {
                throw new BadRequestCustomException("Material is not stocked.", "Choose an active stock inventory material.");
            }

            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                _repository, new[] { material.Id }, cancellationToken);
            string enteredUom = string.IsNullOrWhiteSpace(request.Request.Uom)
                ? (string.IsNullOrWhiteSpace(line.Uom) ? UomConverter.BaseUomOf(material) : line.Uom.Trim().ToUpperInvariant())
                : request.Request.Uom.Trim().ToUpperInvariant();
            decimal enteredQty = request.Request.Quantity ?? line.Quantity;
            if (enteredQty <= 0)
            {
                throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a positive reverse quantity.");
            }

            if (enteredQty > SilaInputRules.MAX_QUANTITY)
            {
                throw new BadRequestCustomException("Quantity is too large.", "Enter a quantity of at most 1,000,000,000.");
            }

            decimal baseQuantity = UomConverter.ToBase(_logger, material, enteredQty, enteredUom, conversions);
            string reason = string.IsNullOrWhiteSpace(request.Request.Reason)
                ? $"Reverse {original.AdjustmentNumber}: {enteredQty:0.####} {enteredUom}"
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

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            await ledger.PostAsync(new InventoryMovement
            {
                LocationId = location.Id,
                Material = material,
                Direction = Common.SILA_DIRECTION_IN,
                TransactionType = original.AdjustmentType,
                BaseQuantity = Math.Abs(baseQuantity),
                EnteredQuantity = Math.Abs(enteredQty),
                EnteredUom = enteredUom,
                ReferenceType = Common.SILA_REF_ADJUSTMENT,
                ReferenceId = reverse.Id,
                ReferenceNumber = reverse.AdjustmentNumber,
                Reason = reason
            }, cancellationToken);

            InventoryErpPosting posting = ledger.QueueErpPosting(
                Common.SILA_REF_ADJUSTMENT,
                reverse.Id,
                reverse.AdjustmentNumber,
                location.Id,
                SilaInventorySap.TYPE_Z02);
            posting.IntegrationSystem = SilaInventorySap.API_WASTE;
            ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, reverse.Id, "REVERSE", $"Z02 reverse of {original.AdjustmentNumber}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Waste adjustment reversed. Original: {original.AdjustmentNumber}, ReverseId: {reverse.Id}, Qty: {enteredQty} {enteredUom}");
            return reverse.Id;
        }
    }
}
