using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventRevisionRepository : RepositoryBase<SilaCulinaryEventRevision>, ISilaCulinaryEventRevisionRepository
    {
        public SilaCulinaryEventRevisionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryEventRevision> FindByConditionTracked(Expression<Func<SilaCulinaryEventRevision, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryEventRevision>().Where(expression);
        }
    }
}
