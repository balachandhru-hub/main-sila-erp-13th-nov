using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipe
{
    public class GetSilaRecipeLegacyRecipeQuery : IRequest<SilaRecipeLegacyRecipeDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public Guid RecipeId { get; set; }
    }
}
