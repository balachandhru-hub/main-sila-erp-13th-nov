using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaCulinaryAiStatus
{
    public class GetSilaCulinaryAiStatusQuery : IRequest<SilaCulinaryAiStatusDto>
    {
        public Guid OrganizationId { get; set; }
    }
}
