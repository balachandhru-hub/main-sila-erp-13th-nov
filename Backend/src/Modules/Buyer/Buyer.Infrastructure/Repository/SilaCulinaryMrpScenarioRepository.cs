using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMrpScenarioRepository : RepositoryBase<SilaCulinaryMrpScenario>, ISilaCulinaryMrpScenarioRepository
    {
        public SilaCulinaryMrpScenarioRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryMrpScenario> FindByConditionTracked(Expression<Func<SilaCulinaryMrpScenario, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryMrpScenario>().Where(expression);
    }
}
