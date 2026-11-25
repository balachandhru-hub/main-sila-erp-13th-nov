using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpsertSilaUserPoFlexConfig
{
    public class UpsertSilaUserPoFlexConfigCommand : IRequest<SilaUserPoFlexConfigDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? Id { get; set; }
        public bool AdminScope { get; set; }
        public SilaUserPoFlexConfigWriteDto Request { get; set; } = new();
    }
}
