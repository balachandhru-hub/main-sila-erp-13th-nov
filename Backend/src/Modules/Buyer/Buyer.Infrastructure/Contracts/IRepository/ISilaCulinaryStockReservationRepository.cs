using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryStockReservationRepository : IRepositoryBase<SilaCulinaryStockReservation>
    {
        IQueryable<SilaCulinaryStockReservation> FindByConditionTracked(Expression<Func<SilaCulinaryStockReservation, bool>> expression);
    }
}
