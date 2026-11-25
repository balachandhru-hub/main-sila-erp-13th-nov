using MediatR;

namespace Buyer.Application.Features.Commands.DeleteSilaGoodsIssueCostCenter
{
    public class DeleteSilaGoodsIssueCostCenterCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid Id { get; set; }
    }
}
