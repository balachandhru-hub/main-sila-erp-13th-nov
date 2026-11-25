using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaUserPurchasingProfileRepository : RepositoryBase<SilaUserPurchasingProfile>, ISilaUserPurchasingProfileRepository
    {
        public SilaUserPurchasingProfileRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
