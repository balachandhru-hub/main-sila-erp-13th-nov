using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public class DeletePredefinedMaterialCommandHandler
        : IRequestHandler<DeletePredefinedMaterialCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeletePredefinedMaterialCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            DeletePredefinedMaterialCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting PredefinedMaterial : {request.Id}");

            var entity = await _repository.ItemBuyerMaster
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundCustomException(
                    "Predefined Material not found.",
                    "");
            }

            // Soft-delete only. Hard delete orphans recipe ingredients (no FK) and breaks
            // Recipe Studio readiness with "Material is inactive or missing" / "Price missing".
            bool usedInRecipe = await _repository.RecipeIngredient
                .FindByCondition(x => x.MaterialId == entity.Id && x.IsActive)
                .AnyAsync(cancellationToken);
            if (usedInRecipe)
            {
                _logger.LogError($"Cannot remove material used in active recipes. MaterialId: {entity.Id}, Code: {entity.MaterialCode}");
                throw new BadRequestCustomException(
                    "Material is used in a recipe.",
                    $"{entity.MaterialCode} is linked to one or more recipes. Remove it from those recipes before deactivating, or keep it active.");
            }

            entity.IsActive = false;
            _repository.ItemBuyerMaster.Update(entity);

            await _repository.SaveAsync();

            _logger.LogInfo($"PredefinedMaterial deactivated (soft delete) : {entity.Id}");

            return true;
        }
    }
}
