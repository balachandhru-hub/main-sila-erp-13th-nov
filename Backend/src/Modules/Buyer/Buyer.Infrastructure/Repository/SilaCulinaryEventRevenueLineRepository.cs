using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventRevenueLineRepository : RepositoryBase<SilaCulinaryEventRevenueLine>, ISilaCulinaryEventRevenueLineRepository
    {
        public SilaCulinaryEventRevenueLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryEventRevenueLine> FindByConditionTracked(Expression<Func<SilaCulinaryEventRevenueLine, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryEventRevenueLine>().Where(expression);
    }
}
