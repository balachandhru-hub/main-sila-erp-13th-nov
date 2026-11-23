using MediatR;

namespace Buyer.Application.Features.Commands.DeactivateSilaUserPurchasingProfile
{
    public class DeactivateSilaUserPurchasingProfileCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid Id { get; set; }
        public bool AdminScope { get; set; }
    }
}
