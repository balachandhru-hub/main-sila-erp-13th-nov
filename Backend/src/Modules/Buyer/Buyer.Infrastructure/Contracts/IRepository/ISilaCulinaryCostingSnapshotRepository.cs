using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryCostingSnapshotRepository : IRepositoryBase<SilaCulinaryCostingSnapshot>
    {
        IQueryable<SilaCulinaryCostingSnapshot> FindByConditionTracked(Expression<Func<SilaCulinaryCostingSnapshot, bool>> expression);
    }
}
