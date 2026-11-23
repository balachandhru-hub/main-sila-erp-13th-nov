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
            PurchaseOrder? order = await _repository.PurchaseOrder
                .FindByCondition(x => x.Id == request.PurchaseOrderId && x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (order == null)
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
                // Mutate the tracked instance — do not Attach/Update a second copy.
                order.Status = Common.PURCHASE_ORDER_REJECTED;
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

            order.Status = Common.PURCHASE_ORDER_APPROVED;
            await _repository.SaveAsync();

            _userContext.SetCurrentUserId(request.UserId);
            PurchaseOrderErpProcessor processor = new(
                _repository,
                _buyerGateway,
                _supplierSalesOrderClient,
                _userContext,
                _logger);
            // ProcessAsync reuses the same DbContext; pass Id and rely on the already-tracked entity.
            PurchaseOrderProcessResultDto result = await processor.ProcessAsync(order.Id, request.UserId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.ErpPurchaseOrderId))
            {
                // Keep mutating the same tracked instance (no second Attach/Update).
                order.Status = Common.PURCHASE_ORDER_CREATED;
                if (string.IsNullOrWhiteSpace(order.ErpPurchaseOrderId))
                {
                    order.ErpPurchaseOrderId = result.ErpPurchaseOrderId;
                }

                await _repository.SaveAsync();
                result.Status = order.Status;
            }

            _logger.LogInfo(
                $"Purchase order approved and posted. PurchaseOrderId: {order.Id}, ErpPurchaseOrderId: {result.ErpPurchaseOrderId}, BuyerErpStatus: {result.BuyerErpStatus}");
            return result;
        }
    }
}
