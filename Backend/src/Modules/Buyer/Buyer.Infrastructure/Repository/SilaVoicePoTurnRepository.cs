using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaVoicePoTurnRepository : RepositoryBase<SilaVoicePoTurn>, ISilaVoicePoTurnRepository
    {
        public SilaVoicePoTurnRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
