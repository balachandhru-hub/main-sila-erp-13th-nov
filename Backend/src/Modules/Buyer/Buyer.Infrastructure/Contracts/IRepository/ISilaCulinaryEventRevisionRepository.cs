using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventRevisionRepository : IRepositoryBase<SilaCulinaryEventRevision>
    {
        IQueryable<SilaCulinaryEventRevision> FindByConditionTracked(Expression<Func<SilaCulinaryEventRevision, bool>> expression);
    }
}
