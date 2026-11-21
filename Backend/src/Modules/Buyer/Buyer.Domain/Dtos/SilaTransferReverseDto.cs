namespace Buyer.Domain.Dtos
{
    public class SilaTransferReverseLineDto
    {
        public Guid ItemId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class SilaTransferReverseDto
    {
        public string Reason { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime? PostingDate { get; set; }
        public List<SilaTransferReverseLineDto> Items { get; set; } = new();
    }

    public class SilaTransferReverseResultDto
    {
        public Guid TransferId { get; set; }
        public string ItoNumber { get; set; } = string.Empty;
        public string? ReversalStatus { get; set; }
        public Guid? PostingId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
