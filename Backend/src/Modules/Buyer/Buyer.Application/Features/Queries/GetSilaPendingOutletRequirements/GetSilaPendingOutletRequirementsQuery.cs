using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaPendingOutletRequirements
{
    public class GetSilaPendingOutletRequirementsQuery : IRequest<SilaPendingOutletRequirementsDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid? DestinationOutletId { get; set; }
        public Guid? WeeklyBucketId { get; set; }
        public Guid? WishlistId { get; set; }
        public Guid? MaterialId { get; set; }
        public string? SourceLocationId { get; set; }
        public bool CountOnly { get; set; }
    }
}
