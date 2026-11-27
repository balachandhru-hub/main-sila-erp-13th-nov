using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMenuRepository : IRepositoryBase<SilaCulinaryMenu>
    {
        IQueryable<SilaCulinaryMenu> FindByConditionTracked(Expression<Func<SilaCulinaryMenu, bool>> expression);
    }
}
