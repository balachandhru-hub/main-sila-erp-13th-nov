using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryProcurementLineRepository : IRepositoryBase<SilaCulinaryProcurementLine>
    {
        IQueryable<SilaCulinaryProcurementLine> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementLine, bool>> expression);
    }
}
