using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryProcurementLineRepository : RepositoryBase<SilaCulinaryProcurementLine>, ISilaCulinaryProcurementLineRepository
    {
        public SilaCulinaryProcurementLineRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryProcurementLine> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementLine, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryProcurementLine>().Where(expression);
    }
}
