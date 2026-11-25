using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.IdentifySilaStockCountPhoto
{
    public class IdentifySilaStockCountPhotoQuery : IRequest<SilaStockCountPhotoIdentifyDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid StockCountId { get; set; }
        /// <summary>Optional product / OCR hint from the client.</summary>
        public string? HintText { get; set; }
        public byte[]? ImageBytes { get; set; }
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
    }
}
