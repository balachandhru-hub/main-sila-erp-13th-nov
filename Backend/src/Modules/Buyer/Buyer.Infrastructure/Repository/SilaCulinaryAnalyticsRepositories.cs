using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryAnalyticsConfigRepository : RepositoryBase<SilaCulinaryAnalyticsConfig>, ISilaCulinaryAnalyticsConfigRepository
    {
        public SilaCulinaryAnalyticsConfigRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinaryAnalyticsConfig> FindByConditionTracked(Expression<Func<SilaCulinaryAnalyticsConfig, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryAnalyticsConfig>().Where(expression);
    }

    public class SilaCulinarySimulationRepository : RepositoryBase<SilaCulinarySimulation>, ISilaCulinarySimulationRepository
    {
        public SilaCulinarySimulationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinarySimulation> FindByConditionTracked(Expression<Func<SilaCulinarySimulation, bool>> expression)
            => RepositoryContext.Set<SilaCulinarySimulation>().Where(expression);
    }

    public class SilaCulinaryOptimizationJobRepository : RepositoryBase<SilaCulinaryOptimizationJob>, ISilaCulinaryOptimizationJobRepository
    {
        public SilaCulinaryOptimizationJobRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinaryOptimizationJob> FindByConditionTracked(Expression<Func<SilaCulinaryOptimizationJob, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryOptimizationJob>().Where(expression);
    }

    public class SilaCulinaryRecommendationRepository : RepositoryBase<SilaCulinaryRecommendation>, ISilaCulinaryRecommendationRepository
    {
        public SilaCulinaryRecommendationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinaryRecommendation> FindByConditionTracked(Expression<Func<SilaCulinaryRecommendation, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryRecommendation>().Where(expression);
    }

    public class SilaCulinaryWasteRecordRepository : RepositoryBase<SilaCulinaryWasteRecord>, ISilaCulinaryWasteRecordRepository
    {
        public SilaCulinaryWasteRecordRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinaryWasteRecord> FindByConditionTracked(Expression<Func<SilaCulinaryWasteRecord, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryWasteRecord>().Where(expression);
    }

    public class SilaCulinaryAnalyticsAuditRepository : RepositoryBase<SilaCulinaryAnalyticsAudit>, ISilaCulinaryAnalyticsAuditRepository
    {
        public SilaCulinaryAnalyticsAuditRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }
        public IQueryable<SilaCulinaryAnalyticsAudit> FindByConditionTracked(Expression<Func<SilaCulinaryAnalyticsAudit, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryAnalyticsAudit>().Where(expression);
    }
}
