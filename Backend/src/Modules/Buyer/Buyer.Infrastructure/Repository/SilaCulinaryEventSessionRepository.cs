using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventSessionRepository : RepositoryBase<SilaCulinaryEventSession>, ISilaCulinaryEventSessionRepository
    {
        public SilaCulinaryEventSessionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryEventSession> FindByConditionTracked(Expression<Func<SilaCulinaryEventSession, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryEventSession>().Where(expression);
        }
    }
}
