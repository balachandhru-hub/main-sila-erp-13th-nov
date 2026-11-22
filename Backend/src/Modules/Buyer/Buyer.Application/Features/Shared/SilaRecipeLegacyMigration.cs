using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Recipe Studio legacy Excel migration: stage → validate → approve → post DRAFT recipes → images → publish.
    /// Additive only — does not alter ImportSilaRecipes / wizard creation paths.
    /// </summary>
    public static partial class SilaRecipeLegacyMigration
    {
        public const string FOLDER = "sila/recipe-legacy";
        public const int MAX_UPLOAD_BYTES = 10 * 1024 * 1024;
        public const int MAX_IMAGE_BYTES = SilaRecipePictureRules.MAX_PICTURE_BYTES;
        public const int MAX_COMMENT = 1000;

        public const string SEVERITY_ERROR = "ERROR";
        public const string SEVERITY_WARNING = "WARNING";
        public const string SEVERITY_INFO = "INFO";

        public const string IMAGE_MATCHED = "MATCHED";
        public const string IMAGE_UNMATCHED = "UNMATCHED";
        public const string IMAGE_ASSIGNED = "ASSIGNED";
        public const string IMAGE_FAILED = "FAILED";
        public const string IMAGE_SKIPPED = "SKIPPED";

        public const string PUBLISH_PENDING = "PENDING";
        public const string PUBLISH_PUBLISHED = "PUBLISHED";
        public const string PUBLISH_FAILED = "FAILED";
        public const string PUBLISH_SKIPPED = "SKIPPED";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

        public static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        public static async Task<RecipeLegacyBatch> GetBatchAsync(
            IRepositoryWrapper repository, ILoggerManager logger, Guid buyerId, Guid batchId, CancellationToken cancellationToken)
        {
            RecipeLegacyBatch? batch = await repository.RecipeLegacyBatch
                .FindByCondition(x => x.Id == batchId && x.BuyerId == buyerId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (batch == null)
            {
                logger.LogError($"Legacy recipe batch not found. BatchId: {batchId}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Migration batch not found.", "Select an active legacy recipe migration batch.");
            }

            return batch;
        }

        public static async Task<List<SilaRecipeLegacyBatchListItemDto>> ListBatchesAsync(
            IRepositoryWrapper repository, Guid buyerId, int index, int limit, CancellationToken cancellationToken)
        {
            int take = Math.Clamp(limit, 1, 100);
            int skip = Math.Max(0, index);
            return await repository.RecipeLegacyBatch
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Skip(skip)
                .Take(take)
                .Select(x => new SilaRecipeLegacyBatchListItemDto
                {
                    Id = x.Id,
                    BatchNumber = x.BatchNumber,
                    FileName = x.FileName,
                    Status = x.Status,
                    RecipeCount = x.RecipeCount,
                    ValidCount = x.ValidCount,
                    WarningCount = x.WarningCount,
                    ErrorCount = x.ErrorCount,
                    UnmappedCount = x.UnmappedCount,
                    DuplicateCount = x.DuplicateCount,
                    PostedCount = x.PostedCount,
                    FailedPostCount = x.FailedPostCount,
                    ImageAssignedCount = x.ImageAssignedCount,
                    PublishedCount = x.PublishedCount,
                    UploadedBy = x.UploadedBy,
                    DateCreated = x.DateCreated,
                    DateUpdated = x.DateUpdated
                })
                .ToListAsync(cancellationToken);
        }

        public static SilaRecipeLegacyBatchDetailDto ToDetail(RecipeLegacyBatch batch)
        {
            return new SilaRecipeLegacyBatchDetailDto
            {
                Id = batch.Id,
                BatchNumber = batch.BatchNumber,
                FileName = batch.FileName,
                StoredFileName = batch.StoredFileName,
                Status = batch.Status,
                RecipeCount = batch.RecipeCount,
                ValidCount = batch.ValidCount,
                WarningCount = batch.WarningCount,
                ErrorCount = batch.ErrorCount,
                UnmappedCount = batch.UnmappedCount,
                DuplicateCount = batch.DuplicateCount,
                PostedCount = batch.PostedCount,
                FailedPostCount = batch.FailedPostCount,
                ImageAssignedCount = batch.ImageAssignedCount,
                PublishedCount = batch.PublishedCount,
                UploadedBy = batch.UploadedBy,
                DateCreated = batch.DateCreated,
                DateUpdated = batch.DateUpdated,
                Revision = batch.Revision,
                ApprovalComment = batch.ApprovalComment,
                ApprovedBy = batch.ApprovedBy,
                ApprovedOn = batch.ApprovedOn,
                RejectedBy = batch.RejectedBy,
                RejectedOn = batch.RejectedOn,
                ValidationReportJson = batch.ValidationReportJson,
                ApprovalSnapshotJson = batch.ApprovalSnapshotJson,
                PostingReportJson = batch.PostingReportJson,
                ImageReportJson = batch.ImageReportJson,
                PublishReportJson = batch.PublishReportJson
            };
        }

        public static SilaRecipeLegacyRecipeListItemDto ToRecipeListItem(RecipeLegacyRecipe recipe)
        {
            return new SilaRecipeLegacyRecipeListItemDto
            {
                Id = recipe.Id,
                LegacyRecipeId = recipe.LegacyRecipeId,
                RecipeName = recipe.RecipeName,
                RecipeFormat = recipe.RecipeFormat,
                ProductClass = recipe.ProductClass,
                Category = recipe.Category,
                ValidationStatus = recipe.ValidationStatus,
                ErrorCount = recipe.ErrorCount,
                WarningCount = recipe.WarningCount,
                PostingStatus = recipe.PostingStatus,
                ImageStatus = recipe.ImageStatus,
                PublishStatus = recipe.PublishStatus,
                CreatedRecipeId = recipe.CreatedRecipeId,
                CreatedRecipeCode = recipe.CreatedRecipeCode,
                IsSelected = recipe.IsSelected,
                SourceRow = recipe.SourceRow
            };
        }

        public static string RequireBasePath(IConfiguration configuration, ILoggerManager logger)
        {
            string basePath = configuration[Common.BASE_FOLDER_PATH] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                logger.LogError("FolderPath:BasePath is not configured for recipe legacy migration.");
                throw new FailedDependencyCustomException("File storage is not configured.", "Ask your administrator to configure the file storage folder.");
            }

            return basePath;
        }

        public static string BatchDirectory(string basePath, Guid buyerId, Guid batchId)
        {
            return Path.Combine(basePath, FOLDER, buyerId.ToString("N"), batchId.ToString("N"));
        }

        public static string ItemModeFromFormat(string? recipeFormat, string? productClass)
        {
            string? format = recipeFormat?.Trim().ToUpperInvariant();
            if (format == SilaHospitalityRules.FORMAT_DIRECT)
            {
                return Common.SILA_RECIPE_DIRECT;
            }

            if (format == SilaHospitalityRules.FORMAT_BATCH || format == SilaHospitalityRules.FORMAT_SUB || format == SilaHospitalityRules.FORMAT_COMBO)
            {
                return Common.SILA_RECIPE_BATCH;
            }

            if (format == SilaHospitalityRules.FORMAT_VARIANT || format == SilaHospitalityRules.FORMAT_STANDARD)
            {
                return Common.SILA_RECIPE_RECIPE;
            }

            return SilaHospitalityRules.DefaultItemMode(productClass);
        }

        public static decimal ServingQtyOf(RecipeLegacyRecipe staged)
        {
            if (staged.PortionQty is > 0) return staged.PortionQty.Value;
            if (staged.YieldQty is > 0) return staged.YieldQty.Value;
            return 1m;
        }

        public static string ServingUomOf(RecipeLegacyRecipe staged)
        {
            if (!string.IsNullOrWhiteSpace(staged.PortionUom)) return staged.PortionUom.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(staged.YieldUom)) return staged.YieldUom.Trim().ToUpperInvariant();
            return "EA";
        }

        private static void EnsureComment(ILoggerManager logger, string? comment, bool required)
        {
            if (!required && string.IsNullOrWhiteSpace(comment)) return;
            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new BadRequestCustomException("A comment is required.", "Say why you reject or return this batch.");
            }

            if (comment.Trim().Length > MAX_COMMENT)
            {
                logger.LogError("Legacy migration comment too long.");
                throw new BadRequestCustomException("Comment is too long.", $"Keep the comment within {MAX_COMMENT} characters.");
            }
        }
    }
}
