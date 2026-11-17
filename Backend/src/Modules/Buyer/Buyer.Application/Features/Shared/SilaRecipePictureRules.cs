using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Recipe pictures stored under FolderPath:BasePath/sila/recipes/{buyerId}/{recipeId}/.
    /// Active slot = selling/approved display. Draft slot = pending revision edits only.
    /// No Recipe Master schema change — association is the filesystem path under the tenant folder.
    /// </summary>
    public static class SilaRecipePictureRules
    {
        public const int MAX_PICTURE_BYTES = 5 * 1024 * 1024;
        public const string PICTURE_FOLDER = "sila/recipes";
        public const string SLOT_ACTIVE = "active";
        public const string SLOT_DRAFT = "draft";
        public const string CONTENT_TYPE_WEBP = "image/webp";

        public static string RequireBasePath(IConfiguration configuration, ILoggerManager logger)
        {
            string basePath = configuration[Common.BASE_FOLDER_PATH] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                logger.LogError("FolderPath:BasePath is not configured for recipe pictures.");
                throw new FailedDependencyCustomException("File storage is not configured.", "Ask your administrator to configure the file storage folder.");
            }

            return basePath;
        }

        public static string? DetectExtension(byte[] content)
        {
            string? kind = SilaFileRules.DetectBinaryKind(content);
            return kind == SilaFileRules.KIND_JPEG
                || kind == SilaFileRules.KIND_PNG
                || kind == SilaFileRules.KIND_WEBP
                ? SilaFileRules.ExtensionOf(kind)
                : null;
        }

        public static string ContentTypeOf(string fileName)
        {
            string? kind = SilaFileRules.KindOf(fileName) ?? SilaFileRules.DetectBinaryKind(Array.Empty<byte>());
            if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return CONTENT_TYPE_WEBP;
            if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return SilaPhotoRules.CONTENT_TYPE_PNG;
            return SilaPhotoRules.CONTENT_TYPE_JPEG;
        }

        public static string DirectoryOf(string basePath, Guid buyerId, Guid recipeId)
        {
            return Path.Combine(basePath, PICTURE_FOLDER, buyerId.ToString("N"), recipeId.ToString("N"));
        }

        /// <summary>Absolute path of an existing picture file for the slot, or null.</summary>
        public static string? FindSlotFile(string basePath, Guid buyerId, Guid recipeId, string slot)
        {
            string directory = DirectoryOf(basePath, buyerId, recipeId);
            if (!Directory.Exists(directory)) return null;
            foreach (string extension in new[] { ".jpg", ".jpeg", ".png", ".webp" })
            {
                string path = Path.Combine(directory, slot + extension);
                if (File.Exists(path)) return path;
            }

            // Legacy single-file name used before slot naming.
            if (slot == SLOT_ACTIVE)
            {
                foreach (string extension in new[] { ".jpg", ".jpeg", ".png", ".webp" })
                {
                    string path = Path.Combine(directory, "picture" + extension);
                    if (File.Exists(path)) return path;
                }
            }

            return null;
        }

        public static bool HasAnyPicture(string basePath, Guid buyerId, Guid recipeId)
        {
            return FindSlotFile(basePath, buyerId, recipeId, SLOT_ACTIVE) != null
                || FindSlotFile(basePath, buyerId, recipeId, SLOT_DRAFT) != null;
        }

        /// <summary>
        /// Catalog / selling display always prefers the active slot so a draft revision cannot replace the approved image.
        /// </summary>
        public static string? ResolveCatalogFile(string basePath, Guid buyerId, Recipe recipe)
        {
            string? active = FindSlotFile(basePath, buyerId, recipe.Id, SLOT_ACTIVE);
            if (active != null) return active;
            // Never-approved drafts only have the working (active) slot; fall through already covered.
            return null;
        }

        /// <summary>Studio / detail for the shown version: draft slot when editing a pending revision, else active.</summary>
        public static string? ResolveStudioFile(string basePath, Guid buyerId, Recipe recipe, bool preferDraft)
        {
            if (preferDraft)
            {
                string? draft = FindSlotFile(basePath, buyerId, recipe.Id, SLOT_DRAFT);
                if (draft != null) return draft;
            }

            return FindSlotFile(basePath, buyerId, recipe.Id, SLOT_ACTIVE);
        }

        public static bool PreferDraftSlot(Recipe recipe)
        {
            // Pending revision of a previously approved recipe (or rejected with active history).
            return recipe.ActiveVersion > 0
                && (recipe.Status == Common.SILA_RECIPE_DRAFT
                    || recipe.Status == Common.SILA_RECIPE_PENDING_APPROVAL
                    || recipe.Status == Common.SILA_RECIPE_REJECTED);
        }

        public static string WriteSlot(string basePath, Guid buyerId, Guid recipeId, string slot, byte[] content, string extension)
        {
            string directory = DirectoryOf(basePath, buyerId, recipeId);
            Directory.CreateDirectory(directory);
            ClearSlot(basePath, buyerId, recipeId, slot);
            string fileName = slot + extension;
            string fullPath = Path.Combine(directory, fileName);
            File.WriteAllBytes(fullPath, content);
            return fullPath;
        }

        public static void ClearSlot(string basePath, Guid buyerId, Guid recipeId, string slot)
        {
            string directory = DirectoryOf(basePath, buyerId, recipeId);
            if (!Directory.Exists(directory)) return;
            foreach (string path in Directory.EnumerateFiles(directory, slot + ".*"))
            {
                File.Delete(path);
            }

            if (slot == SLOT_ACTIVE)
            {
                foreach (string path in Directory.EnumerateFiles(directory, "picture.*"))
                {
                    File.Delete(path);
                }
            }
        }

        /// <summary>When a revision starts, copy active → draft so edits do not overwrite the selling picture.</summary>
        public static void SeedDraftFromActive(string basePath, Guid buyerId, Guid recipeId, ILoggerManager logger)
        {
            string? active = FindSlotFile(basePath, buyerId, recipeId, SLOT_ACTIVE);
            if (active == null) return;
            if (FindSlotFile(basePath, buyerId, recipeId, SLOT_DRAFT) != null) return;
            string extension = Path.GetExtension(active);
            string directory = DirectoryOf(basePath, buyerId, recipeId);
            Directory.CreateDirectory(directory);
            string draftPath = Path.Combine(directory, SLOT_DRAFT + extension);
            File.Copy(active, draftPath, overwrite: false);
            logger.LogInfo($"Seeded recipe draft picture from active. RecipeId: {recipeId}");
        }

        /// <summary>On approval, promote draft → active (approved selling image) and clear draft.</summary>
        public static void PromoteDraftToActive(string basePath, Guid buyerId, Guid recipeId, ILoggerManager logger)
        {
            string? draft = FindSlotFile(basePath, buyerId, recipeId, SLOT_DRAFT);
            if (draft == null) return;
            string extension = Path.GetExtension(draft);
            ClearSlot(basePath, buyerId, recipeId, SLOT_ACTIVE);
            string activePath = Path.Combine(DirectoryOf(basePath, buyerId, recipeId), SLOT_ACTIVE + extension);
            File.Move(draft, activePath, overwrite: true);
            ClearSlot(basePath, buyerId, recipeId, SLOT_DRAFT);
            logger.LogInfo($"Promoted recipe draft picture to active. RecipeId: {recipeId}");
        }

        public static void ClearDraft(string basePath, Guid buyerId, Guid recipeId)
        {
            ClearSlot(basePath, buyerId, recipeId, SLOT_DRAFT);
        }

        public static SilaRecipePictureDto MapInfo(bool hasPicture, string? slot)
        {
            return new SilaRecipePictureDto
            {
                HasPicture = hasPicture,
                Slot = slot
            };
        }
    }
}
