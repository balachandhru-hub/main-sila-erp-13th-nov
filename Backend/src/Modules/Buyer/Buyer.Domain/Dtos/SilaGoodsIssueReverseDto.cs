namespace Buyer.Domain.Dtos
{
    /// <summary>Reverse a posted store→outlet goods issue (local stock swap + ERP goods-movement queue).</summary>
    public class SilaGoodsIssueReverseDto
    {
        public string? Reason { get; set; }
    }
}
