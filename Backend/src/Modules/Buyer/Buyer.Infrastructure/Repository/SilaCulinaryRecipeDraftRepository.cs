using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryRecipeDraftRepository : RepositoryBase<SilaCulinaryRecipeDraft>, ISilaCulinaryRecipeDraftRepository
    {
        public SilaCulinaryRecipeDraftRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
