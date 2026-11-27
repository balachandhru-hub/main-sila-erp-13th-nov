using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialMappingHistoryRepository : RepositoryBase<SilaCulinaryMaterialMappingHistory>, ISilaCulinaryMaterialMappingHistoryRepository
    {
        public SilaCulinaryMaterialMappingHistoryRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
