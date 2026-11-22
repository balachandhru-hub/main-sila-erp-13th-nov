using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatch
{
    public class GetSilaRecipeLegacyBatchQuery : IRequest<SilaRecipeLegacyBatchDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
    }
}
