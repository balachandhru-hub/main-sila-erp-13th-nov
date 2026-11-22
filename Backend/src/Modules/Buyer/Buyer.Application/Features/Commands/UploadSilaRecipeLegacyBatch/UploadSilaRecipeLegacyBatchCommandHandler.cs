using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UploadSilaRecipeLegacyBatch
{
    public class UploadSilaRecipeLegacyBatchCommandHandler : IRequestHandler<UploadSilaRecipeLegacyBatchCommand, SilaRecipeLegacyBatchDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public UploadSilaRecipeLegacyBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public Task<SilaRecipeLegacyBatchDetailDto> Handle(UploadSilaRecipeLegacyBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.UploadAndStageAsync(
                _repository, _logger, _configuration, request.OrganizationId, request.UserId, request.FileName, request.Content, cancellationToken);
        }
    }
}
