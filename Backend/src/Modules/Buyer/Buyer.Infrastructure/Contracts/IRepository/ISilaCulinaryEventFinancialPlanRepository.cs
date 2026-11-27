using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventFinancialPlanRepository : IRepositoryBase<SilaCulinaryEventFinancialPlan>
    {
        IQueryable<SilaCulinaryEventFinancialPlan> FindByConditionTracked(Expression<Func<SilaCulinaryEventFinancialPlan, bool>> expression);
    }
}
