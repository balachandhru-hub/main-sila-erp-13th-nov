using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatches
{
    public class GetSilaRecipeLegacyBatchesQuery : IRequest<List<SilaRecipeLegacyBatchListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; } = 20;
    }
}
