using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryMenuLineRepository : IRepositoryBase<SilaCulinaryMenuLine>
    {
        IQueryable<SilaCulinaryMenuLine> FindByConditionTracked(Expression<Func<SilaCulinaryMenuLine, bool>> expression);
    }
}
