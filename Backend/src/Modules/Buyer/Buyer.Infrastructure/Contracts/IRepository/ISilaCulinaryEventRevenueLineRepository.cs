using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventRevenueLineRepository : IRepositoryBase<SilaCulinaryEventRevenueLine>
    {
        IQueryable<SilaCulinaryEventRevenueLine> FindByConditionTracked(Expression<Func<SilaCulinaryEventRevenueLine, bool>> expression);
    }
}
