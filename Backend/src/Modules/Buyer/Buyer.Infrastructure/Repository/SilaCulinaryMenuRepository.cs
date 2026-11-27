using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMenuRepository : RepositoryBase<SilaCulinaryMenu>, ISilaCulinaryMenuRepository
    {
        public SilaCulinaryMenuRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMenu> FindByConditionTracked(Expression<Func<SilaCulinaryMenu, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMenu>().Where(expression);
        }
    }
}
