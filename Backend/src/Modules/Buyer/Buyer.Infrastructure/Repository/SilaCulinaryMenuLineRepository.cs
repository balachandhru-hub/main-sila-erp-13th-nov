using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMenuLineRepository : RepositoryBase<SilaCulinaryMenuLine>, ISilaCulinaryMenuLineRepository
    {
        public SilaCulinaryMenuLineRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMenuLine> FindByConditionTracked(Expression<Func<SilaCulinaryMenuLine, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMenuLine>().Where(expression);
        }
    }
}
