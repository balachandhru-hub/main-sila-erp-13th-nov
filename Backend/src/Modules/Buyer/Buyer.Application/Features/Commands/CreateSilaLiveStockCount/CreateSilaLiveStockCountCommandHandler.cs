using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateSilaLiveStockCount
{
    /// <summary>
    /// Live Inventory stock count: set book stock to physical qty, queue SAP Z01 (surplus) or Z02 (shortage).
    /// Local ledger updates immediately; SAP posts via Material Document (Positive Stock Adjustment / Goods Issue).
    /// </summary>
    public class CreateSilaLiveStockCountCommandHandler : IRequestHandler<CreateSilaLiveStockCountCommand, SilaLiveStockCountResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateSilaLiveStockCountCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaLiveStockCountResultDto> Handle(CreateSilaLiveStockCountCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(CreateSilaLiveStockCountCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<SilaLiveStockCountResultDto> HandleOnceAsync(CreateSilaLiveStockCountCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.PhysicalQty < 0)
            {
                throw new BadRequestCustomException("Physical quantity cannot be negative.", "Enter zero or a positive physical count.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            InventoryLocation location = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, request.Request.LocationId);
            await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, request.UserId, request.RoleId, location.Id, cancellationToken);

            ItemBuyerMaster material = await ResolveMaterialAsync(buyer.Id, request.Request, cancellationToken);
            if (!SilaMaterialRules.HoldsStock(material))
            {
                throw new BadRequestCustomException("Material is not stocked.", "Choose an active stock inventory material.");
            }

            string uom = string.IsNullOrWhiteSpace(request.Request.Uom)
                ? UomConverter.BaseUomOf(material)
                : request.Request.Uom.Trim().ToUpperInvariant();
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                _repository, new[] { material.Id }, cancellationToken);
            decimal physicalBase = UomConverter.ToBase(_logger, material, request.Request.PhysicalQty, uom, conversions);

            InventoryBalance? balance = await _repository.InventoryBalance
                .FindFirstByConditionAsync(x => x.LocationId == location.Id && x.MaterialId == material.Id && x.IsActive);
            decimal onHand = balance?.OnHandQty ?? 0m;
            decimal variance = physicalBase - onHand;
            if (Math.Abs(variance) < 0.0001m)
            {
                throw new BadRequestCustomException("No variance.", "Physical count matches stock on hand. Nothing to post.");
            }

            string direction = variance > 0 ? Common.SILA_DIRECTION_IN : Common.SILA_DIRECTION_OUT;
            // Live Inventory stock count uses Z01/Z02 material documents (701/702 PI path is reserved for formal counts).
            SilaInventorySap.Rule rule = SilaInventorySap.ForDirection(direction);
            string reason = string.IsNullOrWhiteSpace(request.Request.Reason)
                ? $"Live stock count {material.MaterialCode}: physical {physicalBase:0.####} vs book {onHand:0.####}"
                : request.Request.Reason.Trim();
            SilaInputRules.MaxLength(_logger, reason, SilaInputRules.COMMENT_LENGTH, "Reason");

            string adjustmentNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.ADJUSTMENT, 6, cancellationToken);
            StockAdjustment adjustment = new StockAdjustment
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                AdjustmentNumber = adjustmentNumber,
                LocationId = location.Id,
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
                MaterialCode = material.MaterialCode ?? string.Empty,
                MaterialName = material.Description ?? material.MaterialCode ?? string.Empty,
                Quantity = request.Request.PhysicalQty,
                Uom = uom,
                BaseQuantity = physicalBase,
                IsActive = true
            });

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            await ledger.PostAsync(new InventoryMovement
            {
                LocationId = location.Id,
                Material = material,
                Direction = direction,
                TransactionType = Common.SILA_TXN_STOCK_COUNT_ADJUSTMENT,
                BaseQuantity = Math.Abs(variance),
                EnteredQuantity = Math.Abs(variance),
                EnteredUom = UomConverter.BaseUomOf(material),
                ReferenceType = Common.SILA_REF_ADJUSTMENT,
                ReferenceId = adjustment.Id,
                ReferenceNumber = adjustment.AdjustmentNumber,
                Reason = reason
            }, cancellationToken);

            InventoryErpPosting posting = ledger.QueueErpPosting(
                Common.SILA_REF_ADJUSTMENT,
                adjustment.Id,
                adjustment.AdjustmentNumber,
                location.Id,
                rule.GoodsMovementType);
            posting.IntegrationSystem = rule.ApiName;
            ledger.AddEvent(Common.SILA_REF_ADJUSTMENT, adjustment.Id, "STOCK_COUNT", $"{rule.GoodsMovementType}: {variance:0.####}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Live stock count posted. AdjustmentId: {adjustment.Id}, Movement: {rule.GoodsMovementType}, Variance: {variance}");
            return new SilaLiveStockCountResultDto
            {
                AdjustmentId = adjustment.Id,
                AdjustmentNumber = adjustment.AdjustmentNumber,
                PostingId = posting.Id,
                MaterialCode = material.MaterialCode ?? string.Empty,
                OnHandQty = onHand,
                PhysicalQty = physicalBase,
                VarianceQty = variance,
                MovementType = rule.GoodsMovementType,
                Message = $"Variance {variance:0.####} queued as SAP movement {rule.GoodsMovementType}."
            };
        }

        private async Task<ItemBuyerMaster> ResolveMaterialAsync(Guid buyerId, SilaLiveStockCountWriteDto request, CancellationToken cancellationToken)
        {
            if (request.MaterialId != null && request.MaterialId != Guid.Empty)
            {
                ItemBuyerMaster? byId = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.Id == request.MaterialId && x.BuyerId == buyerId && x.IsActive);
                if (byId != null)
                {
                    return byId;
                }
            }

            string code = (request.MaterialCodeOrBarcode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new BadRequestCustomException("Material required.", "Scan a barcode or enter a material code.");
            }

            ItemBuyerMaster? material = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                    && (x.MaterialCode == code || x.Barcode == code))
                .FirstOrDefaultAsync(cancellationToken);
            if (material == null)
            {
                // Tolerate leading-zero SAP padding.
                string trimmed = code.TrimStart('0');
                if (!string.IsNullOrWhiteSpace(trimmed) && trimmed != code)
                {
                    material = await _repository.ItemBuyerMaster
                        .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                            && (x.MaterialCode == trimmed || x.Barcode == trimmed
                                || x.MaterialCode == code || x.Barcode == code))
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }

            if (material == null)
            {
                throw new NotFoundCustomException("Material not found.", "No active material matches that code or barcode.");
            }

            return material;
        }
    }
}
