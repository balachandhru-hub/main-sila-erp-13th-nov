using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.RemoveSilaRecipePicture
{
    public class RemoveSilaRecipePictureCommand : IRequest<SilaRecipePictureDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid RecipeId { get; set; }
    }
}
