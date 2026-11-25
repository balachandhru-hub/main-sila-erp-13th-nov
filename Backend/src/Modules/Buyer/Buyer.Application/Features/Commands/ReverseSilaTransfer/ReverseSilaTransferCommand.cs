using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ReverseSilaTransfer
{
    public class ReverseSilaTransferCommand : IRequest<SilaTransferReverseResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid TransferId { get; set; }
        public SilaTransferReverseDto Request { get; set; } = new();
    }
}
