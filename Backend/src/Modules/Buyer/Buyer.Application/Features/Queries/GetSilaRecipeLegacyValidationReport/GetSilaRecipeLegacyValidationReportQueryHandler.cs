using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyValidationReport
{
    public class GetSilaRecipeLegacyValidationReportQueryHandler : IRequestHandler<GetSilaRecipeLegacyValidationReportQuery, SilaRecipeFileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyValidationReportQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaRecipeFileDto> Handle(GetSilaRecipeLegacyValidationReportQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            RecipeLegacyBatch batch = await SilaRecipeLegacyMigration.GetBatchAsync(_repository, _logger, buyer.Id, request.BatchId, cancellationToken);
            List<RecipeLegacyRecipe> recipes = await _repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batch.Id && x.BuyerId == buyer.Id && x.IsActive)
                .OrderBy(x => x.SourceRow)
                .ToListAsync(cancellationToken);
            return SilaRecipeLegacyMigration.BuildValidationReport(batch, recipes);
        }
    }
}
