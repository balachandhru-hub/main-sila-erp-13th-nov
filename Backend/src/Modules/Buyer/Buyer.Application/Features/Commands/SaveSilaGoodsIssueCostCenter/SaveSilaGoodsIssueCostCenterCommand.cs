using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SaveSilaGoodsIssueCostCenter
{
    public class SaveSilaGoodsIssueCostCenterCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? Id { get; set; }
        public SilaGoodsIssueCostCenterWriteDto Request { get; set; } = new();
    }
}
