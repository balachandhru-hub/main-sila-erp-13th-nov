using System.Linq.Expressions;
using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface ISilaCulinaryEventTemplateRepository : IRepositoryBase<SilaCulinaryEventTemplate>
    {
        IQueryable<SilaCulinaryEventTemplate> FindByConditionTracked(Expression<Func<SilaCulinaryEventTemplate, bool>> expression);
    }
}
