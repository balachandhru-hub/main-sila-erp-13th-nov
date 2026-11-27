using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryAnalyticsConfigRepository : IRepositoryBase<SilaCulinaryAnalyticsConfig>
    {
        IQueryable<SilaCulinaryAnalyticsConfig> FindByConditionTracked(Expression<Func<SilaCulinaryAnalyticsConfig, bool>> expression);
    }

    public interface ISilaCulinarySimulationRepository : IRepositoryBase<SilaCulinarySimulation>
    {
        IQueryable<SilaCulinarySimulation> FindByConditionTracked(Expression<Func<SilaCulinarySimulation, bool>> expression);
    }

    public interface ISilaCulinaryOptimizationJobRepository : IRepositoryBase<SilaCulinaryOptimizationJob>
    {
        IQueryable<SilaCulinaryOptimizationJob> FindByConditionTracked(Expression<Func<SilaCulinaryOptimizationJob, bool>> expression);
    }

    public interface ISilaCulinaryRecommendationRepository : IRepositoryBase<SilaCulinaryRecommendation>
    {
        IQueryable<SilaCulinaryRecommendation> FindByConditionTracked(Expression<Func<SilaCulinaryRecommendation, bool>> expression);
    }

    public interface ISilaCulinaryWasteRecordRepository : IRepositoryBase<SilaCulinaryWasteRecord>
    {
        IQueryable<SilaCulinaryWasteRecord> FindByConditionTracked(Expression<Func<SilaCulinaryWasteRecord, bool>> expression);
    }

    public interface ISilaCulinaryAnalyticsAuditRepository : IRepositoryBase<SilaCulinaryAnalyticsAudit>
    {
        IQueryable<SilaCulinaryAnalyticsAudit> FindByConditionTracked(Expression<Func<SilaCulinaryAnalyticsAudit, bool>> expression);
    }
}
