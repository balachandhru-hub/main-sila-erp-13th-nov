using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialIndexStateRepository : RepositoryBase<SilaCulinaryMaterialIndexState>, ISilaCulinaryMaterialIndexStateRepository
    {
        public SilaCulinaryMaterialIndexStateRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
