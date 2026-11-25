using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SetDefaultSilaUserPurchasingProfile
{
    public class SetDefaultSilaUserPurchasingProfileCommand : IRequest<SilaUserPurchasingProfileDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid Id { get; set; }
        public bool AdminScope { get; set; }
    }
}
