using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SubmitSilaRecipeLegacyBatch
{
    public class SubmitSilaRecipeLegacyBatchCommandHandler : IRequestHandler<SubmitSilaRecipeLegacyBatchCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SubmitSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(SubmitSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.SubmitForApprovalAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.RoleId, request.BatchId, cancellationToken);
        }
    }
}
