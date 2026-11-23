using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ProcessSilaVoicePoTurn
{
    public class ProcessSilaVoicePoTurnCommand : IRequest<SilaVoicePoSessionDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SessionId { get; set; }
        public SilaVoicePoTurnRequestDto Request { get; set; } = new();
    }
}
