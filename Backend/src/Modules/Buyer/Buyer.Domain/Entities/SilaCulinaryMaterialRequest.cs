using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialRequest : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string RequestReference { get; set; } = string.Empty;

        /// <summary>NEW_MATERIAL, MATERIAL_EXTENSION, MATERIAL_REACTIVATION, UOM_MAINTENANCE, SPECIFICATION_CORRECTION, CLASSIFICATION_MAINTENANCE, MANUAL_REVIEW.</summary>
        [Required]
        [MaxLength(50)]
        public string RequestType { get; set; } = "NEW_MATERIAL";

        /// <summary>DRAFT, VALIDATION_REQUIRED, READY_FOR_SUBMISSION, SUBMITTED, PENDING_REVIEW, PENDING_APPROVAL, RETURNED_FOR_CORRECTION, APPROVED, REJECTED, CREATION_PENDING, CREATION_IN_PROGRESS, CREATION_FAILED, ERP_OUTCOME_UNKNOWN, MATERIAL_CREATED, SYNCHRONIZATION_PENDING, MATERIAL_AVAILABLE, MAPPING_REVIEW_REQUIRED, RESOLVED, CANCELLED.</summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "DRAFT";

        public int RevisionNumber { get; set; } = 1;

        public int? ApprovedRevisionNumber { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProposedDescription { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? LongDescription { get; set; }

        [MaxLength(80)]
        public string? IngredientCategory { get; set; }

        [MaxLength(60)]
        public string? MaterialType { get; set; }

        [MaxLength(80)]
        public string? MaterialGroup { get; set; }

        [MaxLength(120)]
        public string? ProductHierarchy { get; set; }

        [MaxLength(120)]
        public string? Brand { get; set; }

        [MaxLength(160)]
        public string? Manufacturer { get; set; }

        [MaxLength(1000)]
        public string? ProductSpecification { get; set; }

        [MaxLength(80)]
        public string? Barcode { get; set; }

        [MaxLength(80)]
        public string? CountryOfOrigin { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [MaxLength(40)]
        public string? CompanyCode { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        [MaxLength(40)]
        public string? StorageLocation { get; set; }

        [MaxLength(40)]
        public string? PurchasingOrganization { get; set; }

        [MaxLength(40)]
        public string? PurchasingGroup { get; set; }

        [Required]
        [MaxLength(30)]
        public string BaseUom { get; set; } = "KG";

        [MaxLength(30)]
        public string? PurchasingUom { get; set; }

        [MaxLength(30)]
        public string? AlternateUom { get; set; }

        [Precision(18, 4)]
        public decimal? PackSize { get; set; }

        [Precision(18, 4)]
        public decimal? ConversionNumerator { get; set; }

        [Precision(18, 4)]
        public decimal? ConversionDenominator { get; set; }

        [Precision(18, 4)]
        public decimal? NetWeight { get; set; }

        [Precision(18, 4)]
        public decimal? GrossWeight { get; set; }

        [Precision(18, 4)]
        public decimal? Volume { get; set; }

        [MaxLength(80)]
        public string? IngredientForm { get; set; }

        [MaxLength(200)]
        public string? StorageRequirements { get; set; }

        public int? ShelfLifeDays { get; set; }

        public bool BatchManaged { get; set; }

        [MaxLength(500)]
        public string? PotentialAllergens { get; set; }

        [MaxLength(200)]
        public string? DietaryClassification { get; set; }

        [MaxLength(500)]
        public string? QualitySpecifications { get; set; }

        [MaxLength(500)]
        public string? FoodSafetyRequirements { get; set; }

        [Precision(18, 4)]
        public decimal? RequiredQuantity { get; set; }

        public DateTime? RequiredDateUtc { get; set; }

        public Guid? RequestedByUserId { get; set; }

        [MaxLength(200)]
        public string? RequestedByName { get; set; }

        [MaxLength(1000)]
        public string? BusinessJustification { get; set; }

        [MaxLength(200)]
        public string? SuggestedSupplier { get; set; }

        /// <summary>Existing target material when request is Extension, Reactivation, UOM, or Spec correction.</summary>
        public Guid? TargetMaterialId { get; set; }

        [MaxLength(80)]
        public string? TargetMaterialCode { get; set; }

        /// <summary>AI_SUGGESTED, CHEF_ENTERED, EXISTING_MASTER_DATA, MASTER_DATA_VERIFIED, APPROVED.</summary>
        [MaxLength(40)]
        public string DataOrigin { get; set; } = "CHEF_ENTERED";

        public string? DuplicateCheckJson { get; set; }

        public string? AttachmentsJson { get; set; }

        public string? ApprovalTrailJson { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [MaxLength(120)]
        public string? CorrelationId { get; set; }

        [MaxLength(80)]
        public string? ExecutionReference { get; set; }

        /// <summary>NOT_STARTED, PENDING_ERP_MANUAL, IN_PROGRESS, SUCCEEDED, FAILED, ERP_OUTCOME_UNKNOWN.</summary>
        [MaxLength(40)]
        public string ExecutionStatus { get; set; } = "NOT_STARTED";

        public Guid? ResultingMaterialId { get; set; }

        [MaxLength(80)]
        public string? ResultingMaterialCode { get; set; }

        [MaxLength(1000)]
        public string? LastError { get; set; }

        [MaxLength(500)]
        public string? DecisionComment { get; set; }

        public DateTime? SubmittedOnUtc { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        public DateTime? MaterialAvailableOnUtc { get; set; }
    }
}
