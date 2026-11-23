using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Deterministic Voice PO agent: parse order intents, apply defaults / qty / remove-line commands,
    /// resolve materials and suppliers from ItemBuyerMaster + PurchaseOrder history + BuyerSupplierMapping,
    /// never invent codes, and require explicit confirmation phrases (reject yes/ok/silence).
    /// Optional LLM/STT/TTS endpoints are read from environment variables only — no hardcoded secrets.
    /// </summary>
    public static class SilaVoicePoAgentRules
    {
        public const string ENV_LLM_URL = "SILA_VOICE_PO_LLM_URL";
        public const string ENV_STT_URL = "SILA_VOICE_PO_STT_URL";
        public const string ENV_TTS_URL = "SILA_VOICE_PO_TTS_URL";

        public const string STATUS_OPEN = "OPEN";
        public const string STATUS_REVIEW = "REVIEW";
        public const string STATUS_CONFIRMED = "CONFIRMED";
        public const string STATUS_CREATED = "CREATED";
        public const string STATUS_CANCELLED = "CANCELLED";
        public const string STATUS_FAILED = "FAILED";

        public const string ROLE_USER = "USER";
        public const string ROLE_AGENT = "AGENT";

        public const string VALIDATION_OK = "OK";
        public const string VALIDATION_NEEDS_CLARIFICATION = "NEEDS_CLARIFICATION";
        public const string VALIDATION_INVALID = "INVALID";

        public const decimal MIN_TRANSCRIPT_CONFIDENCE = 0.55m;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private static readonly Regex OrderPattern = new(
            @"\b(?:order|buy|purchase|add|get|need|procure)\b\s+(?:(?<qty>\d+(?:\.\d+)?)\s*(?<uom>kg|kgs|g|grams?|l|liters?|litres?|pcs?|pieces?|boxes?|box|packs?|pack|units?|unit|ea|each)?\s*(?:of\s+)?)?(?<material>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex QtyMaterialPattern = new(
            @"^(?<qty>\d+(?:\.\d+)?)\s*(?<uom>kg|kgs|g|grams?|l|liters?|litres?|pcs?|pieces?|boxes?|box|packs?|pack|units?|unit|ea|each)?\s+(?:of\s+)?(?<material>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> WeakAffirmations = new(StringComparer.OrdinalIgnoreCase)
        {
            "yes", "y", "ok", "okay", "sure", "yep", "yeah", "ya", "alright", "right", "fine", "go", "do it", "proceed"
        };

        private static readonly string[] ExplicitConfirmationPhrases =
        {
            "confirm purchase order",
            "confirm the purchase order",
            "confirm and create",
            "confirm and create the order",
            "confirm and create purchase order",
            "create the purchase order",
            "place the purchase order",
            "submit the purchase order",
            "i confirm the purchase order",
            "i confirm this purchase order",
            "confirm order creation",
            "confirm po"
        };

        private static readonly string[] GoWithDefaultPhrases =
        {
            "go with default",
            "go with the default",
            "use default",
            "use the default",
            "use my default",
            "use my defaults",
            "apply default",
            "apply defaults",
            "default supplier",
            "default profile",
            "use preferred supplier"
        };

        private static readonly string[] DoubleQtyPhrases =
        {
            "double the quantity",
            "double quantity",
            "double qty",
            "double the qty",
            "twice as much",
            "make it double",
            "double it"
        };

        public static string? LlmUrl => Environment.GetEnvironmentVariable(ENV_LLM_URL);
        public static string? SttUrl => Environment.GetEnvironmentVariable(ENV_STT_URL);
        public static string? TtsUrl => Environment.GetEnvironmentVariable(ENV_TTS_URL);

        public static string NormalizeTranscript(string? transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return string.Empty;
            }

            return Regex.Replace(transcript.Trim().ToLowerInvariant(), @"\s+", " ");
        }

        public static bool IsSilence(string? transcript) => string.IsNullOrWhiteSpace(transcript);

        public static bool IsWeakAffirmation(string? transcript)
        {
            string text = NormalizeTranscript(transcript);
            return !string.IsNullOrEmpty(text) && WeakAffirmations.Contains(text);
        }

        public static bool IsExplicitConfirmation(string? transcript)
        {
            string text = NormalizeTranscript(transcript);
            if (string.IsNullOrEmpty(text) || IsWeakAffirmation(text))
            {
                return false;
            }

            return ExplicitConfirmationPhrases.Any(p => text == p || text.Contains(p, StringComparison.Ordinal));
        }

        public static bool RejectsConfirmation(string? transcript)
        {
            return IsSilence(transcript) || IsWeakAffirmation(transcript) || !IsExplicitConfirmation(transcript);
        }

        public static bool IsGoWithDefault(string? transcript)
        {
            string text = NormalizeTranscript(transcript);
            return GoWithDefaultPhrases.Any(p => text == p || text.Contains(p, StringComparison.Ordinal));
        }

        public static bool IsDoubleQty(string? transcript)
        {
            string text = NormalizeTranscript(transcript);
            return DoubleQtyPhrases.Any(p => text == p || text.Contains(p, StringComparison.Ordinal));
        }

        public static bool IsRemoveLine(string? transcript, out string? materialHint)
        {
            materialHint = null;
            string text = NormalizeTranscript(transcript);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            Match match = Regex.Match(
                text,
                @"\b(?:remove|delete|drop|cancel)\b(?:\s+(?:the\s+)?(?:line|item))?\s*(?:for\s+)?(?<material>.+)?$",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            string? raw = match.Groups["material"].Success ? match.Groups["material"].Value.Trim() : null;
            if (string.IsNullOrWhiteSpace(raw) || raw is "line" or "item" or "it" or "that" or "this" or "last")
            {
                materialHint = null;
                return true;
            }

            materialHint = raw;
            return true;
        }

        public static bool IsUncertainTranscript(string? transcript, decimal? confidence, string? transcriptionStatus = null)
        {
            if (!string.IsNullOrWhiteSpace(transcriptionStatus)
                && transcriptionStatus.Trim().Equals("FAILED", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsSilence(transcript))
            {
                return true;
            }

            if (confidence != null && confidence < MIN_TRANSCRIPT_CONFIDENCE)
            {
                return true;
            }

            string text = NormalizeTranscript(transcript);
            return text.Contains('[') || text.Contains("inaudible") || text.Contains("unclear") || text == "?";
        }

        public static VoicePoParseResult ParseOrderIntent(string? transcript)
        {
            VoicePoParseResult result = new();
            if (IsSilence(transcript))
            {
                result.Intent = VoicePoIntent.None;
                result.NeedsClarification = true;
                result.ClarificationMessage = "I did not catch that. Please say the material and quantity to order.";
                return result;
            }

            string text = transcript!.Trim();
            string normalized = NormalizeTranscript(text);

            if (IsExplicitConfirmation(normalized))
            {
                result.Intent = VoicePoIntent.Confirm;
                return result;
            }

            if (IsGoWithDefault(normalized))
            {
                result.Intent = VoicePoIntent.GoWithDefault;
                return result;
            }

            if (IsDoubleQty(normalized))
            {
                result.Intent = VoicePoIntent.DoubleQuantity;
                return result;
            }

            if (IsRemoveLine(normalized, out string? hint))
            {
                result.Intent = VoicePoIntent.RemoveLine;
                result.MaterialHint = hint;
                return result;
            }

            if (normalized is "review" or "show draft" or "show the draft" or "read back" or "read the order")
            {
                result.Intent = VoicePoIntent.Review;
                return result;
            }

            if (normalized is "cancel" or "cancel session" or "cancel the order" or "stop")
            {
                result.Intent = VoicePoIntent.Cancel;
                return result;
            }

            Match order = OrderPattern.Match(text);
            if (!order.Success)
            {
                order = QtyMaterialPattern.Match(text);
            }

            if (order.Success)
            {
                result.Intent = VoicePoIntent.AddLine;
                if (order.Groups["qty"].Success
                    && decimal.TryParse(order.Groups["qty"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal qty)
                    && qty > 0)
                {
                    result.Quantity = qty;
                }
                else
                {
                    result.Quantity = 1;
                }

                if (order.Groups["uom"].Success && !string.IsNullOrWhiteSpace(order.Groups["uom"].Value))
                {
                    result.UnitOfMeasure = NormalizeUom(order.Groups["uom"].Value);
                }

                result.MaterialHint = CleanMaterialHint(order.Groups["material"].Value);
                if (string.IsNullOrWhiteSpace(result.MaterialHint))
                {
                    result.NeedsClarification = true;
                    result.ClarificationMessage = "Which material should I add? Please include the material name or code.";
                }

                return result;
            }

            result.Intent = VoicePoIntent.Unknown;
            result.NeedsClarification = true;
            result.ClarificationMessage =
                "I can add a line (for example 'order 10 kg chicken'), use your default, double quantity, remove a line, or confirm the purchase order.";
            result.MaterialHint = CleanMaterialHint(text);
            return result;
        }

        public static SilaVoicePoDraftValidation ValidateDraft(SilaVoicePoDraftDto? draft)
        {
            SilaVoicePoDraftValidation validation = new();
            if (draft == null || draft.Lines == null || draft.Lines.Count == 0)
            {
                validation.Status = VALIDATION_INVALID;
                validation.Errors.Add("The draft has no lines.");
                return validation;
            }

            for (int i = 0; i < draft.Lines.Count; i++)
            {
                SilaVoicePoDraftLineDto line = draft.Lines[i];
                string where = $"Line {(line.LineNumber > 0 ? line.LineNumber : i + 1)}";
                if (!line.Resolved || string.IsNullOrWhiteSpace(line.MaterialCode))
                {
                    validation.Errors.Add($"{where}: material is not resolved. Never invent a material code.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "MATERIAL_UNRESOLVED",
                        Message = $"{where}: say the material code or a unique description from the item master.",
                        Field = "MaterialCode"
                    });
                }

                if (line.Quantity <= 0)
                {
                    validation.Errors.Add($"{where}: quantity must be greater than zero.");
                }

                if (line.SupplierId == null || line.SupplierId == Guid.Empty)
                {
                    validation.Errors.Add($"{where}: supplier is not resolved.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "SUPPLIER_UNRESOLVED",
                        Message = $"{where}: choose a supplier from history or your preferred default.",
                        Field = "SupplierId"
                    });
                }

                if (SilaUserPurchasingProfileRules.RequiresAccountAssignment(line.InventoryType))
                {
                    if (string.IsNullOrWhiteSpace(line.AccountAssignmentCategory)
                        || string.IsNullOrWhiteSpace(line.CostCenter)
                        || string.IsNullOrWhiteSpace(line.GlAccount))
                    {
                        validation.Errors.Add($"{where}: consumable items need account assignment category, cost center and GL account.");
                        validation.Clarifications.Add(new SilaVoicePoClarificationDto
                        {
                            Code = "ACCOUNT_ASSIGNMENT",
                            Message = $"{where}: say 'go with default' to apply your purchasing profile account assignment.",
                            Field = "CostCenter"
                        });
                    }
                }
            }

            if (validation.Errors.Count == 0)
            {
                validation.Status = VALIDATION_OK;
            }
            else if (validation.Clarifications.Count > 0)
            {
                validation.Status = VALIDATION_NEEDS_CLARIFICATION;
            }
            else
            {
                validation.Status = VALIDATION_INVALID;
            }

            return validation;
        }

        public static void ApplyGoWithDefault(SilaVoicePoDraftDto draft, SilaUserPurchasingDefaultsDto defaults)
        {
            draft.CompanyCode ??= defaults.CompanyCode;
            draft.PlantCode ??= defaults.PlantCode;
            draft.StorageLocation ??= defaults.StorageLocation;
            draft.PurchasingOrganization ??= defaults.PurchasingOrganization;
            draft.PurchasingGroup ??= defaults.PurchasingGroup;
            draft.Currency ??= defaults.DefaultCurrency;
            if (defaults.DefaultDeliveryLeadTimeDays != null)
            {
                draft.DeliveryDate ??= DateTime.UtcNow.Date.AddDays(defaults.DefaultDeliveryLeadTimeDays.Value);
            }

            foreach (SilaVoicePoDraftLineDto line in draft.Lines)
            {
                if (line.SupplierId == null && defaults.PreferredSupplierId != null)
                {
                    line.SupplierId = defaults.PreferredSupplierId;
                    line.SupplierCode = defaults.PreferredSupplierCode;
                    line.ResolutionNote = AppendNote(line.ResolutionNote, "Preferred supplier from profile.");
                }

                line.StorageLocation ??= defaults.StorageLocation;
                if (SilaUserPurchasingProfileRules.RequiresAccountAssignment(line.InventoryType ?? defaults.InventoryType))
                {
                    line.AccountAssignmentCategory ??= defaults.AccountAssignmentCategory;
                    line.CostCenter ??= defaults.CostCenter;
                    line.GlAccount ??= defaults.GlAccount;
                    line.InventoryType ??= defaults.InventoryType;
                }
                else
                {
                    // Stock: do not force CC/GL from profile.
                    line.AccountAssignmentCategory = null;
                    line.CostCenter = null;
                    line.GlAccount = null;
                }
            }

            if (defaults.RequiresAccountAssignment)
            {
                draft.AccountAssignmentCategory ??= defaults.AccountAssignmentCategory;
                draft.CostCenter ??= defaults.CostCenter;
                draft.GlAccount ??= defaults.GlAccount;
            }
            else
            {
                draft.AccountAssignmentCategory = null;
                draft.CostCenter = null;
                draft.GlAccount = null;
            }
        }

        public static void DoubleQuantities(SilaVoicePoDraftDto draft)
        {
            foreach (SilaVoicePoDraftLineDto line in draft.Lines)
            {
                line.Quantity *= 2;
            }
        }

        public static bool RemoveLine(SilaVoicePoDraftDto draft, string? materialHint)
        {
            if (draft.Lines.Count == 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(materialHint))
            {
                draft.Lines.RemoveAt(draft.Lines.Count - 1);
                Renumber(draft);
                return true;
            }

            string hint = materialHint.Trim();
            int index = draft.Lines.FindIndex(l =>
                (!string.IsNullOrWhiteSpace(l.MaterialCode) && l.MaterialCode.Contains(hint, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(l.Description) && l.Description.Contains(hint, StringComparison.OrdinalIgnoreCase)));
            if (index < 0)
            {
                return false;
            }

            draft.Lines.RemoveAt(index);
            Renumber(draft);
            return true;
        }

        public static void Renumber(SilaVoicePoDraftDto draft)
        {
            for (int i = 0; i < draft.Lines.Count; i++)
            {
                draft.Lines[i].LineNumber = i + 1;
            }
        }

        public static async Task<MaterialResolution> ResolveMaterialAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            string? hint,
            CancellationToken cancellationToken)
        {
            MaterialResolution resolution = new();
            if (string.IsNullOrWhiteSpace(hint))
            {
                resolution.Note = "No material hint provided.";
                return resolution;
            }

            string text = hint.Trim();
            string upper = text.ToUpperInvariant();

            // Exact code match only — never invent a code.
            ItemBuyerMaster? byCode = await repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.MaterialCode == upper);
            if (byCode != null)
            {
                resolution.Material = byCode;
                resolution.Resolved = true;
                resolution.Note = "Matched material code.";
                return resolution;
            }

            List<ItemBuyerMaster> byDescription = await repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.Description != null && x.Description.ToLower().Contains(text.ToLower()))
                .OrderBy(x => x.Description)
                .Take(5)
                .ToListAsync(cancellationToken);

            if (byDescription.Count == 1)
            {
                resolution.Material = byDescription[0];
                resolution.Resolved = true;
                resolution.Note = "Matched unique material description.";
                return resolution;
            }

            if (byDescription.Count > 1)
            {
                resolution.Candidates = byDescription;
                resolution.Note = "Multiple materials match. Please say the material code.";
                return resolution;
            }

            resolution.Note = "No material found in the item master. Codes are never invented.";
            return resolution;
        }

        public static async Task<SupplierResolution> ResolveSupplierAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid? preferredSupplierId,
            string? preferredSupplierCode,
            string? materialCode,
            CancellationToken cancellationToken)
        {
            SupplierResolution resolution = new();

            if (preferredSupplierId != null && preferredSupplierId != Guid.Empty)
            {
                BuyerSupplierMapping? mapped = await repository.BuyerSupplierMapping
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.SupplierId == preferredSupplierId.Value);
                if (mapped != null)
                {
                    SilaSupplier? sila = await repository.SilaSupplier
                        .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.SupplierOrganizationId == preferredSupplierId);
                    resolution.SupplierId = preferredSupplierId;
                    resolution.SupplierCode = preferredSupplierCode
                        ?? sila?.SupplierCode;
                    resolution.SupplierName = sila?.Name;
                    resolution.Resolved = true;
                    resolution.Note = "Preferred supplier from purchasing profile.";
                    return resolution;
                }
            }

            if (!string.IsNullOrWhiteSpace(preferredSupplierCode))
            {
                string code = preferredSupplierCode.Trim().ToUpperInvariant();
                SilaSupplier? byCode = await repository.SilaSupplier
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.SupplierCode == code);
                if (byCode != null)
                {
                    resolution.SupplierId = byCode.SupplierOrganizationId ?? byCode.Id;
                    resolution.SupplierCode = byCode.SupplierCode;
                    resolution.SupplierName = byCode.Name;
                    resolution.Resolved = true;
                    resolution.Note = "Preferred supplier code from purchasing profile.";
                    return resolution;
                }
            }

            if (!string.IsNullOrWhiteSpace(materialCode))
            {
                string code = materialCode.Trim().ToUpperInvariant();
                var history = await (
                    from item in repository.PurchaseOrderItem.FindByCondition(x => x.IsActive && x.MaterialCode == code)
                    join po in repository.PurchaseOrder.FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                        on item.PurchaseOrderId equals po.Id
                    orderby po.DateCreated descending
                    select new { po.SupplierId, po.SupplierName })
                    .Take(10)
                    .ToListAsync(cancellationToken);

                Guid? supplierId = history.Select(h => (Guid?)h.SupplierId).FirstOrDefault(id => id != null && id != Guid.Empty);
                if (supplierId != null)
                {
                    BuyerSupplierMapping? stillMapped = await repository.BuyerSupplierMapping
                        .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.SupplierId == supplierId.Value);
                    if (stillMapped != null)
                    {
                        resolution.SupplierId = supplierId;
                        resolution.SupplierName = history.FirstOrDefault(h => h.SupplierId == supplierId)?.SupplierName;
                        resolution.Resolved = true;
                        resolution.Note = "Supplier from purchase order history for this material.";
                        return resolution;
                    }
                }
            }

            List<BuyerSupplierMapping> mappings = await repository.BuyerSupplierMapping
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (mappings.Count == 1)
            {
                resolution.SupplierId = mappings[0].SupplierId;
                resolution.Resolved = true;
                resolution.Note = "Only mapped supplier for this buyer.";
                return resolution;
            }

            resolution.Note = "Supplier could not be resolved. Codes and suppliers are never invented.";
            return resolution;
        }

        public static string BuildSpokenReply(VoicePoIntent intent, SilaVoicePoDraftDto draft, SilaVoicePoDraftValidation validation, string? extra = null)
        {
            if (!string.IsNullOrWhiteSpace(extra))
            {
                return extra.Trim();
            }

            return intent switch
            {
                VoicePoIntent.AddLine when validation.Status == VALIDATION_OK =>
                    $"Added. Draft has {draft.Lines.Count} line{(draft.Lines.Count == 1 ? string.Empty : "s")}. Say 'confirm purchase order' when ready.",
                VoicePoIntent.GoWithDefault => "Applied your purchasing defaults.",
                VoicePoIntent.DoubleQuantity => "Doubled the quantities on the draft.",
                VoicePoIntent.RemoveLine => draft.Lines.Count == 0 ? "Removed the line. The draft is empty." : "Removed the line.",
                VoicePoIntent.Review => SummarizeDraft(draft),
                VoicePoIntent.Confirm => "Confirmation received.",
                VoicePoIntent.Cancel => "Session cancelled.",
                _ => validation.Clarifications.FirstOrDefault()?.Message
                    ?? validation.Errors.FirstOrDefault()
                    ?? "How can I help with this purchase order?"
            };
        }

        public static string SummarizeDraft(SilaVoicePoDraftDto draft)
        {
            if (draft.Lines.Count == 0)
            {
                return "The draft is empty.";
            }

            IEnumerable<string> parts = draft.Lines.Select(l =>
                $"{l.Quantity:0.####} {l.UnitOfMeasure ?? ""} {l.Description ?? l.MaterialCode}".Trim());
            return $"Draft: {string.Join("; ", parts)}. Say 'confirm purchase order' to create it.";
        }

        public static string SerializeDraft(SilaVoicePoDraftDto? draft) =>
            draft == null ? "null" : JsonSerializer.Serialize(draft, JsonOptions);

        public static SilaVoicePoDraftDto? DeserializeDraft(string? json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                return new SilaVoicePoDraftDto();
            }

            try
            {
                return JsonSerializer.Deserialize<SilaVoicePoDraftDto>(json, JsonOptions) ?? new SilaVoicePoDraftDto();
            }
            catch (JsonException)
            {
                return new SilaVoicePoDraftDto();
            }
        }

        public static string SerializeClarifications(List<SilaVoicePoClarificationDto> items) =>
            JsonSerializer.Serialize(items, JsonOptions);

        public static List<SilaVoicePoClarificationDto> DeserializeClarifications(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SilaVoicePoClarificationDto>>(json, JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        public static string SerializeErrors(List<string> errors) =>
            JsonSerializer.Serialize(errors, JsonOptions);

        public static List<string> DeserializeErrors(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }

        private static string? CleanMaterialHint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string cleaned = Regex.Replace(value.Trim(), @"[^\w\s\-/&.]", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        private static string NormalizeUom(string uom)
        {
            string value = uom.Trim().ToUpperInvariant();
            return value switch
            {
                "KGS" or "KG" => "KG",
                "GRAMS" or "GRAM" or "G" => "G",
                "LITERS" or "LITRES" or "LITER" or "LITRE" or "L" => "L",
                "PCS" or "PC" or "PIECES" or "PIECE" or "EA" or "EACH" or "UNITS" or "UNIT" => "EA",
                "BOXES" or "BOX" => "BOX",
                "PACKS" or "PACK" => "PACK",
                _ => value
            };
        }

        private static string AppendNote(string? existing, string note)
        {
            return string.IsNullOrWhiteSpace(existing) ? note : $"{existing} {note}";
        }
    }

    public enum VoicePoIntent
    {
        None,
        Unknown,
        AddLine,
        GoWithDefault,
        DoubleQuantity,
        RemoveLine,
        Review,
        Confirm,
        Cancel
    }

    public class VoicePoParseResult
    {
        public VoicePoIntent Intent { get; set; } = VoicePoIntent.None;
        public decimal? Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? MaterialHint { get; set; }
        public bool NeedsClarification { get; set; }
        public string? ClarificationMessage { get; set; }
    }

    public class SilaVoicePoDraftValidation
    {
        public string Status { get; set; } = SilaVoicePoAgentRules.VALIDATION_INVALID;
        public List<string> Errors { get; set; } = new();
        public List<SilaVoicePoClarificationDto> Clarifications { get; set; } = new();
    }

    public class MaterialResolution
    {
        public bool Resolved { get; set; }
        public ItemBuyerMaster? Material { get; set; }
        public List<ItemBuyerMaster> Candidates { get; set; } = new();
        public string? Note { get; set; }
    }

    public class SupplierResolution
    {
        public bool Resolved { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierCode { get; set; }
        public string? SupplierName { get; set; }
        public string? Note { get; set; }
    }
}
