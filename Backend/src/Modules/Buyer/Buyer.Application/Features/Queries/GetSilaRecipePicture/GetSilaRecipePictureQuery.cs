using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipePicture
{
    public class GetSilaRecipePictureQuery : IRequest<SilaRecipePictureFileDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid RecipeId { get; set; }
        /// <summary>When true, prefer the draft revision picture for editors. Catalog uses false.</summary>
        public bool PreferDraft { get; set; }
    }
}
