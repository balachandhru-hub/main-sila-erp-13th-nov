using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RecipeLegacyMaterialMapRepository : RepositoryBase<RecipeLegacyMaterialMap>, IRecipeLegacyMaterialMapRepository
    {
        public RecipeLegacyMaterialMapRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
