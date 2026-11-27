using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventFinancialPlanRepository : RepositoryBase<SilaCulinaryEventFinancialPlan>, ISilaCulinaryEventFinancialPlanRepository
    {
        public SilaCulinaryEventFinancialPlanRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryEventFinancialPlan> FindByConditionTracked(Expression<Func<SilaCulinaryEventFinancialPlan, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryEventFinancialPlan>().Where(expression);
    }
}
