using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ValidateSilaRecipeLegacyBatch
{
    public class ValidateSilaRecipeLegacyBatchCommandHandler : IRequestHandler<ValidateSilaRecipeLegacyBatchCommand, SilaRecipeLegacyBatchDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ValidateSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaRecipeLegacyBatchDetailDto> Handle(ValidateSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.ValidateBatchAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.BatchId, cancellationToken);
        }
    }
}
