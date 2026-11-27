using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMrpSnapshotRepository : RepositoryBase<SilaCulinaryMrpSnapshot>, ISilaCulinaryMrpSnapshotRepository
    {
        public SilaCulinaryMrpSnapshotRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryMrpSnapshot> FindByConditionTracked(Expression<Func<SilaCulinaryMrpSnapshot, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryMrpSnapshot>().Where(expression);
    }
}
