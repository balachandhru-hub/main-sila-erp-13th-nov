using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaUserPoFlexConfigRepository : RepositoryBase<SilaUserPoFlexConfig>, ISilaUserPoFlexConfigRepository
    {
        public SilaUserPoFlexConfigRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }

    public class SilaUserPoFlexParamRepository : RepositoryBase<SilaUserPoFlexParam>, ISilaUserPoFlexParamRepository
    {
        public SilaUserPoFlexParamRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }

    public class SilaUserPoFlexConditionRepository : RepositoryBase<SilaUserPoFlexCondition>, ISilaUserPoFlexConditionRepository
    {
        public SilaUserPoFlexConditionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
