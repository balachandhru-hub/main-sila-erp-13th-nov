using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ReverseSilaAdjustment
{
    public class ReverseSilaAdjustmentCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid AdjustmentId { get; set; }
        public SilaAdjustmentReverseDto Request { get; set; } = new();
    }
}
