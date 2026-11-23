using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.ResolveSilaUserPurchasingDefaults
{
    public class ResolveSilaUserPurchasingDefaultsQueryHandler
        : IRequestHandler<ResolveSilaUserPurchasingDefaultsQuery, SilaUserPurchasingDefaultsDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ResolveSilaUserPurchasingDefaultsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaUserPurchasingDefaultsDto> Handle(
            ResolveSilaUserPurchasingDefaultsQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            return await SilaUserPurchasingProfileRules.ResolveAsync(
                _repository, _logger, buyer.Id, request.UserId, request.Request, cancellationToken);
        }
    }
}
