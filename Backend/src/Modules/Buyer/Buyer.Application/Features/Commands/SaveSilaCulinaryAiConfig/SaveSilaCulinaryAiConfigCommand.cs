using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SaveSilaCulinaryAiConfig
{
    public class SaveSilaCulinaryAiConfigCommand : IRequest<SilaCulinaryAiConfigDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public SilaCulinaryAiConfigWriteDto Request { get; set; } = new();
    }
}
