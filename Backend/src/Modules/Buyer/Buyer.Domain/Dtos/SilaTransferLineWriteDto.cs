namespace Buyer.Domain.Dtos
{
    /// <summary>One material of a new transfer, in any unit the material can be converted from.</summary>
    public class SilaTransferLineWriteDto
    {
        public Guid MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public string? Uom { get; set; }

        /// <summary>Optional demand link for weekly-bucket based ITO lines.</summary>
        public Guid? WeeklyBucketId { get; set; }
        public Guid? WeeklyBucketItemId { get; set; }

        /// <summary>Optional demand link for wishlist based ITO lines.</summary>
        public Guid? WishlistItemId { get; set; }
    }
}
