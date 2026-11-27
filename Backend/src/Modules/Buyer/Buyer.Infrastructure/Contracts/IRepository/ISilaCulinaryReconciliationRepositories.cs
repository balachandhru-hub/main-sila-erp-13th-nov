using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryReconciliationConfigRepository : IRepositoryBase<SilaCulinaryReconciliationConfig> { }
    public interface ISilaCulinaryProductionConsumptionRepository : IRepositoryBase<SilaCulinaryProductionConsumption> { }
    public interface ISilaCulinaryAdditionalMaterialRequestRepository : IRepositoryBase<SilaCulinaryAdditionalMaterialRequest> { }
    public interface ISilaCulinaryMaterialReturnRepository : IRepositoryBase<SilaCulinaryMaterialReturn> { }
    public interface ISilaCulinaryMaterialIssueReversalRepository : IRepositoryBase<SilaCulinaryMaterialIssueReversal> { }
    public interface ISilaCulinaryWasteEventRepository : IRepositoryBase<SilaCulinaryWasteEvent> { }
    public interface ISilaCulinaryFoodLeftoverRepository : IRepositoryBase<SilaCulinaryFoodLeftover> { }
    public interface ISilaCulinaryWasteDisposalRepository : IRepositoryBase<SilaCulinaryWasteDisposal> { }
    public interface ISilaCulinaryProductionOutputConfirmationRepository : IRepositoryBase<SilaCulinaryProductionOutputConfirmation> { }
    public interface ISilaCulinaryPortionSampleRepository : IRepositoryBase<SilaCulinaryPortionSample> { }
    public interface ISilaCulinaryProductionReconciliationRepository : IRepositoryBase<SilaCulinaryProductionReconciliation> { }
    public interface ISilaCulinaryProductionReconciliationLineRepository : IRepositoryBase<SilaCulinaryProductionReconciliationLine> { }
    public interface ISilaCulinaryLaborAllocationRepository : IRepositoryBase<SilaCulinaryLaborAllocation> { }
    public interface ISilaCulinaryCentralKitchenAllocationRepository : IRepositoryBase<SilaCulinaryCentralKitchenAllocation> { }
    public interface ISilaCulinaryBuffetReconciliationRepository : IRepositoryBase<SilaCulinaryBuffetReconciliation> { }
    public interface ISilaCulinaryEventClosureRepository : IRepositoryBase<SilaCulinaryEventClosure> { }
    public interface ISilaCulinaryErpReconciliationRefRepository : IRepositoryBase<SilaCulinaryErpReconciliationRef> { }
    public interface ISilaCulinaryHistoricalLearningSnapshotRepository : IRepositoryBase<SilaCulinaryHistoricalLearningSnapshot> { }
    public interface ISilaCulinaryReconciliationAuditRepository : IRepositoryBase<SilaCulinaryReconciliationAudit> { }
}
