using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class MaterialIntegrationExceptionRepository : RepositoryBase<MaterialIntegrationException>, IMaterialIntegrationExceptionRepository
    {
        public MaterialIntegrationExceptionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
