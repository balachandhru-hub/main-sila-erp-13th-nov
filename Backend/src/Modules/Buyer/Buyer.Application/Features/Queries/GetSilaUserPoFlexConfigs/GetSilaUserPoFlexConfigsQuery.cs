using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaUserPoFlexConfigs
{
    public class GetSilaUserPoFlexConfigsQuery : IRequest<List<SilaUserPoFlexConfigDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? FilterUserId { get; set; }
        public string? ConfigType { get; set; }
        public bool AdminScope { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }
}
