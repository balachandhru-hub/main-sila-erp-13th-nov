using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class GoodsIssueCostCenterRepository : RepositoryBase<GoodsIssueCostCenter>, IGoodsIssueCostCenterRepository
    {
        public GoodsIssueCostCenterRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
