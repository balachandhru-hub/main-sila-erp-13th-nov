using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaMaterialDetail
{
    public class GetSilaMaterialDetailQuery : IRequest<SilaMaterialDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid MaterialId { get; set; }
    }
}
