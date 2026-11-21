namespace Buyer.Domain.Dtos
{
    public class SilaPendingOutletRequirementDto
    {
        public Guid RequirementId { get; set; }
        public string RequirementSource { get; set; } = string.Empty;
        public string? SourceReference { get; set; }
        public Guid? WeeklyBucketId { get; set; }
        public Guid? WeeklyBucketItemId { get; set; }
        public Guid? WishlistId { get; set; }
        public Guid? WishlistItemId { get; set; }
        public DateTime? RequestedDate { get; set; }
        public Guid? DestinationOutletId { get; set; }
        public string? DestinationOutletName { get; set; }
        public Guid? DestinationLocationId { get; set; }
        public string? DestinationLocationCode { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal OutstandingQuantity { get; set; }
        public decimal AllocatedToActiveItos { get; set; }
        public decimal AvailableForNewIto { get; set; }
        public string Uom { get; set; } = string.Empty;
        public decimal AvailableSourceStock { get; set; }
        public string RequirementStatus { get; set; } = string.Empty;
    }

    public class SilaPendingOutletRequirementsDto
    {
        public int Count { get; set; }
        public List<SilaPendingOutletRequirementDto> Items { get; set; } = new();
    }
}
