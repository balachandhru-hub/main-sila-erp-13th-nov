using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMrpRunRepository : RepositoryBase<SilaCulinaryMrpRun>, ISilaCulinaryMrpRunRepository
    {
        public SilaCulinaryMrpRunRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryMrpRun> FindByConditionTracked(Expression<Func<SilaCulinaryMrpRun, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryMrpRun>().Where(expression);
    }
}
