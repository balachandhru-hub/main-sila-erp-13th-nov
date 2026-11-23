namespace Buyer.Domain.Dtos
{
    /// <summary>Authorized purchasing / account-assignment defaults for one user context.</summary>
    public class SilaUserPurchasingProfileDto
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public int ProfileVersion { get; set; }
        public bool IsDefault { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? LocationId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? DefaultDeliveryLocation { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ProfitCenter { get; set; }
        public string? BusinessArea { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public Guid? PreferredSupplierId { get; set; }
        public string? PreferredSupplierCode { get; set; }
        public string? DefaultCurrency { get; set; }
        public int? DefaultDeliveryLeadTimeDays { get; set; }
        public string? DefaultPoDocumentType { get; set; }
        public string? DefaultItemCategory { get; set; }
        public string? Notes { get; set; }
        public DateTime DateUpdated { get; set; }
    }

    public class SilaUserPurchasingProfileWriteDto
    {
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public bool IsDefault { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? LocationId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? DefaultDeliveryLocation { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ProfitCenter { get; set; }
        public string? BusinessArea { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public Guid? PreferredSupplierId { get; set; }
        public string? PreferredSupplierCode { get; set; }
        public string? DefaultCurrency { get; set; }
        public int? DefaultDeliveryLeadTimeDays { get; set; }
        public string? DefaultPoDocumentType { get; set; }
        public string? DefaultItemCategory { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>Resolved defaults for PO / Voice PO creation for the signed-in (or requested) user.</summary>
    public class SilaUserPurchasingDefaultsDto
    {
        public Guid? ProfileId { get; set; }
        public int? ProfileVersion { get; set; }
        public bool ProfileFound { get; set; }
        public bool IsDefault { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? LocationId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? DefaultDeliveryLocation { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ProfitCenter { get; set; }
        public string? BusinessArea { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public Guid? PreferredSupplierId { get; set; }
        public string? PreferredSupplierCode { get; set; }
        public string? DefaultCurrency { get; set; }
        public int? DefaultDeliveryLeadTimeDays { get; set; }
        public string? DefaultPoDocumentType { get; set; }
        public string? DefaultItemCategory { get; set; }
        /// <summary>STOCK | NON_STOCK | SERVICE | CONSUMABLE — drives whether CC/GL are required.</summary>
        public string? InventoryType { get; set; }
        /// <summary>True when account assignment (category + CC + GL) must be present for consumables.</summary>
        public bool RequiresAccountAssignment { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class ResolveSilaUserPurchasingDefaultsRequestDto
    {
        public Guid? UserId { get; set; }
        public Guid? ProfileId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? LocationId { get; set; }
        public string? PlantCode { get; set; }
        public string? MaterialCode { get; set; }
        public string? InventoryType { get; set; }
    }
}
