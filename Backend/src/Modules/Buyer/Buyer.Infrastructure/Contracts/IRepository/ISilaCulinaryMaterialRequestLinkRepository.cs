using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMaterialRequestLinkRepository : IRepositoryBase<SilaCulinaryMaterialRequestLink>
    {
        IQueryable<SilaCulinaryMaterialRequestLink> FindByConditionTracked(Expression<Func<SilaCulinaryMaterialRequestLink, bool>> expression);
    }
}
