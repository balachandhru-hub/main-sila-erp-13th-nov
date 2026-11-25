using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ConfirmSilaVoicePoDraft
{
    public class ConfirmSilaVoicePoDraftCommand : IRequest<SilaVoicePoSessionDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public SilaVoicePoConfirmRequestDto Request { get; set; } = new();
    }
}
