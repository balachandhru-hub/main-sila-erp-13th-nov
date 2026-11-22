using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ReprocessSilaRecipeLegacyBatch
{
    public class ReprocessSilaRecipeLegacyBatchCommandHandler : IRequestHandler<ReprocessSilaRecipeLegacyBatchCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ReprocessSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(ReprocessSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.PostApprovedAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.RoleId, request.BatchId, failedOnly: true, cancellationToken);
        }
    }
}
