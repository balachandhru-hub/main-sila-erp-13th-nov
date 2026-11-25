using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyPostingReport
{
    public class GetSilaRecipeLegacyPostingReportQuery : IRequest<SilaRecipeFileDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
    }
}
