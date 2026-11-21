using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaErpPostingPayload
{
    public class GetSilaErpPostingPayloadQueryHandler : IRequestHandler<GetSilaErpPostingPayloadQuery, SilaErpPostingPayloadDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaErpPostingPayloadQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaErpPostingPayloadDto> Handle(GetSilaErpPostingPayloadQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Reading ERP posting payload. OrganizationId: {request.OrganizationId}, PostingId: {request.PostingId}");
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            // Include inactive rows — failed POS posts must still expose the request body.
            InventoryErpPosting? posting = await _repository.InventoryErpPosting.FindFirstByConditionAsync(
                x => x.Id == request.PostingId && x.BuyerId == buyer.Id);
            if (posting == null)
            {
                _logger.LogError($"ERP posting not found. PostingId: {request.PostingId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Transaction not found.", "Select a transaction from this tracker.");
            }

            string? requestBody = SilaErpPayload.Sanitise(posting.ErpRequestPayload);
            string? responseBody = SilaErpPayload.Sanitise(posting.ErpResponsePayload)
                ?? SilaErpPayload.HttpBodyFromError(posting.ErrorMessage);

            if (string.IsNullOrWhiteSpace(requestBody)
                && posting.ReferenceType == Common.SILA_REF_POS_SALE)
            {
                PosSalesTransaction? sale = await _repository.PosSalesTransaction
                    .FindFirstByConditionAsync(x => x.Id == posting.ReferenceId && x.BuyerId == buyer.Id);
                if (sale != null)
                {
                    requestBody = await SilaPosErpPayload.EnsureRequestAsync(
                        _repository, buyer.Id, sale, posting, cancellationToken);
                    responseBody ??= SilaErpPayload.HttpBodyFromError(sale.FailureMessage ?? posting.ErrorMessage);
                }
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                requestBody = SilaPosErpPayload.RememberRequest(posting, null);
                try
                {
                    _repository.InventoryErpPosting.Update(posting);
                    await _repository.SaveAsync();
                }
                catch (Exception)
                {
                    // Still return the diagnostic body below.
                }
            }

            return new SilaErpPostingPayloadDto
            {
                TechnicalDetail = requestBody,
                ErpMessage = responseBody,
                ErpHttpStatus = posting.ErpHttpStatus
            };
        }
    }
}
