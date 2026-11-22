using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UploadSilaRecipeLegacyImages
{
    public class UploadSilaRecipeLegacyImagesCommand : IRequest<SilaRecipeLegacyActionResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public List<(string FileName, byte[] Content)> Files { get; set; } = new();
    }
}
