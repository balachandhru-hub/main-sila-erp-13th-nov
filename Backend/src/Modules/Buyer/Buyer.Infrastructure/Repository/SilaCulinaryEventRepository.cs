using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventRepository : RepositoryBase<SilaCulinaryEvent>, ISilaCulinaryEventRepository
    {
        public SilaCulinaryEventRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryEvent> FindByConditionTracked(Expression<Func<SilaCulinaryEvent, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryEvent>().Where(expression);
        }
    }
}
