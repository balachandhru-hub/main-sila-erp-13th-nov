using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SetSilaRecipePicture
{
    public class SetSilaRecipePictureCommand : IRequest<SilaRecipePictureDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid RecipeId { get; set; }
        public SilaRecipePictureUploadDto Request { get; set; } = new();
    }
}
