using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.DecideSilaPurchaseOrder
{
    public class DecideSilaPurchaseOrderCommand : IRequest<PurchaseOrderProcessResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public SilaInvoiceDecisionDto Request { get; set; } = new();
    }
}
