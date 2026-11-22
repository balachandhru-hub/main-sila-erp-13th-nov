using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PublishSilaRecipeLegacyBatch
{
    public class PublishSilaRecipeLegacyBatchCommandHandler : IRequestHandler<PublishSilaRecipeLegacyBatchCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public PublishSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(PublishSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.PublishBatchAsync(
                _repository, _logger, _configuration, request.OrganizationId, request.UserId, request.RoleId,
                request.BatchId, request.CanApprove, cancellationToken);
        }
    }
}
