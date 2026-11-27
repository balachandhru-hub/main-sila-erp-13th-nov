using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryFinancialScenarioRepository : RepositoryBase<SilaCulinaryFinancialScenario>, ISilaCulinaryFinancialScenarioRepository
    {
        public SilaCulinaryFinancialScenarioRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryFinancialScenario> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialScenario, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryFinancialScenario>().Where(expression);
    }
}
