using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryRecipeRevisionRepository : RepositoryBase<SilaCulinaryRecipeRevision>, ISilaCulinaryRecipeRevisionRepository
    {
        public SilaCulinaryRecipeRevisionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
