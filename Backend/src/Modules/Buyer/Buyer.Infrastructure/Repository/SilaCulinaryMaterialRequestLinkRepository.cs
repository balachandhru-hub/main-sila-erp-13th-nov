using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialRequestLinkRepository : RepositoryBase<SilaCulinaryMaterialRequestLink>, ISilaCulinaryMaterialRequestLinkRepository
    {
        public SilaCulinaryMaterialRequestLinkRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMaterialRequestLink> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialRequestLink, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMaterialRequestLink>().Where(expression);
        }
    }
}
