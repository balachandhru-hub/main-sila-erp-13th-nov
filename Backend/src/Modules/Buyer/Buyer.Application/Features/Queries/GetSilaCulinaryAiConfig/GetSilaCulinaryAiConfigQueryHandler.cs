using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaCulinaryAiConfig
{
    public class GetSilaCulinaryAiConfigQueryHandler : IRequestHandler<GetSilaCulinaryAiConfigQuery, SilaCulinaryAiConfigDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _protector;

        public GetSilaCulinaryAiConfigQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector protector)
        {
            _repository = repository;
            _logger = logger;
            _protector = protector;
        }

        public async Task<SilaCulinaryAiConfigDto> Handle(GetSilaCulinaryAiConfigQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaCulinaryAiConfiguration? entity = await _repository.SilaCulinaryAiConfiguration.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.IsActive);
            bool canEdit = SilaAccess.IsAdministrator(request.RoleId);
            return SilaCulinaryAiSettings.ToDto(entity, buyer.Id, canEdit, _protector);
        }
    }
}
