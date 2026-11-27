using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialMappingRepository : RepositoryBase<SilaCulinaryMaterialMapping>, ISilaCulinaryMaterialMappingRepository
    {
        public SilaCulinaryMaterialMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryMaterialMapping> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialMapping, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryMaterialMapping>().Where(expression);
        }
    }
}
