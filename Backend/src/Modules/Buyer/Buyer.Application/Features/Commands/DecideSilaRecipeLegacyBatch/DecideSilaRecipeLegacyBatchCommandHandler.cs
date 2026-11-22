using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideSilaRecipeLegacyBatch
{
    public class DecideSilaRecipeLegacyBatchCommandHandler : IRequestHandler<DecideSilaRecipeLegacyBatchCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DecideSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(DecideSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.DecideBatchAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.RoleId, request.BatchId, request.Request, cancellationToken);
        }
    }
}
