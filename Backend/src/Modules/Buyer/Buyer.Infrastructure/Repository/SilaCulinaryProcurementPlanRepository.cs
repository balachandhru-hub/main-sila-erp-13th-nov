using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryProcurementPlanRepository : RepositoryBase<SilaCulinaryProcurementPlan>, ISilaCulinaryProcurementPlanRepository
    {
        public SilaCulinaryProcurementPlanRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryProcurementPlan> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementPlan, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryProcurementPlan>().Where(expression);
    }
}
