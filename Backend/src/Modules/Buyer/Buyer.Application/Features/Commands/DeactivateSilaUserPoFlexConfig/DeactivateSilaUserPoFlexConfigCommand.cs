using MediatR;

namespace Buyer.Application.Features.Commands.DeactivateSilaUserPoFlexConfig
{
    public class DeactivateSilaUserPoFlexConfigCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid Id { get; set; }
    }
}
