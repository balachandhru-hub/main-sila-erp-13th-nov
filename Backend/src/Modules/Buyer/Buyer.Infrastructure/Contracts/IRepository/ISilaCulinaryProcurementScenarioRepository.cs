using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryProcurementScenarioRepository : IRepositoryBase<SilaCulinaryProcurementScenario>
    {
        IQueryable<SilaCulinaryProcurementScenario> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementScenario, bool>> expression);
    }
}
