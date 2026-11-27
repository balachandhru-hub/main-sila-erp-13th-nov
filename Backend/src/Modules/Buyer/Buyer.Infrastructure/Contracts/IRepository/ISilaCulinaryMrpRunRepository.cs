using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMrpRunRepository : IRepositoryBase<SilaCulinaryMrpRun>
    {
        IQueryable<SilaCulinaryMrpRun> FindByConditionTracked(Expression<Func<SilaCulinaryMrpRun, bool>> expression);
    }
}
