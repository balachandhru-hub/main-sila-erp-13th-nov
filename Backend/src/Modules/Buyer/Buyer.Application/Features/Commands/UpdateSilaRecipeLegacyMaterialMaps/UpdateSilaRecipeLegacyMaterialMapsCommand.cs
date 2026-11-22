using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateSilaRecipeLegacyMaterialMaps
{
    public class UpdateSilaRecipeLegacyMaterialMapsCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public List<SilaRecipeLegacyMaterialMapWriteDto> Mappings { get; set; } = new();
    }
}
