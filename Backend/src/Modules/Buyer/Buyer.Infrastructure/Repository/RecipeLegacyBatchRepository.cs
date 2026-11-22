using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RecipeLegacyBatchRepository : RepositoryBase<RecipeLegacyBatch>, IRecipeLegacyBatchRepository
    {
        public RecipeLegacyBatchRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
