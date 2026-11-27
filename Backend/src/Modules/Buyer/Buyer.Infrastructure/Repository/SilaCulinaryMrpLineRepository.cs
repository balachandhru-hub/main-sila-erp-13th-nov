using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMrpLineRepository : RepositoryBase<SilaCulinaryMrpLine>, ISilaCulinaryMrpLineRepository
    {
        public SilaCulinaryMrpLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryMrpLine> FindByConditionTracked(Expression<Func<SilaCulinaryMrpLine, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryMrpLine>().Where(expression);
    }
}
