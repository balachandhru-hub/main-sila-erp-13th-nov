using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyValidationReport
{
    public class GetSilaRecipeLegacyValidationReportQuery : IRequest<SilaRecipeFileDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
    }
}
