using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryProcurementPlanRepository : IRepositoryBase<SilaCulinaryProcurementPlan>
    {
        IQueryable<SilaCulinaryProcurementPlan> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementPlan, bool>> expression);
    }
}
