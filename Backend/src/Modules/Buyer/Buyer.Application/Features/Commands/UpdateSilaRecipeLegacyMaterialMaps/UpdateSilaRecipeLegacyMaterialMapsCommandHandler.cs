using Buyer.Application.Features.Shared;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateSilaRecipeLegacyMaterialMaps
{
    public class UpdateSilaRecipeLegacyMaterialMapsCommandHandler : IRequestHandler<UpdateSilaRecipeLegacyMaterialMapsCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSilaRecipeLegacyMaterialMapsCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateSilaRecipeLegacyMaterialMapsCommand request, CancellationToken cancellationToken)
        {
            await SilaRecipeLegacyMigration.UpdateMaterialMapsAsync(
                _repository, _logger, request.OrganizationId, request.UserId, request.BatchId, request.Mappings, cancellationToken);
            return Unit.Value;
        }
    }
}
