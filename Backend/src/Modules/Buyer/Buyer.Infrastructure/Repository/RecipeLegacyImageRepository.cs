using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RecipeLegacyImageRepository : RepositoryBase<RecipeLegacyImage>, IRecipeLegacyImageRepository
    {
        public RecipeLegacyImageRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}