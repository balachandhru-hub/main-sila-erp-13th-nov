using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ReprocessSilaRecipeLegacyBatch
{
    public class ReprocessSilaRecipeLegacyBatchCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
    }
}
