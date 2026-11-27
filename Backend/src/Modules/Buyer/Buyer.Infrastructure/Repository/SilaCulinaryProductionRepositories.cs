using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryKitchenRepository : RepositoryBase<SilaCulinaryKitchen>, ISilaCulinaryKitchenRepository
    {
        public SilaCulinaryKitchenRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryWorkstationRepository : RepositoryBase<SilaCulinaryWorkstation>, ISilaCulinaryWorkstationRepository
    {
        public SilaCulinaryWorkstationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryEquipmentRepository : RepositoryBase<SilaCulinaryEquipment>, ISilaCulinaryEquipmentRepository
    {
        public SilaCulinaryEquipmentRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryStaffAssignmentRepository : RepositoryBase<SilaCulinaryStaffAssignment>, ISilaCulinaryStaffAssignmentRepository
    {
        public SilaCulinaryStaffAssignmentRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionDemandRepository : RepositoryBase<SilaCulinaryProductionDemand>, ISilaCulinaryProductionDemandRepository
    {
        public SilaCulinaryProductionDemandRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionPlanRepository : RepositoryBase<SilaCulinaryProductionPlan>, ISilaCulinaryProductionPlanRepository
    {
        public SilaCulinaryProductionPlanRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionPlanLineRepository : RepositoryBase<SilaCulinaryProductionPlanLine>, ISilaCulinaryProductionPlanLineRepository
    {
        public SilaCulinaryProductionPlanLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionPlanVersionRepository : RepositoryBase<SilaCulinaryProductionPlanVersion>, ISilaCulinaryProductionPlanVersionRepository
    {
        public SilaCulinaryProductionPlanVersionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionOrderRepository : RepositoryBase<SilaCulinaryProductionOrder>, ISilaCulinaryProductionOrderRepository
    {
        public SilaCulinaryProductionOrderRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionOrderIngredientRepository : RepositoryBase<SilaCulinaryProductionOrderIngredient>, ISilaCulinaryProductionOrderIngredientRepository
    {
        public SilaCulinaryProductionOrderIngredientRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionBatchRepository : RepositoryBase<SilaCulinaryProductionBatch>, ISilaCulinaryProductionBatchRepository
    {
        public SilaCulinaryProductionBatchRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionTaskRepository : RepositoryBase<SilaCulinaryProductionTask>, ISilaCulinaryProductionTaskRepository
    {
        public SilaCulinaryProductionTaskRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionPickingLineRepository : RepositoryBase<SilaCulinaryProductionPickingLine>, ISilaCulinaryProductionPickingLineRepository
    {
        public SilaCulinaryProductionPickingLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionMaterialIssueRefRepository : RepositoryBase<SilaCulinaryProductionMaterialIssueRef>, ISilaCulinaryProductionMaterialIssueRefRepository
    {
        public SilaCulinaryProductionMaterialIssueRefRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionQualityCheckpointRepository : RepositoryBase<SilaCulinaryProductionQualityCheckpoint>, ISilaCulinaryProductionQualityCheckpointRepository
    {
        public SilaCulinaryProductionQualityCheckpointRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionQualityResultRepository : RepositoryBase<SilaCulinaryProductionQualityResult>, ISilaCulinaryProductionQualityResultRepository
    {
        public SilaCulinaryProductionQualityResultRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionExceptionRepository : RepositoryBase<SilaCulinaryProductionException>, ISilaCulinaryProductionExceptionRepository
    {
        public SilaCulinaryProductionExceptionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionDispatchRepository : RepositoryBase<SilaCulinaryProductionDispatch>, ISilaCulinaryProductionDispatchRepository
    {
        public SilaCulinaryProductionDispatchRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionAuditRepository : RepositoryBase<SilaCulinaryProductionAudit>, ISilaCulinaryProductionAuditRepository
    {
        public SilaCulinaryProductionAuditRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }
}
