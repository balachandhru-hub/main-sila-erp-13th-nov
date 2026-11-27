using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryRecipeLifecycleRepository : IRepositoryBase<SilaCulinaryRecipeLifecycle>
    {
        IQueryable<SilaCulinaryRecipeLifecycle> FindByConditionTracked(Expression<Func<SilaCulinaryRecipeLifecycle, bool>> expression);
    }
}
