using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.PostSilaRecipeLegacyBatch
{
    public class PostSilaRecipeLegacyBatchCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public bool FailedOnly { get; set; }
    }
}
