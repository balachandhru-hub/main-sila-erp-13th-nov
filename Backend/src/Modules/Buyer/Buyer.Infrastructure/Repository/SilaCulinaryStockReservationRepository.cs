using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryStockReservationRepository : RepositoryBase<SilaCulinaryStockReservation>, ISilaCulinaryStockReservationRepository
    {
        public SilaCulinaryStockReservationRepository(RepositoryContext repositoryContext) : base(repositoryContext) { }

        public IQueryable<SilaCulinaryStockReservation> FindByConditionTracked(Expression<Func<SilaCulinaryStockReservation, bool>> expression)
            => RepositoryContext.Set<SilaCulinaryStockReservation>().Where(expression);
    }
}
