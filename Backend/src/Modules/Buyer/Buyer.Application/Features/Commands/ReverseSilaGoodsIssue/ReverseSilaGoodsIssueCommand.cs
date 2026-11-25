using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ReverseSilaGoodsIssue
{
    public class ReverseSilaGoodsIssueCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid GoodsIssueId { get; set; }
        public SilaGoodsIssueReverseDto Request { get; set; } = new();
    }
}
