using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryProcurementScenarioRepository : RepositoryBase<SilaCulinaryProcurementScenario>, ISilaCulinaryProcurementScenarioRepository
    {
        public SilaCulinaryProcurementScenarioRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryProcurementScenario> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementScenario, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryProcurementScenario>().Where(expression);
    }
}
