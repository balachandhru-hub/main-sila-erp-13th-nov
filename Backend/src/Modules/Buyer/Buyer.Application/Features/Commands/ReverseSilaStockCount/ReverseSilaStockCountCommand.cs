using MediatR;

namespace Buyer.Application.Features.Commands.ReverseSilaStockCount
{
    public class ReverseSilaStockCountCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid StockCountId { get; set; }
        public string? Reason { get; set; }
    }
}
