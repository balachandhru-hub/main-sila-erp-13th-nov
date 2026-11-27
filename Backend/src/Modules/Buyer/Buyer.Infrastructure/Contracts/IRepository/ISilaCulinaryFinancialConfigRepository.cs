using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryFinancialConfigRepository : IRepositoryBase<SilaCulinaryFinancialConfig>
    {
        IQueryable<SilaCulinaryFinancialConfig> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialConfig, bool>> expression);
    }
}
