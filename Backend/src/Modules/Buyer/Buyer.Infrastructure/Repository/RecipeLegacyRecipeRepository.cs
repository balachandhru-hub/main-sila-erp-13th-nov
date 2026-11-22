using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RecipeLegacyRecipeRepository : RepositoryBase<RecipeLegacyRecipe>, IRecipeLegacyRecipeRepository
    {
        public RecipeLegacyRecipeRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
