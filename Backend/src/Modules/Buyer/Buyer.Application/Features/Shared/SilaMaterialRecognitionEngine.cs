using System.Text;
using System.Text.RegularExpressions;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Shared SILA Material Recognition Engine — tenant-isolated Material Master candidate ranking.
    /// Does not invent materials, does not post inventory, and does not auto-select a material.
    /// Vision providers plug in later; catalog + OCR-hint matching is always available.
    /// </summary>
    public static class SilaMaterialRecognitionEngine
    {
        private static readonly Regex TokenSplit = new Regex(@"[^a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private const int MaxCandidates = 8;

        /// <summary>
        /// Rank Material Master candidates for a photo / OCR hint. Confidence is calibrated from token overlap
        /// (never hardcoded display percentages). Empty hint + no OCR yields a safe low-confidence fallback message.
        /// </summary>
        public static async Task<SilaStockCountPhotoIdentifyDto> IdentifyAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            string? hintText,
            byte[]? imageBytes,
            string? contentType,
            CancellationToken cancellationToken)
        {
            List<string> tokens = TokensOf(hintText);
            bool hasImage = imageBytes != null && imageBytes.Length > 0;
            if (tokens.Count == 0 && !hasImage)
            {
                return new SilaStockCountPhotoIdentifyDto
                {
                    Configured = true,
                    Message = "Capture a clearer photo or enter a product hint, then confirm a Material Master match. Inventory was not changed.",
                    Candidates = new List<SilaStockCountPhotoCandidateDto>()
                };
            }

            // Lightweight label extraction from embedded text chunks (e.g. EXIF / PNG tEXt) when present.
            if (hasImage)
            {
                tokens.AddRange(TokensOf(ExtractEmbeddedText(imageBytes!)));
            }

            tokens = tokens
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            if (tokens.Count == 0)
            {
                return new SilaStockCountPhotoIdentifyDto
                {
                    Configured = true,
                    Message = hasImage
                        ? "Image received. No readable product label found. Scan barcode or search the material manually. Inventory was not changed."
                        : "No product hint provided. Scan barcode or search the material. Inventory was not changed.",
                    Candidates = new List<SilaStockCountPhotoCandidateDto>(),
                    PreferBarcodeFallback = hasImage
                };
            }

            List<ItemBuyerMaster> materials = await repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .AsNoTracking()
                .Take(4000)
                .ToListAsync(cancellationToken);

            List<(ItemBuyerMaster Material, int Score, int Hits)> ranked = new List<(ItemBuyerMaster, int, int)>();
            foreach (ItemBuyerMaster material in materials)
            {
                string haystack = string.Join(' ',
                    material.MaterialCode ?? string.Empty,
                    material.Description ?? string.Empty,
                    material.Barcode ?? string.Empty,
                    material.MaterialGroup ?? string.Empty,
                    material.ProductType ?? string.Empty,
                    material.InventoryType ?? string.Empty).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(haystack))
                {
                    continue;
                }

                int hits = 0;
                int score = 0;
                foreach (string token in tokens)
                {
                    if (haystack.Contains(token, StringComparison.Ordinal))
                    {
                        hits++;
                        score += token.Length >= 5 ? 12 : 8;
                        if ((material.MaterialCode ?? string.Empty).Contains(token, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 10;
                        }
                        if ((material.Barcode ?? string.Empty).Equals(token, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 40;
                        }
                    }
                }

                if (hits == 0)
                {
                    continue;
                }

                ranked.Add((material, score, hits));
            }

            List<SilaStockCountPhotoCandidateDto> candidates = ranked
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Material.Description)
                .Take(MaxCandidates)
                .Select(x =>
                {
                    double confidence = ConfidenceOf(x.Hits, tokens.Count, x.Score);
                    return new SilaStockCountPhotoCandidateDto
                    {
                        MaterialId = x.Material.Id,
                        MaterialCode = x.Material.MaterialCode ?? string.Empty,
                        MaterialName = x.Material.Description ?? x.Material.MaterialCode ?? string.Empty,
                        BaseUom = x.Material.BaseUnitOfMeasure ?? string.Empty,
                        Confidence = confidence,
                        Brand = null,
                        Category = x.Material.MaterialGroup,
                        PackSize = null
                    };
                })
                .ToList();

            bool ambiguous = candidates.Count >= 2
                && candidates[0].Confidence - candidates[1].Confidence < 0.12
                && candidates[0].Confidence < 0.92;

            string message;
            if (candidates.Count == 0)
            {
                message = "No Material Master match. Scan barcode or search manually. Inventory was not changed.";
            }
            else if (ambiguous || candidates[0].Confidence < 0.55)
            {
                message = "Multiple similar materials found. Scan barcode for accurate identification. Confirm before counting — inventory was not changed.";
            }
            else if (candidates[0].Confidence >= 0.80)
            {
                message = "Recommended Material Master match. Confirm before counting — inventory was not changed.";
            }
            else
            {
                message = "Review candidates and confirm the Material Master record. Inventory was not changed.";
            }

            return new SilaStockCountPhotoIdentifyDto
            {
                Configured = true,
                Message = message,
                Candidates = candidates,
                PreferBarcodeFallback = ambiguous || (candidates.Count > 0 && candidates[0].Confidence < 0.55)
            };
        }

        private static double ConfidenceOf(int hits, int tokenCount, int score)
        {
            if (tokenCount <= 0 || hits <= 0)
            {
                return 0;
            }

            double coverage = (double)hits / tokenCount;
            double scoreFactor = Math.Min(1.0, score / 60.0);
            double value = 0.35 * coverage + 0.65 * scoreFactor;
            return Math.Round(Math.Clamp(value, 0.05, 0.98), 4);
        }

        private static List<string> TokensOf(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }

            return TokenSplit.Split(text.ToLowerInvariant())
                .Where(t => t.Length >= 2)
                .Where(t => t is not ("ml" or "l" or "kg" or "g" or "ea" or "the" or "and" or "for"))
                .ToList();
        }

        /// <summary>Best-effort extraction of printable ASCII runs from image bytes (embedded labels / tEXt).</summary>
        private static string ExtractEmbeddedText(byte[] bytes)
        {
            StringBuilder buffer = new StringBuilder();
            StringBuilder run = new StringBuilder();
            foreach (byte b in bytes)
            {
                if (b >= 32 && b < 127)
                {
                    run.Append((char)b);
                    continue;
                }

                if (run.Length >= 4)
                {
                    buffer.Append(' ').Append(run);
                }

                run.Clear();
            }

            if (run.Length >= 4)
            {
                buffer.Append(' ').Append(run);
            }

            string raw = buffer.ToString();
            return raw.Length > 4000 ? raw[..4000] : raw;
        }
    }
}
