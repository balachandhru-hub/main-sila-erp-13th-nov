using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.StartSilaVoicePoSession
{
    public class StartSilaVoicePoSessionCommand : IRequest<SilaVoicePoSessionDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public SilaVoicePoStartRequestDto Request { get; set; } = new();
    }
}
