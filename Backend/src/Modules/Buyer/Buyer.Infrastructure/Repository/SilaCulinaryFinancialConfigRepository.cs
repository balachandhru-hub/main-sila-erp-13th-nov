using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryFinancialConfigRepository : RepositoryBase<SilaCulinaryFinancialConfig>, ISilaCulinaryFinancialConfigRepository
    {
        public SilaCulinaryFinancialConfigRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryFinancialConfig> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialConfig, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryFinancialConfig>().Where(expression);
    }
}
