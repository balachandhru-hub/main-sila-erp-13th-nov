using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.PublishSilaRecipeLegacyBatch
{
    public class PublishSilaRecipeLegacyBatchCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        /// <summary>True when the caller has APPROVE_SILA_RECIPE (controller attribute).</summary>
        public bool CanApprove { get; set; }
    }
}
