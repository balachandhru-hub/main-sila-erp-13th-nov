using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialRequestHistoryRepository : RepositoryBase<SilaCulinaryMaterialRequestHistory>, ISilaCulinaryMaterialRequestHistoryRepository
    {
        public SilaCulinaryMaterialRequestHistoryRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMaterialRequestHistory> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialRequestHistory, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMaterialRequestHistory>().Where(expression);
        }
    }
}
