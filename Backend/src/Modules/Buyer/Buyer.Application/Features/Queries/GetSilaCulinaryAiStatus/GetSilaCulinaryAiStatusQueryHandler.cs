using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaCulinaryAiStatus
{
    public class GetSilaCulinaryAiStatusQueryHandler : IRequestHandler<GetSilaCulinaryAiStatusQuery, SilaCulinaryAiStatusDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaCulinaryAiStatusQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaCulinaryAiStatusDto> Handle(GetSilaCulinaryAiStatusQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaCulinaryAiConfiguration? entity = await _repository.SilaCulinaryAiConfiguration.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.IsActive);
            return SilaCulinaryAiSettings.ToStatusDto(entity, buyer.Id);
        }
    }
}
