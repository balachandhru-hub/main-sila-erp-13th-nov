using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaVoicePoSessionRepository : RepositoryBase<SilaVoicePoSession>, ISilaVoicePoSessionRepository
    {
        public SilaVoicePoSessionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
