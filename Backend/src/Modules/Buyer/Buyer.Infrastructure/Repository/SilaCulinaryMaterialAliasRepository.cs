using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialAliasRepository : RepositoryBase<SilaCulinaryMaterialAlias>, ISilaCulinaryMaterialAliasRepository
    {
        public SilaCulinaryMaterialAliasRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
