using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryFinancialExceptionRepository : IRepositoryBase<SilaCulinaryFinancialException>
    {
        IQueryable<SilaCulinaryFinancialException> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialException, bool>> expression);
    }
}
