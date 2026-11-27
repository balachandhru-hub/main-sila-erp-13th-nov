using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryCostingSnapshotRepository : RepositoryBase<SilaCulinaryCostingSnapshot>, ISilaCulinaryCostingSnapshotRepository
    {
        public SilaCulinaryCostingSnapshotRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryCostingSnapshot> FindByConditionTracked(Expression<Func<SilaCulinaryCostingSnapshot, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryCostingSnapshot>().Where(expression);
        }
    }
}
