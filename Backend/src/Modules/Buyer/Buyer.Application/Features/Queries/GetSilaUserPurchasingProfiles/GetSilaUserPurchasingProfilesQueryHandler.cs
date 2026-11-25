using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaUserPurchasingProfiles
{
    public class GetSilaUserPurchasingProfilesQueryHandler
        : IRequestHandler<GetSilaUserPurchasingProfilesQuery, List<SilaUserPurchasingProfileDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaUserPurchasingProfilesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SilaUserPurchasingProfileDto>> Handle(
            GetSilaUserPurchasingProfilesQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Guid scopeUserId = request.AdminScope ? (request.FilterUserId ?? Guid.Empty) : request.UserId;
            IQueryable<SilaUserPurchasingProfile> query = _repository.SilaUserPurchasingProfile
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (request.ActiveOnly)
            {
                query = query.Where(x => x.Status == SilaUserPurchasingProfileRules.STATUS_ACTIVE);
            }

            if (!request.AdminScope || request.FilterUserId != null)
            {
                Guid userId = request.AdminScope ? request.FilterUserId!.Value : request.UserId;
                query = query.Where(x => x.UserId == userId);
            }

            List<SilaUserPurchasingProfile> rows = await query
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.ProfileVersion)
                .ThenBy(x => x.DisplayName)
                .ToListAsync(cancellationToken);
            _logger.LogInfo($"Purchasing profiles listed. BuyerId: {buyer.Id}, Count: {rows.Count}, ScopeUser: {scopeUserId}");
            return rows.Select(SilaUserPurchasingProfileRules.ToDto).ToList();
        }
    }
}
