using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMaterialMappingRepository : IRepositoryBase<SilaCulinaryMaterialMapping>
    {
        IQueryable<SilaCulinaryMaterialMapping> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialMapping, bool>> expression);
    }
}
