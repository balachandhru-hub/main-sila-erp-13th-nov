using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryFinancialExceptionRepository : RepositoryBase<SilaCulinaryFinancialException>, ISilaCulinaryFinancialExceptionRepository
    {
        public SilaCulinaryFinancialExceptionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryFinancialException> FindByConditionTracked(Expression<Func<SilaCulinaryFinancialException, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryFinancialException>().Where(expression);
    }
}
