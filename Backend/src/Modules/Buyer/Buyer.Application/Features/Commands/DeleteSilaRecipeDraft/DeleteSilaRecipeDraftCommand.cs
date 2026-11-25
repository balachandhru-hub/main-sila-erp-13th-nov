using MediatR;

namespace Buyer.Application.Features.Commands.DeleteSilaRecipeDraft
{
    /// <summary>Deletes a DRAFT recipe or discards a Draft revision of an approved recipe.</summary>
    public class DeleteSilaRecipeDraftCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid RecipeId { get; set; }
    }
}
