using System.Linq.Expressions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class SilaCulinaryEventTemplateRepository : RepositoryBase<SilaCulinaryEventTemplate>, ISilaCulinaryEventTemplateRepository
    {
        public SilaCulinaryEventTemplateRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public IQueryable<SilaCulinaryEventTemplate> FindByConditionTracked(Expression<Func<SilaCulinaryEventTemplate, bool>> expression)
        {
            return RepositoryContext.Set<SilaCulinaryEventTemplate>().Where(expression);
        }
    }
}
