using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryMaterialFeedbackRepository : RepositoryBase<SilaCulinaryMaterialFeedback>, ISilaCulinaryMaterialFeedbackRepository
    {
        public SilaCulinaryMaterialFeedbackRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
