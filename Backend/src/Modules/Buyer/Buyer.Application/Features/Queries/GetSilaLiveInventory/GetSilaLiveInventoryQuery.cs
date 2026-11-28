using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaLiveInventory
{
    public class GetSilaLiveInventoryQuery : IRequest<List<SilaLiveInventoryRowDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public string? Search { get; set; }
        public Guid? LocationId { get; set; }
        /// <summary>Optional health filter when LocationId is set: LOW | OUT | EXCESS | NEGATIVE | HEALTHY | IN_STOCK.</summary>
        public string? Health { get; set; }
        /// <summary>Optional class filter: BEVERAGE | FOOD | MINIBAR | TOBACCO | GENERAL.</summary>
        public string? ProductClass { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}
