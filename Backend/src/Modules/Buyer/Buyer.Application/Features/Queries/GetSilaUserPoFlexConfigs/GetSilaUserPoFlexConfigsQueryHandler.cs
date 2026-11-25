using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaUserPoFlexConfigs
{
    public class GetSilaUserPoFlexConfigsQueryHandler : IRequestHandler<GetSilaUserPoFlexConfigsQuery, List<SilaUserPoFlexConfigDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaUserPoFlexConfigsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SilaUserPoFlexConfigDto>> Handle(GetSilaUserPoFlexConfigsQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Guid userId = request.AdminScope
                ? (request.FilterUserId ?? throw new SharedKernel.ExceptionHandler.BadRequestCustomException(
                    "User ID is required.", "Enter a user GUID to load PO defaults."))
                : request.UserId;
            string configType = SilaUserPoFlexRules.NormalizeType(request.ConfigType);

            IQueryable<SilaUserPoFlexConfig> query = _repository.SilaUserPoFlexConfig
                .FindByCondition(x => x.BuyerId == buyer.Id && x.UserId == userId && x.ConfigType == configType && x.IsActive);
            if (request.ActiveOnly)
            {
                query = query.Where(x => x.Status == SilaUserPoFlexRules.STATUS_ACTIVE);
            }

            List<SilaUserPoFlexConfig> configs = await query
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.DateUpdated)
                .ToListAsync(cancellationToken);

            List<Guid> ids = configs.Select(c => c.Id).ToList();
            List<SilaUserPoFlexParam> parameters = ids.Count == 0
                ? new()
                : await _repository.SilaUserPoFlexParam
                    .FindByCondition(x => ids.Contains(x.ConfigId) && x.IsActive)
                    .ToListAsync(cancellationToken);
            List<SilaUserPoFlexCondition> conditions = ids.Count == 0
                ? new()
                : await _repository.SilaUserPoFlexCondition
                    .FindByCondition(x => ids.Contains(x.ConfigId) && x.IsActive)
                    .ToListAsync(cancellationToken);

            return configs.Select(c => SilaUserPoFlexRules.ToDto(
                c,
                parameters.Where(p => p.ConfigId == c.Id),
                conditions.Where(r => r.ConfigId == c.Id))).ToList();
        }
    }
}
