using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.DecideSilaRecipeLegacyBatch
{
    public class DecideSilaRecipeLegacyBatchCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public SilaRecipeLegacyDecideDto Request { get; set; } = new();
    }
}
