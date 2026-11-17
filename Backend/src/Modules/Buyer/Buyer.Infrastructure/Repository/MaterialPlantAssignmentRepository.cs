using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class MaterialPlantAssignmentRepository : RepositoryBase<MaterialPlantAssignment>, IMaterialPlantAssignmentRepository
    {
        public MaterialPlantAssignmentRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
