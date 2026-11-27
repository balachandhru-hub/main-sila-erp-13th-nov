using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventExpenseLineRepository : IRepositoryBase<SilaCulinaryEventExpenseLine>
    {
        IQueryable<SilaCulinaryEventExpenseLine> FindByConditionTracked(Expression<Func<SilaCulinaryEventExpenseLine, bool>> expression);
    }
}
