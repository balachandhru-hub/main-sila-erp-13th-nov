using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryRecipeLifecycleRepository : RepositoryBase<SilaCulinaryRecipeLifecycle>, ISilaCulinaryRecipeLifecycleRepository
    {
        public SilaCulinaryRecipeLifecycleRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryRecipeLifecycle> FindByConditionTracked(Expression<Func<SilaCulinaryRecipeLifecycle, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryRecipeLifecycle>().Where(expression);
        }
    }
}
