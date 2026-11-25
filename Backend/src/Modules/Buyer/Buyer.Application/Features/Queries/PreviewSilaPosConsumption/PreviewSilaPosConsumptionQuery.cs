using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.PreviewSilaPosConsumption
{
    /// <summary>Read-only recipe explosion and theoretical cost for a PREVIEW batch (optional line selection).</summary>
    public class PreviewSilaPosConsumptionQuery : IRequest<SilaPosConsumptionPreviewDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public List<Guid>? TransactionIds { get; set; }
    }
}
