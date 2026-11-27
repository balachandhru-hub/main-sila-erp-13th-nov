using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMrpExceptionRepository : IRepositoryBase<SilaCulinaryMrpException>
    {
        IQueryable<SilaCulinaryMrpException> FindByConditionTracked(Expression<Func<SilaCulinaryMrpException, bool>> expression);
    }
}
