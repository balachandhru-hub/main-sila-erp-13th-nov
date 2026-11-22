using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyTemplate
{
    public class GetSilaRecipeLegacyTemplateQuery : IRequest<SilaRecipeFileDto>
    {
    }
}
