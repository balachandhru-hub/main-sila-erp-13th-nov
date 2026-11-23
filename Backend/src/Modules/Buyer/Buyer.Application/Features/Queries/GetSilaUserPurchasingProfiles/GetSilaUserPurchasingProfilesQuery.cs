using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaUserPurchasingProfiles
{
    public class GetSilaUserPurchasingProfilesQuery : IRequest<List<SilaUserPurchasingProfileDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? FilterUserId { get; set; }
        public bool AdminScope { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }
}
