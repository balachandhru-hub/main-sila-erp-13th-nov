using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ValidateSilaRecipeLegacyBatch
{
    public class ValidateSilaRecipeLegacyBatchCommand : IRequest<SilaRecipeLegacyBatchDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
    }
}
