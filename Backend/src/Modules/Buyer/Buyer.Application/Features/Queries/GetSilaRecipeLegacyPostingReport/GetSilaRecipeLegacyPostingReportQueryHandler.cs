using System.Text;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyPostingReport
{
    public class GetSilaRecipeLegacyPostingReportQueryHandler : IRequestHandler<GetSilaRecipeLegacyPostingReportQuery, SilaRecipeFileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyPostingReportQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaRecipeFileDto> Handle(GetSilaRecipeLegacyPostingReportQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            RecipeLegacyBatch batch = await SilaRecipeLegacyMigration.GetBatchAsync(_repository, _logger, buyer.Id, request.BatchId, cancellationToken);
            if (string.IsNullOrWhiteSpace(batch.PostingReportJson))
            {
                throw new BadRequestCustomException("Posting report is not available.", "Run the related action first.");
            }

            return new SilaRecipeFileDto
            {
                FileName = "recipe-legacy-posting-" + batch.BatchNumber + ".json",
                ContentType = "application/json",
                Content = Encoding.UTF8.GetBytes(batch.PostingReportJson)
            };
        }
    }
}
