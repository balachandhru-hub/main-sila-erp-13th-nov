using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryReconciliationConfigRepository : RepositoryBase<SilaCulinaryReconciliationConfig>, ISilaCulinaryReconciliationConfigRepository
    {
        public SilaCulinaryReconciliationConfigRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionConsumptionRepository : RepositoryBase<SilaCulinaryProductionConsumption>, ISilaCulinaryProductionConsumptionRepository
    {
        public SilaCulinaryProductionConsumptionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryAdditionalMaterialRequestRepository : RepositoryBase<SilaCulinaryAdditionalMaterialRequest>, ISilaCulinaryAdditionalMaterialRequestRepository
    {
        public SilaCulinaryAdditionalMaterialRequestRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryMaterialReturnRepository : RepositoryBase<SilaCulinaryMaterialReturn>, ISilaCulinaryMaterialReturnRepository
    {
        public SilaCulinaryMaterialReturnRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryMaterialIssueReversalRepository : RepositoryBase<SilaCulinaryMaterialIssueReversal>, ISilaCulinaryMaterialIssueReversalRepository
    {
        public SilaCulinaryMaterialIssueReversalRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryWasteEventRepository : RepositoryBase<SilaCulinaryWasteEvent>, ISilaCulinaryWasteEventRepository
    {
        public SilaCulinaryWasteEventRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryFoodLeftoverRepository : RepositoryBase<SilaCulinaryFoodLeftover>, ISilaCulinaryFoodLeftoverRepository
    {
        public SilaCulinaryFoodLeftoverRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryWasteDisposalRepository : RepositoryBase<SilaCulinaryWasteDisposal>, ISilaCulinaryWasteDisposalRepository
    {
        public SilaCulinaryWasteDisposalRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionOutputConfirmationRepository : RepositoryBase<SilaCulinaryProductionOutputConfirmation>, ISilaCulinaryProductionOutputConfirmationRepository
    {
        public SilaCulinaryProductionOutputConfirmationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryPortionSampleRepository : RepositoryBase<SilaCulinaryPortionSample>, ISilaCulinaryPortionSampleRepository
    {
        public SilaCulinaryPortionSampleRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionReconciliationRepository : RepositoryBase<SilaCulinaryProductionReconciliation>, ISilaCulinaryProductionReconciliationRepository
    {
        public SilaCulinaryProductionReconciliationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryProductionReconciliationLineRepository : RepositoryBase<SilaCulinaryProductionReconciliationLine>, ISilaCulinaryProductionReconciliationLineRepository
    {
        public SilaCulinaryProductionReconciliationLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryLaborAllocationRepository : RepositoryBase<SilaCulinaryLaborAllocation>, ISilaCulinaryLaborAllocationRepository
    {
        public SilaCulinaryLaborAllocationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryCentralKitchenAllocationRepository : RepositoryBase<SilaCulinaryCentralKitchenAllocation>, ISilaCulinaryCentralKitchenAllocationRepository
    {
        public SilaCulinaryCentralKitchenAllocationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryBuffetReconciliationRepository : RepositoryBase<SilaCulinaryBuffetReconciliation>, ISilaCulinaryBuffetReconciliationRepository
    {
        public SilaCulinaryBuffetReconciliationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryEventClosureRepository : RepositoryBase<SilaCulinaryEventClosure>, ISilaCulinaryEventClosureRepository
    {
        public SilaCulinaryEventClosureRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryErpReconciliationRefRepository : RepositoryBase<SilaCulinaryErpReconciliationRef>, ISilaCulinaryErpReconciliationRefRepository
    {
        public SilaCulinaryErpReconciliationRefRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryHistoricalLearningSnapshotRepository : RepositoryBase<SilaCulinaryHistoricalLearningSnapshot>, ISilaCulinaryHistoricalLearningSnapshotRepository
    {
        public SilaCulinaryHistoricalLearningSnapshotRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }

    public class SilaCulinaryReconciliationAuditRepository : RepositoryBase<SilaCulinaryReconciliationAudit>, ISilaCulinaryReconciliationAuditRepository
    {
        public SilaCulinaryReconciliationAuditRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
    }
}
