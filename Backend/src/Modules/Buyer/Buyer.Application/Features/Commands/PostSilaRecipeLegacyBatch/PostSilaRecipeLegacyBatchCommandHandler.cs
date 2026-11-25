using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PostSilaRecipeLegacyBatch
{
    public class PostSilaRecipeLegacyBatchCommandHandler : IRequestHandler<PostSilaRecipeLegacyBatchCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public PostSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(PostSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.PostApprovedAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.RoleId, request.BatchId, request.FailedOnly, cancellationToken);
        }
    }
}
