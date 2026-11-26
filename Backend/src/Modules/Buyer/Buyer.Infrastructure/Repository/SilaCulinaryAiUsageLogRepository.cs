using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryAiUsageLogRepository : RepositoryBase<SilaCulinaryAiUsageLog>, ISilaCulinaryAiUsageLogRepository
    {
        public SilaCulinaryAiUsageLogRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
