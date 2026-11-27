using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventRepository : IRepositoryBase<SilaCulinaryEvent>
    {
        IQueryable<SilaCulinaryEvent> FindByConditionTracked(Expression<Func<SilaCulinaryEvent, bool>> expression);
    }
}
