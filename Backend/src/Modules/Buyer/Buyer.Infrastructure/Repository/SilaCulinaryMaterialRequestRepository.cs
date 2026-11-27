using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialRequestRepository : RepositoryBase<SilaCulinaryMaterialRequest>, ISilaCulinaryMaterialRequestRepository
    {
        public SilaCulinaryMaterialRequestRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMaterialRequest> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialRequest, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMaterialRequest>().Where(expression);
        }
    }
}
