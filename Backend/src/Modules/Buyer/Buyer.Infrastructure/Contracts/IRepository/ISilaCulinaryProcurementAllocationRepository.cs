using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryProcurementAllocationRepository : IRepositoryBase<SilaCulinaryProcurementAllocation>
    {
        IQueryable<SilaCulinaryProcurementAllocation> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementAllocation, bool>> expression);
    }
}
