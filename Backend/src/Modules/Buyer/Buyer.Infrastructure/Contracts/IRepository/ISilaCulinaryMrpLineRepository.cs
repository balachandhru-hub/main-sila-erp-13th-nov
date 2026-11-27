using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMrpLineRepository : IRepositoryBase<SilaCulinaryMrpLine>
    {
        IQueryable<SilaCulinaryMrpLine> FindByConditionTracked(Expression<Func<SilaCulinaryMrpLine, bool>> expression);
    }
}
