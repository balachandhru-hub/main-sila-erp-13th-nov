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
    public static partial class SilaRecipeLegacyMigration
    {
        public static async Task<SilaRecipeLegacyActionResultDto> UploadImagesAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            Guid organizationId,
            Guid userId,
            Guid batchId,
            IReadOnlyList<(string FileName, byte[] Content)> files,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is not (Common.SILA_RECIPE_LEGACY_POSTED or Common.SILA_RECIPE_LEGACY_PARTIAL_POST
                or Common.SILA_RECIPE_LEGACY_IMAGES_ASSIGNED or Common.SILA_RECIPE_LEGACY_PUBLISHED
                or Common.SILA_RECIPE_LEGACY_PARTIAL_PUBLISH))
            {
                throw new BadRequestCustomException("Images require posted recipes.", "Post the batch before uploading recipe images.");
            }

            if (files.Count == 0)
            {
                throw new BadRequestCustomException("Images are required.", "Upload at least one JPEG, PNG or WebP image.");
            }

            string basePath = RequireBasePath(configuration, logger);
            string directory = Path.Combine(BatchDirectory(basePath, buyer.Id, batchId), "images");
            Directory.CreateDirectory(directory);

            Dictionary<string, RecipeLegacyRecipe> byCode = (await repository.RecipeLegacyRecipe
                    .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive && x.CreatedRecipeCode != null)
                    .ToListAsync(cancellationToken))
                .Where(x => !string.IsNullOrWhiteSpace(x.CreatedRecipeCode))
                .ToDictionary(x => x.CreatedRecipeCode!, StringComparer.OrdinalIgnoreCase);

            int matched = 0, unmatched = 0, failed = 0;
            List<RecipeLegacyImage> created = new List<RecipeLegacyImage>();
            foreach ((string fileName, byte[] content) in files)
            {
                try
                {
                    if (content.Length == 0 || content.Length > MAX_IMAGE_BYTES)
                    {
                        throw new BadRequestCustomException("Image size invalid.", "Each image must be between 1 byte and 5 MB.");
                    }

                    string? extension = SilaRecipePictureRules.DetectExtension(content);
                    if (extension == null)
                    {
                        throw new BadRequestCustomException("File type not accepted.", "Upload JPEG, PNG or WebP images.");
                    }

                    Guid imageId = Guid.NewGuid();
                    string stored = $"{imageId:N}{extension}";
                    await File.WriteAllBytesAsync(Path.Combine(directory, stored), content, cancellationToken);

                    string stem = Path.GetFileNameWithoutExtension(fileName ?? string.Empty).Trim();
                    RecipeLegacyRecipe? hit = null;
                    if (!string.IsNullOrWhiteSpace(stem) && byCode.TryGetValue(stem, out RecipeLegacyRecipe? byStem))
                    {
                        hit = byStem;
                    }

                    RecipeLegacyImage image = new RecipeLegacyImage
                    {
                        Id = imageId,
                        BatchId = batchId,
                        BuyerId = buyer.Id,
                        FileName = SilaFileRules.SafeDisplayName(fileName, "recipe-image",
                            extension == ".png" ? SilaFileRules.KIND_PNG : extension == ".webp" ? SilaFileRules.KIND_WEBP : SilaFileRules.KIND_JPEG),
                        StoredName = stored,
                        Status = hit != null ? IMAGE_MATCHED : IMAGE_UNMATCHED,
                        RecipeId = hit?.CreatedRecipeId,
                        RecipeCode = hit?.CreatedRecipeCode,
                        IsPrimary = true,
                        ReplaceConfirmed = false,
                        IsActive = true
                    };
                    created.Add(image);
                    if (hit != null) matched++; else unmatched++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    created.Add(new RecipeLegacyImage
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batchId,
                        BuyerId = buyer.Id,
                        FileName = fileName ?? "unknown",
                        StoredName = string.Empty,
                        Status = IMAGE_FAILED,
                        Error = ex is BaseCustomException bce ? $"{bce.Message} {bce.Description}".Trim() : ex.Message,
                        IsActive = true
                    });
                }
            }

            if (created.Count > 0)
            {
                repository.RecipeLegacyImage.CreateRange(created);
            }

            batch.ImageReportJson = Serialize(new
            {
                GeneratedOn = DateTime.UtcNow,
                Matched = matched,
                Unmatched = unmatched,
                Failed = failed,
                Images = created.Select(x => new { x.Id, x.FileName, x.Status, x.RecipeCode, x.Error })
            });
            repository.RecipeLegacyBatch.Update(batch);
            new InventoryLedger(repository, buyer.Id, userId)
                .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_IMAGE_UPLOAD,
                    $"Uploaded {files.Count} image(s): matched={matched}, unmatched={unmatched}, failed={failed}.");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = $"Uploaded images: matched={matched}, unmatched={unmatched}, failed={failed}.",
                Succeeded = matched,
                Failed = failed,
                Skipped = unmatched,
                ReportJson = batch.ImageReportJson
            };
        }

        public static async Task<SilaRecipeLegacyActionResultDto> AssignImagesAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            Guid organizationId,
            Guid userId,
            Guid batchId,
            List<SilaRecipeLegacyImageAssignDto>? assignments,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            string basePath = RequireBasePath(configuration, logger);
            string imageDir = Path.Combine(BatchDirectory(basePath, buyer.Id, batchId), "images");

            List<RecipeLegacyImage> images = await repository.RecipeLegacyImage
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<RecipeLegacyRecipe> recipes = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive && x.CreatedRecipeId != null)
                .ToListAsync(cancellationToken);

            // Auto-assign MATCHED images when no explicit body; otherwise apply body rows.
            List<(RecipeLegacyImage Image, RecipeLegacyRecipe Recipe, bool Replace, bool Primary)> work = new();
            if (assignments == null || assignments.Count == 0)
            {
                foreach (RecipeLegacyImage image in images.Where(x => x.Status == IMAGE_MATCHED && x.RecipeId != null))
                {
                    RecipeLegacyRecipe? recipe = recipes.FirstOrDefault(x => x.CreatedRecipeId == image.RecipeId);
                    if (recipe != null)
                    {
                        work.Add((image, recipe, image.ReplaceConfirmed, image.IsPrimary));
                    }
                }
            }
            else
            {
                foreach (SilaRecipeLegacyImageAssignDto assign in assignments)
                {
                    RecipeLegacyImage? image = assign.ImageId != null
                        ? images.FirstOrDefault(x => x.Id == assign.ImageId.Value)
                        : null;
                    if (image == null)
                    {
                        throw new NotFoundCustomException("Image not found.", "Select an uploaded image of this batch.");
                    }

                    RecipeLegacyRecipe? recipe = null;
                    if (assign.RecipeId != null)
                    {
                        recipe = recipes.FirstOrDefault(x => x.CreatedRecipeId == assign.RecipeId);
                    }
                    else if (!string.IsNullOrWhiteSpace(assign.RecipeCode))
                    {
                        recipe = recipes.FirstOrDefault(x => string.Equals(x.CreatedRecipeCode, assign.RecipeCode, StringComparison.OrdinalIgnoreCase));
                    }

                    if (recipe?.CreatedRecipeId == null)
                    {
                        throw new BadRequestCustomException("Recipe not posted.", "Assign images only to posted recipes with a recipe code.");
                    }

                    image.RecipeId = recipe.CreatedRecipeId;
                    image.RecipeCode = recipe.CreatedRecipeCode;
                    work.Add((image, recipe, assign.ReplaceConfirmed, assign.IsPrimary));
                }
            }

            int assigned = 0, failed = 0, skipped = 0;
            InventoryLedger ledger = new InventoryLedger(repository, buyer.Id, userId);
            foreach ((RecipeLegacyImage image, RecipeLegacyRecipe staged, bool replace, bool primary) in work)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(image.StoredName))
                    {
                        image.Status = IMAGE_SKIPPED;
                        skipped++;
                        continue;
                    }

                    string source = Path.Combine(imageDir, image.StoredName);
                    if (!File.Exists(source))
                    {
                        throw new BadRequestCustomException("Image file missing.", $"Stored file {image.StoredName} was not found.");
                    }

                    byte[] content = await File.ReadAllBytesAsync(source, cancellationToken);
                    string? extension = SilaRecipePictureRules.DetectExtension(content) ?? Path.GetExtension(image.StoredName);
                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        throw new BadRequestCustomException("Image type unknown.", "Re-upload the image as JPEG, PNG or WebP.");
                    }

                    Guid recipeId = staged.CreatedRecipeId!.Value;
                    bool hasActive = SilaRecipePictureRules.FindSlotFile(basePath, buyer.Id, recipeId, SilaRecipePictureRules.SLOT_ACTIVE) != null;
                    if (hasActive && !replace)
                    {
                        image.Status = IMAGE_SKIPPED;
                        image.Error = "Active picture exists; set ReplaceConfirmed to overwrite.";
                        skipped++;
                        continue;
                    }

                    string slot = SilaRecipePictureRules.SLOT_ACTIVE;
                    SilaRecipePictureRules.WriteSlot(basePath, buyer.Id, recipeId, slot, content, extension);
                    image.Status = IMAGE_ASSIGNED;
                    image.IsPrimary = primary;
                    image.ReplaceConfirmed = replace;
                    image.Error = null;
                    staged.ImageStatus = IMAGE_ASSIGNED;
                    repository.RecipeLegacyRecipe.Update(staged);
                    ledger.AddEvent(Common.SILA_REF_RECIPE, recipeId, SilaRecipeRules.EVENT_UPDATED,
                        $"Picture assigned from legacy migration batch {batch.BatchNumber} ({image.FileName}).");
                    assigned++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    image.Status = IMAGE_FAILED;
                    image.Error = ex is BaseCustomException bce ? $"{bce.Message} {bce.Description}".Trim() : ex.Message;
                }
            }

            if (images.Count > 0)
            {
                repository.RecipeLegacyImage.UpdateRange(images);
            }

            batch.ImageAssignedCount = images.Count(x => x.Status == IMAGE_ASSIGNED);
            if (assigned > 0)
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_IMAGES_ASSIGNED;
            }

            batch.ImageReportJson = Serialize(new
            {
                GeneratedOn = DateTime.UtcNow,
                Assigned = assigned,
                Failed = failed,
                Skipped = skipped,
                Images = images.Select(x => new { x.Id, x.FileName, x.Status, x.RecipeCode, x.Error })
            });
            repository.RecipeLegacyBatch.Update(batch);
            ledger.AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_IMAGE_ASSIGN,
                $"Assigned images: assigned={assigned}, failed={failed}, skipped={skipped}.");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = $"Assigned {assigned} image(s); {failed} failed; {skipped} skipped.",
                Succeeded = assigned,
                Failed = failed,
                Skipped = skipped,
                ReportJson = batch.ImageReportJson
            };
        }
    }
}
