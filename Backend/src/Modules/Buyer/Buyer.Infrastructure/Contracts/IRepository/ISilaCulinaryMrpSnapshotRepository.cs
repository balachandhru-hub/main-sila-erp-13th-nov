using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMrpSnapshotRepository : IRepositoryBase<SilaCulinaryMrpSnapshot>
    {
        IQueryable<SilaCulinaryMrpSnapshot> FindByConditionTracked(Expression<Func<SilaCulinaryMrpSnapshot, bool>> expression);
    }
}
