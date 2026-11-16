using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class UpdatePredefinedMaterialCommandHandler
        : IRequestHandler<UpdatePredefinedMaterialCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdatePredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdatePredefinedMaterialCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating ItemBuyerMaster : {request.Id}");

            if (string.IsNullOrWhiteSpace(request.ItemBuyerMasterDto.Description))
            {
                throw new BadRequestCustomException(
                    "Description is required.",
                    "Material Master requires a description.");
            }

            if (string.IsNullOrWhiteSpace(request.ItemBuyerMasterDto.MaterialCode))
            {
                throw new BadRequestCustomException(
                    "Material Code is required.",
                    "Material Master requires a material code.");
            }

            var entity = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.Id == request.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Item Buyer Master not found.",
                    "");
            }

            var duplicate = await _repository.ItemBuyerMaster
                .FindByCondition(x =>
                    x.MaterialCode == request.ItemBuyerMasterDto.MaterialCode &&
                    x.Id != request.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate != null)
            {
                throw new PreConditionFailedCustomException(
                    "Material Code already exists.",
                    "");
            }

            entity.Description = request.ItemBuyerMasterDto.Description.Trim();
            entity.MaterialCode = request.ItemBuyerMasterDto.MaterialCode.Trim();
            entity.MaterialGroup = request.ItemBuyerMasterDto.MaterialGroup?.Trim();

            _repository.ItemBuyerMaster.Update(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"ItemBuyerMaster updated : {entity.Id}");

            return entity.Id;
        }
    }
}