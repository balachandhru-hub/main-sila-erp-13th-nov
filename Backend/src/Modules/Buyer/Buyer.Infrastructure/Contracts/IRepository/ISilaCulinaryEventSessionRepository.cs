using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventSessionRepository : IRepositoryBase<SilaCulinaryEventSession>
    {
        IQueryable<SilaCulinaryEventSession> FindByConditionTracked(Expression<Func<SilaCulinaryEventSession, bool>> expression);
    }
}
