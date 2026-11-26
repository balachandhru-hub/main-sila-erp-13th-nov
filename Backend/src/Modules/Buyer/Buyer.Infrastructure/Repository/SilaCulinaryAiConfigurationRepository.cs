using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryAiConfigurationRepository : RepositoryBase<SilaCulinaryAiConfiguration>, ISilaCulinaryAiConfigurationRepository
    {
        public SilaCulinaryAiConfigurationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
