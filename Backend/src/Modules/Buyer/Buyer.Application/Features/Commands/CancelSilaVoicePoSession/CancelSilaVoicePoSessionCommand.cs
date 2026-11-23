using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CancelSilaVoicePoSession
{
    public class CancelSilaVoicePoSessionCommand : IRequest<SilaVoicePoSessionDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
    }
}
