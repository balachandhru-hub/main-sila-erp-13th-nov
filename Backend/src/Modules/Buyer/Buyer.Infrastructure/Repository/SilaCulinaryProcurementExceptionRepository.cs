using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryProcurementExceptionRepository : RepositoryBase<SilaCulinaryProcurementException>, ISilaCulinaryProcurementExceptionRepository
    {
        public SilaCulinaryProcurementExceptionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryProcurementException> FindByConditionTracked(Expression<Func<SilaCulinaryProcurementException, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryProcurementException>().Where(expression);
    }
}
