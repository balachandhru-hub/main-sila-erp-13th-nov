using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMrpExceptionRepository : RepositoryBase<SilaCulinaryMrpException>, ISilaCulinaryMrpExceptionRepository
    {
        public SilaCulinaryMrpExceptionRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryMrpException> FindByConditionTracked(Expression<Func<SilaCulinaryMrpException, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryMrpException>().Where(expression);
    }
}
