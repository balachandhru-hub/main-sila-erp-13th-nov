using Buyer.Application.Contracts;
using Buyer.Application.Features.Shared;
using Buyer.Application.Services;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideSilaPurchaseOrder
{
    /// <summary>
    /// Approves or rejects a local Voice/text PO that is waiting for approval.
    /// On final approval, posts to SAP via the existing PurchaseOrderErpProcessor (FIVE Create PO).
    /// </summary>
    public class DecideSilaPurchaseOrderCommandHandler : IRequestHandler<DecideSilaPurchaseOrderCommand, PurchaseOrderProcessResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IBuyerPurchaseDocumentGateway _buyerGateway;
        private readonly ISupplierSalesOrderClient _supplierSalesOrderClient;
        private readonly IUserContext _userContext;

        public DecideSilaPurchaseOrderCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IBuyerPurchaseDocumentGateway buyerGateway,
            ISupplierSalesOrderClient supplierSalesOrderClient,
            IUserContext userContext)
        {
            _repository = repository;
            _logger = logger;
            _buyerGateway = buyerGateway;
            _supplierSalesOrderClient = supplierSalesOrderClient;
            _userContext = userContext;
        }

        public async Task<PurchaseOrderProcessResultDto> Handle(DecideSilaPurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deciding purchase order approval. PurchaseOrderId: {request.PurchaseOrderId}, Approve: {request.Request.Approve}, UserId: {request.UserId}");
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            // Must be tracked — FindByCondition is AsNoTracking and status changes would not persist.
            PurchaseOrder? order = await _repository.PurchaseOrder
                .FindFirstByConditionAsync(x => x.Id == request.PurchaseOrderId && x.BuyerId == buyer.Id && x.IsActive);
            if (order == null || !order.IsActive || order.BuyerId != buyer.Id)
            {
                throw new NotFoundCustomException("Purchase order not found.", "The purchase order is not available.");
            }

            if (order.Status != Common.PURCHASE_ORDER_PENDING_APPROVAL)
            {
                throw new BadRequestCustomException(
                    "Purchase order is not waiting for approval.",
                    $"Status is {order.Status}. Only PENDING_APPROVAL orders can be decided.");
            }

            List<int> versions = await _repository.SilaApprovalStep
                .FindByCondition(x => x.ReferenceType == Common.SILA_REF_PO && x.ReferenceId == order.Id && x.IsActive)
                .Select(x => x.Version)
                .ToListAsync(cancellationToken);
            int version = versions.Count == 0 ? 1 : versions.Max();

            ApprovalDecision decision = await SilaApprovals.DecideAsync(
                _repository,
                _logger,
                Common.SILA_REF_PO,
                order.Id,
                version,
                request.UserId,
                request.Request.Approve,
                request.Request.Comment,
                cancellationToken,
                SilaAccess.HasFullAccess(request.RoleId));

            if (decision.Outcome == SilaApprovals.OUTCOME_REJECTED)
            {
                order.Status = Common.PURCHASE_ORDER_REJECTED;
                _repository.PurchaseOrder.Update(order);
                await _repository.SaveAsync();
                _logger.LogInfo($"Purchase order approval rejected. PurchaseOrderId: {order.Id}");
                return PurchaseOrderErpProcessor.Describe(order, null, null);
            }

            if (decision.Outcome != SilaApprovals.OUTCOME_APPROVED)
            {
                await _repository.SaveAsync();
                _logger.LogInfo($"Purchase order approval recorded. PurchaseOrderId: {order.Id}, Level: {decision.Level}");
                return PurchaseOrderErpProcessor.Describe(order, null, null);
            }

            // Leave PENDING_APPROVAL immediately so the PO drops from pending lists even if SAP is slow.
            order.Status = Common.PURCHASE_ORDER_APPROVED;
            _repository.PurchaseOrder.Update(order);
            await _repository.SaveAsync();

            _userContext.SetCurrentUserId(request.UserId);
            PurchaseOrderErpProcessor processor = new(
                _repository,
                _buyerGateway,
                _supplierSalesOrderClient,
                _userContext,
                _logger);
            PurchaseOrderProcessResultDto result = await processor.ProcessAsync(order.Id, request.UserId, cancellationToken);

            // Re-load tracked row — ProcessAsync may have mutated via its own FindFirstByCondition.
            PurchaseOrder? posted = await _repository.PurchaseOrder
                .FindFirstByConditionAsync(x => x.Id == order.Id && x.IsActive);
            if (posted != null)
            {
                order = posted;
            }

            if (!string.IsNullOrWhiteSpace(result.ErpPurchaseOrderId)
                || !string.IsNullOrWhiteSpace(order.ErpPurchaseOrderId))
            {
                string sap = result.ErpPurchaseOrderId ?? order.ErpPurchaseOrderId!;
                order.Status = Common.PURCHASE_ORDER_CREATED;
                if (string.IsNullOrWhiteSpace(order.ErpPurchaseOrderId))
                {
                    order.ErpPurchaseOrderId = sap;
                }

                _repository.PurchaseOrder.Update(order);
                InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
                ledger.AddEvent(
                    Common.SILA_REF_PO,
                    order.Id,
                    "SAP_PO",
                    $"SAP purchase order {sap}");
                await _repository.SaveAsync();
                result.ErpPurchaseOrderId = order.ErpPurchaseOrderId;
                result.Status = order.Status;
            }
            else
            {
                // Approved locally; SAP not yet — keep APPROVED (not PENDING) so Approve stays disabled.
                order.Status = Common.PURCHASE_ORDER_APPROVED;
                _repository.PurchaseOrder.Update(order);
                await _repository.SaveAsync();
                result.Status = order.Status;
            }

            _logger.LogInfo(
                $"Purchase order approved and posted. PurchaseOrderId: {order.Id}, ErpPurchaseOrderId: {result.ErpPurchaseOrderId}, BuyerErpStatus: {result.BuyerErpStatus}");
            return result;
        }
    }
}
