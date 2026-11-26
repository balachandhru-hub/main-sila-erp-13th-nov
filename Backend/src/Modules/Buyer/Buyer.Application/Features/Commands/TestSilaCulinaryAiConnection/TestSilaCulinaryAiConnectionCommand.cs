using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.TestSilaCulinaryAiConnection
{
    public class TestSilaCulinaryAiConnectionCommand : IRequest<SilaCulinaryAiConnectionTestResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public SilaCulinaryAiConnectionTestRequestDto Request { get; set; } = new();
    }
}
