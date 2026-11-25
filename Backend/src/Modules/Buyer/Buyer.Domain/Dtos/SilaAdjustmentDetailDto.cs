namespace Buyer.Domain.Dtos
{
    public class SilaAdjustmentDetailDto
    {
        public Guid Id { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public Guid LocationId { get; set; }
        public string? LocationName { get; set; }
        public string AdjustmentType { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public Guid PostedBy { get; set; }
        public string? PostedByName { get; set; }
        public DateTime PostedOn { get; set; }
        public List<SilaAdjustmentItemDto> Items { get; set; } = new();
        /// <summary>Workflow / ERP tracker history for mobile and cloud detail.</summary>
        public List<SilaTransferEventDto> Events { get; set; } = new();
        public string? ErpStatus { get; set; }
        public string? ErpReference { get; set; }
        public Guid? ErpPostingId { get; set; }
    }
}
