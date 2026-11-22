using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyTemplate
{
    public class GetSilaRecipeLegacyTemplateQueryHandler : IRequestHandler<GetSilaRecipeLegacyTemplateQuery, SilaRecipeFileDto>
    {
        public Task<SilaRecipeFileDto> Handle(GetSilaRecipeLegacyTemplateQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(SilaRecipeLegacyMigration.BuildTemplate());
        }
    }
}
