using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpsertSilaUserPurchasingProfile
{
    public class UpsertSilaUserPurchasingProfileCommand : IRequest<SilaUserPurchasingProfileDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? Id { get; set; }
        public bool AdminScope { get; set; }
        public SilaUserPurchasingProfileWriteDto Request { get; set; } = new();
    }
}
