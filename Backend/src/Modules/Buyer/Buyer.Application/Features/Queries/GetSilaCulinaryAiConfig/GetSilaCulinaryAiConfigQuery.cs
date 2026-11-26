using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaCulinaryAiConfig
{
    public class GetSilaCulinaryAiConfigQuery : IRequest<SilaCulinaryAiConfigDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid RoleId { get; set; }
    }
}
