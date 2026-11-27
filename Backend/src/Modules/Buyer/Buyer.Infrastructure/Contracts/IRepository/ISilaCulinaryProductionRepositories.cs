using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryKitchenRepository : IRepositoryBase<SilaCulinaryKitchen> { }
    public interface ISilaCulinaryWorkstationRepository : IRepositoryBase<SilaCulinaryWorkstation> { }
    public interface ISilaCulinaryEquipmentRepository : IRepositoryBase<SilaCulinaryEquipment> { }
    public interface ISilaCulinaryStaffAssignmentRepository : IRepositoryBase<SilaCulinaryStaffAssignment> { }
    public interface ISilaCulinaryProductionDemandRepository : IRepositoryBase<SilaCulinaryProductionDemand> { }
    public interface ISilaCulinaryProductionPlanRepository : IRepositoryBase<SilaCulinaryProductionPlan> { }
    public interface ISilaCulinaryProductionPlanLineRepository : IRepositoryBase<SilaCulinaryProductionPlanLine> { }
    public interface ISilaCulinaryProductionPlanVersionRepository : IRepositoryBase<SilaCulinaryProductionPlanVersion> { }
    public interface ISilaCulinaryProductionOrderRepository : IRepositoryBase<SilaCulinaryProductionOrder> { }
    public interface ISilaCulinaryProductionOrderIngredientRepository : IRepositoryBase<SilaCulinaryProductionOrderIngredient> { }
    public interface ISilaCulinaryProductionBatchRepository : IRepositoryBase<SilaCulinaryProductionBatch> { }
    public interface ISilaCulinaryProductionTaskRepository : IRepositoryBase<SilaCulinaryProductionTask> { }
    public interface ISilaCulinaryProductionPickingLineRepository : IRepositoryBase<SilaCulinaryProductionPickingLine> { }
    public interface ISilaCulinaryProductionMaterialIssueRefRepository : IRepositoryBase<SilaCulinaryProductionMaterialIssueRef> { }
    public interface ISilaCulinaryProductionQualityCheckpointRepository : IRepositoryBase<SilaCulinaryProductionQualityCheckpoint> { }
    public interface ISilaCulinaryProductionQualityResultRepository : IRepositoryBase<SilaCulinaryProductionQualityResult> { }
    public interface ISilaCulinaryProductionExceptionRepository : IRepositoryBase<SilaCulinaryProductionException> { }
    public interface ISilaCulinaryProductionDispatchRepository : IRepositoryBase<SilaCulinaryProductionDispatch> { }
    public interface ISilaCulinaryProductionAuditRepository : IRepositoryBase<SilaCulinaryProductionAudit> { }
}
