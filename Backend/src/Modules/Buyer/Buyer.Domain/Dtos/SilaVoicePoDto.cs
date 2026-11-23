namespace Buyer.Domain.Dtos
{
    public class SilaVoicePoSessionDto
    {
        public Guid Id { get; set; }
        public string SessionNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "OPEN";
        public Guid UserId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? PurchasingProfileId { get; set; }
        public int? PurchasingProfileVersion { get; set; }
        public string? LatestTranscript { get; set; }
        public SilaVoicePoDraftDto? Draft { get; set; }
        public List<SilaVoicePoClarificationDto> Clarifications { get; set; } = new();
        public string? SpokenReply { get; set; }
        public string? ValidationStatus { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? IdempotencyKey { get; set; }
        public Guid? CreatedPurchaseOrderId { get; set; }
        public string? CreatedPoNumber { get; set; }
        public List<SilaVoicePoCreatedOrderDto> CreatedOrders { get; set; } = new();
        public string? Language { get; set; }
        public List<SilaVoicePoTurnDto> Turns { get; set; } = new();
        public DateTime DateUpdated { get; set; }
    }

    public class SilaVoicePoTurnDto
    {
        public Guid Id { get; set; }
        public int TurnNumber { get; set; }
        public string Role { get; set; } = "USER";
        public string? Channel { get; set; }
        public string? Transcript { get; set; }
        public string? SpokenText { get; set; }
        public string? StructuredJson { get; set; }
        public string? TranscriptionStatus { get; set; }
        public decimal? Confidence { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class SilaVoicePoDraftDto
    {
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? Currency { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public List<SilaVoicePoDraftLineDto> Lines { get; set; } = new();
    }

    public class SilaVoicePoDraftLineDto
    {
        public int LineNumber { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitPrice { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierCode { get; set; }
        public string? SupplierName { get; set; }
        public string? StorageLocation { get; set; }
        public string? CostCenter { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? GlAccount { get; set; }
        public string? InventoryType { get; set; }
        public bool Resolved { get; set; }
        public string? ResolutionNote { get; set; }
    }

    public class SilaVoicePoClarificationDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
    }

    public class SilaVoicePoCreatedOrderDto
    {
        public Guid PurchaseOrderId { get; set; }
        public string? PoNumber { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
    }

    public class SilaVoicePoStartRequestDto
    {
        public Guid? LocationId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? PurchasingProfileId { get; set; }
        public string? Language { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? InitialTranscript { get; set; }
    }

    public class SilaVoicePoTurnRequestDto
    {
        public string? Transcript { get; set; }
        public string? Channel { get; set; }
        public string? Language { get; set; }
        public decimal? Confidence { get; set; }
        public string? TranscriptionStatus { get; set; }
    }

    public class SilaVoicePoConfirmRequestDto
    {
        /// <summary>Must be an explicit confirmation phrase (not yes/ok/silence).</summary>
        public string? ConfirmationPhrase { get; set; }
        public string? IdempotencyKey { get; set; }
    }
}
