using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaGoodsIssueCostCenters
{
    public class GetSilaGoodsIssueCostCentersQuery : IRequest<List<SilaGoodsIssueCostCenterDto>>
    {
        public Guid OrganizationId { get; set; }
        public string? Scope { get; set; }
    }
}
