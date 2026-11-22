using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UploadSilaRecipeLegacyImages
{
    public class UploadSilaRecipeLegacyImagesCommandHandler : IRequestHandler<UploadSilaRecipeLegacyImagesCommand, SilaRecipeLegacyActionResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public UploadSilaRecipeLegacyImagesCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public Task<SilaRecipeLegacyActionResultDto> Handle(UploadSilaRecipeLegacyImagesCommand request, CancellationToken cancellationToken)
        {
            return SilaRecipeLegacyMigration.UploadImagesAsync(
                _repository, _logger, _configuration, request.OrganizationId, request.UserId, request.BatchId, request.Files, cancellationToken);
        }
    }
}
