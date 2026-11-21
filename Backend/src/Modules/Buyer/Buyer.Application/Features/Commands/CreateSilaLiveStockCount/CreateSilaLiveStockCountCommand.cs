using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateSilaLiveStockCount
{
    public class CreateSilaLiveStockCountCommand : IRequest<SilaLiveStockCountResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public SilaLiveStockCountWriteDto Request { get; set; } = new();
    }
}
