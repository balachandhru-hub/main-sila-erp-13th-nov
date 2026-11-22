using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipes
{
    public class GetSilaRecipeLegacyRecipesQuery : IRequest<SilaRecipeLegacyRecipePageDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; } = 50;
    }
}
