using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMaterialRequestRepository : IRepositoryBase<SilaCulinaryMaterialRequest>
    {
        IQueryable<SilaCulinaryMaterialRequest> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialRequest, bool>> expression);
    }
}
