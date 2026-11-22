namespace Buyer.Domain.Dtos
{
    public class SilaRecipeLegacyBatchListItemDto
    {
        public Guid Id { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RecipeCount { get; set; }
        public int ValidCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }
        public int UnmappedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int PostedCount { get; set; }
        public int FailedPostCount { get; set; }
        public int ImageAssignedCount { get; set; }
        public int PublishedCount { get; set; }
        public Guid UploadedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }

    public class SilaRecipeLegacyBatchDetailDto : SilaRecipeLegacyBatchListItemDto
    {
        public string StoredFileName { get; set; } = string.Empty;
        public int Revision { get; set; }
        public string? ApprovalComment { get; set; }
        public Guid? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public Guid? RejectedBy { get; set; }
        public DateTime? RejectedOn { get; set; }
        public string? ValidationReportJson { get; set; }
        public string? ApprovalSnapshotJson { get; set; }
        public string? PostingReportJson { get; set; }
        public string? ImageReportJson { get; set; }
        public string? PublishReportJson { get; set; }
    }

    public class SilaRecipeLegacyRecipeListItemDto
    {
        public Guid Id { get; set; }
        public string LegacyRecipeId { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string? RecipeFormat { get; set; }
        public string? ProductClass { get; set; }
        public string? Category { get; set; }
        public string ValidationStatus { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public string PostingStatus { get; set; } = string.Empty;
        public string? ImageStatus { get; set; }
        public string? PublishStatus { get; set; }
        public Guid? CreatedRecipeId { get; set; }
        public string? CreatedRecipeCode { get; set; }
        public bool IsSelected { get; set; }
        public int SourceRow { get; set; }
    }

    public class SilaRecipeLegacyRecipePageDto
    {
        public List<SilaRecipeLegacyRecipeListItemDto> Items { get; set; } = new();
        public int Total { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }

    public class SilaRecipeLegacyRecipeDetailDto : SilaRecipeLegacyRecipeListItemDto
    {
        public Guid BatchId { get; set; }
        public string? Description { get; set; }
        public string? Subcategory { get; set; }
        public decimal? YieldQty { get; set; }
        public string? YieldUom { get; set; }
        public decimal? PortionQty { get; set; }
        public string? PortionUom { get; set; }
        public int? PrepMinutes { get; set; }
        public int? CookMinutes { get; set; }
        public string? ServingInstructions { get; set; }
        public string? OutletScope { get; set; }
        public string? ActiveStatus { get; set; }
        public string? LegacyReference { get; set; }
        public string? Notes { get; set; }
        public string? IngredientsJson { get; set; }
        public string? InstructionsJson { get; set; }
        public string? SubRecipesJson { get; set; }
        public string? VariantsJson { get; set; }
        public string? FindingsJson { get; set; }
        public string? MaterialMappingsJson { get; set; }
        public int? CreatedVersion { get; set; }
        public string? PostingError { get; set; }
        public List<SilaRecipeLegacyMaterialMapDto> MaterialMaps { get; set; } = new();
    }

    public class SilaRecipeLegacyMaterialMapDto
    {
        public Guid Id { get; set; }
        public string LegacyRecipeId { get; set; } = string.Empty;
        public int IngredientLine { get; set; }
        public string? LegacyMaterialNumber { get; set; }
        public string? MaterialNumber { get; set; }
        public string? MaterialDescription { get; set; }
        public Guid? MappedMaterialId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SilaRecipeLegacyMaterialMapWriteDto
    {
        public Guid? Id { get; set; }
        public string LegacyRecipeId { get; set; } = string.Empty;
        public int IngredientLine { get; set; }
        public string? MaterialNumber { get; set; }
        public Guid? MappedMaterialId { get; set; }
        /// <summary>MAPPED | UNMAPPED | IGNORED</summary>
        public string Status { get; set; } = "MAPPED";
    }

    public class SilaRecipeLegacyDecideDto
    {
        /// <summary>approve | reject | return</summary>
        public string Decision { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class SilaRecipeLegacyImageDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? RecipeCode { get; set; }
        public Guid? RecipeId { get; set; }
        public string? Error { get; set; }
        public bool IsPrimary { get; set; }
        public bool ReplaceConfirmed { get; set; }
    }

    public class SilaRecipeLegacyImageAssignDto
    {
        public Guid? ImageId { get; set; }
        public Guid? RecipeId { get; set; }
        public string? RecipeCode { get; set; }
        public bool ReplaceConfirmed { get; set; }
        public bool IsPrimary { get; set; } = true;
    }

    public class SilaRecipeLegacyFindingDto
    {
        public string Severity { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Field { get; set; }
        public int? Row { get; set; }
        public string? Sheet { get; set; }
    }

    public class SilaRecipeLegacyIngredientDto
    {
        public int Line { get; set; }
        public string? LegacyMaterialNumber { get; set; }
        public string? MaterialNumber { get; set; }
        public string? MaterialDescription { get; set; }
        public decimal Quantity { get; set; }
        public string? Uom { get; set; }
        public string? Notes { get; set; }
    }

    public class SilaRecipeLegacySubRecipeDto
    {
        public string SubLegacyRecipeId { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Uom { get; set; }
    }

    public class SilaRecipeLegacyVariantDto
    {
        public string? VariantLegacyRecipeId { get; set; }
        public string? VariantName { get; set; }
        public string? Notes { get; set; }
    }

    public class SilaRecipeLegacyActionResultDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public string? ReportJson { get; set; }
    }
}
