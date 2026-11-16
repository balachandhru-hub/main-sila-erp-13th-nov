using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class MaterialErpStockSnapshotRepository : RepositoryBase<MaterialErpStockSnapshot>, IMaterialErpStockSnapshotRepository
    {
        public MaterialErpStockSnapshotRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
