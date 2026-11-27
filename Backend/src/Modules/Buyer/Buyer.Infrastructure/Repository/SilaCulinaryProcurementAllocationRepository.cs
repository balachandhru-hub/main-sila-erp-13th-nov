using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryProcurementAllocationRepository : RepositoryBase<SilaCulinaryProcurementAllocation>, ISilaCulinaryProcurementAllocationRepository
    {
        public SilaCulinaryProcurementAllocationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryProcurementAllocation> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementAllocation, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryProcurementAllocation>().Where(expression);
    }
}
