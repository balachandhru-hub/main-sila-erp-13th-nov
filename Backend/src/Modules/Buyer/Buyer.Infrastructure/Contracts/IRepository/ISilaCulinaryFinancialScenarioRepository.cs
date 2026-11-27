using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryFinancialScenarioRepository : IRepositoryBase<SilaCulinaryFinancialScenario>
    {
        IQueryable<SilaCulinaryFinancialScenario> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialScenario, bool>> expression);
    }
}
