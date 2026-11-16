using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class MaterialStorageLocationAssignmentRepository : RepositoryBase<MaterialStorageLocationAssignment>, IMaterialStorageLocationAssignmentRepository
    {
        public MaterialStorageLocationAssignmentRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
