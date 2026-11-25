using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.ResolveSilaUserPurchasingDefaults
{
    public class ResolveSilaUserPurchasingDefaultsQuery : IRequest<SilaUserPurchasingDefaultsDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public ResolveSilaUserPurchasingDefaultsRequestDto Request { get; set; } = new();
    }
}
