using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMrpScenarioRepository : IRepositoryBase<SilaCulinaryMrpScenario>
    {
        IQueryable<SilaCulinaryMrpScenario> FindByConditionTracked(Expression<Func<SilaCulinaryMrpScenario, bool>> expression);
    }
}
