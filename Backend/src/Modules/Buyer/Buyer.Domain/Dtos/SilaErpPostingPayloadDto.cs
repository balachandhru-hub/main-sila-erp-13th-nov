namespace Buyer.Domain.Dtos
{
    /// <summary>The request/response bodies stored for one ERP posting (transaction tracker).</summary>
    public class SilaErpPostingPayloadDto
    {
        public string? TechnicalDetail { get; set; }
        public string? ErpMessage { get; set; }
        public int? ErpHttpStatus { get; set; }
    }
}
