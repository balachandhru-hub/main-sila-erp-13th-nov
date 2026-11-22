using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.AssignSilaRecipeLegacyImages
{
    public class AssignSilaRecipeLegacyImagesCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public List<SilaRecipeLegacyImageAssignDto>? Assignments { get; set; }
    }
}
