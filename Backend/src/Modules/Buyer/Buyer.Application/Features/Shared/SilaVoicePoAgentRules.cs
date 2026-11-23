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

        // Longer UOM tokens first so "10kgs" / Chrome STT "10 kilograms" → KG + material "onion".
        private const string UomToken =
            @"kilograms?|kilos?|kilo|kgs|kg|grammes?|grams?|g|litres?|liters?|liter|litre|l|pieces?|pcs?|pc|boxes?|box|packs?|pack|units?|unit|each|ea";

        private static readonly Regex OrderPattern = new(
            $@"\b(?:order|buy|purchase|add|get|need|procure)\b\s+(?:(?<qty>\d+(?:\.\d+)?)\s*(?<uom>{UomToken})?\.?\s*(?:of\s+)?)?(?<material>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex QtyMaterialPattern = new(
            $@"^(?<qty>\d+(?:\.\d+)?)\s*(?<uom>{UomToken})?\.?\s+(?:of\s+)?(?<material>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // When STT splits oddly ("10 of kilograms onion") recover UOM from the material text.
        private static readonly Regex LeadingUomInMaterial = new(
            $@"^(?<uom>{UomToken})\.?\s+(?:of\s+)?(?<material>.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Explicit "for the price N" / currency / at/@ / per — never treat bare quantity as price.
        private static readonly Regex PricePattern = new(
            $@"(?:\bfor\s+(?:the\s+)?price\b|\bat\s+(?:the\s+)?price\b|\bpriced?\s+(?:at|is)?\b)\s*(?<price4>\d+(?:\.\d+)?)(?:\s*(?<currency4>aed|usd|eur|gbp|sar))?(?:\s*(?:per|/)\s*(?<priceuom4>{UomToken}))?|(?:(?:\bat\b|@|\bfor\b|\bprice(?:d| is)?\b)\s*)?(?<price>\d+(?:\.\d+)?)\s*(?<currency>aed|usd|eur|gbp|sar)(?:\s*(?:per|/)\s*(?<priceuom>{UomToken}))?|(?:(?:\bat\b|@|\bprice(?:d| is)?\b)\s*)(?<price2>\d+(?:\.\d+)?)(?:\s*(?:per|/)\s*(?<priceuom2>{UomToken}))?|(?<price3>\d+(?:\.\d+)?)\s*(?:per|/)\s*(?<priceuom3>{UomToken})",
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

            // Bare amount answers first ("20", "20.", "20 AED") so they never become a new line.
            if (TryParseUnitPriceAnswer(normalized, text, out decimal? answeredPrice, out string? answeredCurrency))
            {
                result.Intent = VoicePoIntent.SetUnitPrice;
                result.UnitPrice = answeredPrice;
                result.Currency = answeredCurrency ?? "AED";
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
                // Chrome often says "kilograms" / "kilos" — recover UOM if the first pattern missed it.
                if (string.IsNullOrWhiteSpace(result.UnitOfMeasure) && !string.IsNullOrWhiteSpace(result.MaterialHint))
                {
                    Match leadingUom = LeadingUomInMaterial.Match(result.MaterialHint);
                    if (leadingUom.Success)
                    {
                        result.UnitOfMeasure = NormalizeUom(leadingUom.Groups["uom"].Value);
                        result.MaterialHint = CleanMaterialHint(leadingUom.Groups["material"].Value);
                    }
                }

                ApplyPriceHint(text, result);
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

        /// <param name="forConfirm">
        /// Confirm still requires unit price (and UOM). Price is never invented here —
        /// the user must provide it or say "use one AED" first.
        /// </param>
        public static SilaVoicePoDraftValidation ValidateDraft(SilaVoicePoDraftDto? draft, bool forConfirm = false)
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
                // Text PO lines are allowed: no material code, but description + material group from defaults.
                bool isTextLine = string.IsNullOrWhiteSpace(line.MaterialCode)
                    && !string.IsNullOrWhiteSpace(line.Description)
                    && string.Equals(line.InventoryType, SilaUserPurchasingProfileRules.INVENTORY_NON_STOCK, StringComparison.OrdinalIgnoreCase);
                if (!line.Resolved && !isTextLine)
                {
                    validation.Errors.Add($"{where}: material is not resolved. Never invent a material code.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "MATERIAL_UNRESOLVED",
                        Message = $"{where}: say the material code, a unique description from the item master, or continue as a text PO with defaults.",
                        Field = "MaterialCode"
                    });
                }

                if (isTextLine && string.IsNullOrWhiteSpace(line.MaterialGroup))
                {
                    validation.Errors.Add($"{where}: text PO lines need a material group from your PO defaults.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "MATERIAL_GROUP",
                        Message = $"{where}: set MaterialGroup on your PO defaults, then say 'go with default'.",
                        Field = "MaterialGroup"
                    });
                }

                if (line.Quantity <= 0)
                {
                    validation.Errors.Add($"{where}: quantity must be greater than zero.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "QUANTITY",
                        Message = $"{where}: how many units do you need?",
                        Field = "Quantity"
                    });
                }

                if (string.IsNullOrWhiteSpace(line.UnitOfMeasure))
                {
                    validation.Errors.Add($"{where}: unit of measure is required.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "UOM",
                        Message = $"{where}: what unit of measure should I use (for example KG, EA, L)?",
                        Field = "UnitOfMeasure"
                    });
                }

                // Always require a positive unit price before create — never invent it.
                if (line.UnitPrice == null || line.UnitPrice <= 0)
                {
                    string cur = draft.Currency ?? "AED";
                    validation.Errors.Add($"{where}: unit price is required.");
                    validation.Clarifications.Add(new SilaVoicePoClarificationDto
                    {
                        Code = "UNIT_PRICE",
                        Message = forConfirm
                            ? $"{where}: unit price is required before submit. Say the amount (for example 20) — {cur} is added automatically — or say 'use one {cur}' to default to 1."
                            : $"{where}: what is the unit price in {cur}? Say the amount (for example 20) — {cur} is added automatically — or say 'use one {cur}' to default to 1.",
                        Field = "UnitPrice"
                    });
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

                bool needsAccountAssignment = SilaUserPurchasingProfileRules.RequiresAccountAssignment(line.InventoryType)
                    || !string.IsNullOrWhiteSpace(line.AccountAssignmentCategory);
                if (needsAccountAssignment)
                {
                    if (string.IsNullOrWhiteSpace(line.AccountAssignmentCategory)
                        || string.IsNullOrWhiteSpace(line.CostCenter)
                        || string.IsNullOrWhiteSpace(line.GlAccount))
                    {
                        validation.Errors.Add($"{where}: account assignment category, cost center and GL account are required.");
                        validation.Clarifications.Add(new SilaVoicePoClarificationDto
                        {
                            Code = "ACCOUNT_ASSIGNMENT",
                            Message = $"{where}: say 'go with default' to apply your purchasing profile account assignment.",
                            Field = "CostCenter"
                        });
                    }
                }
            }

            // FIVE Create PO header fields (same set used for successful SAP PO 4500005033).
            void RequireHeader(string? value, string field, string ask)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                validation.Errors.Add($"{field} is required.");
                validation.Clarifications.Add(new SilaVoicePoClarificationDto
                {
                    Code = field.ToUpperInvariant(),
                    Message = ask,
                    Field = field
                });
            }

            RequireHeader(draft.CompanyCode, "CompanyCode", "Company code is missing from your PO defaults. Say 'go with default' after updating defaults.");
            RequireHeader(draft.PlantCode, "PlantCode", "Plant is missing from your PO defaults. Say 'go with default' after updating defaults.");
            RequireHeader(draft.PurchasingOrganization, "PurchasingOrganization", "Purchasing organization is missing from your PO defaults.");
            RequireHeader(draft.PurchasingGroup, "PurchasingGroup", "Purchasing group is missing from your PO defaults.");
            if (string.IsNullOrWhiteSpace(draft.Currency))
            {
                draft.Currency = "AED";
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

        /// <summary>Applies 1 AED (or draft currency) when a line still has no positive unit price.</summary>
        public static void ApplyDefaultUnitPrices(SilaVoicePoDraftDto draft)
        {
            string currency = string.IsNullOrWhiteSpace(draft.Currency) ? "AED" : draft.Currency.Trim().ToUpperInvariant();
            draft.Currency = currency;
            foreach (SilaVoicePoDraftLineDto line in draft.Lines)
            {
                if (line.UnitPrice == null || line.UnitPrice <= 0)
                {
                    line.UnitPrice = 1m;
                    line.ResolutionNote = AppendNote(line.ResolutionNote, $"Default unit price 1 {currency}.");
                }
            }
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
                line.MaterialGroup ??= defaults.MaterialGroup;
                if (defaults.RequiresAccountAssignment
                    || SilaUserPurchasingProfileRules.RequiresAccountAssignment(line.InventoryType ?? defaults.InventoryType))
                {
                    line.AccountAssignmentCategory ??= defaults.AccountAssignmentCategory;
                    line.CostCenter ??= defaults.CostCenter;
                    line.GlAccount ??= defaults.GlAccount;
                    line.InventoryType ??= defaults.InventoryType;
                }
                else
                {
                    // Stock without a profile AA category: do not force CC/GL.
                    line.AccountAssignmentCategory = null;
                    line.CostCenter = null;
                    line.GlAccount = null;
                }
            }

            draft.MaterialGroup ??= defaults.MaterialGroup;
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
                string code = preferredSupplierCode.Trim();
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

                // SAP vendor code from defaults is enough for text / ERP POs even without a local mapping row.
                resolution.SupplierId = preferredSupplierId is { } id && id != Guid.Empty ? id : Guid.NewGuid();
                resolution.SupplierCode = code;
                resolution.SupplierName = code;
                resolution.Resolved = true;
                resolution.Note = "Preferred supplier code from PO defaults.";
                return resolution;
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
            // Missing UOM / price / other mandatory fields always win over "added" success copy.
            string? ask = PreferMandatoryAsk(validation) ?? PreferMandatoryAskFromDraft(draft);
            if (!string.IsNullOrWhiteSpace(ask))
            {
                return ask;
            }

            if (!string.IsNullOrWhiteSpace(extra) && validation.Status == VALIDATION_OK)
            {
                return extra.Trim();
            }

            return intent switch
            {
                VoicePoIntent.AddLine when validation.Status == VALIDATION_OK =>
                    $"Added. Draft has {draft.Lines.Count} line{(draft.Lines.Count == 1 ? string.Empty : "s")}. Say 'confirm purchase order' when ready.",
                VoicePoIntent.GoWithDefault when validation.Status == VALIDATION_OK => "Applied your purchasing defaults.",
                VoicePoIntent.GoWithDefault => "Applied your purchasing defaults. Some fields still need a value.",
                VoicePoIntent.SetUnitPrice when validation.Status == VALIDATION_OK =>
                    $"Got it. Unit price set in {draft.Currency ?? "AED"}. {SummarizeDraft(draft)}",
                VoicePoIntent.SetUnitPrice => "Unit price updated.",
                VoicePoIntent.DoubleQuantity => "Doubled the quantities on the draft.",
                VoicePoIntent.RemoveLine => draft.Lines.Count == 0 ? "Removed the line. The draft is empty." : "Removed the line.",
                VoicePoIntent.Review => SummarizeDraft(draft),
                VoicePoIntent.Confirm => "Confirmation received.",
                VoicePoIntent.Cancel => "Session cancelled.",
                _ => validation.Errors.FirstOrDefault()
                    ?? "How can I help with this purchase order?"
            };
        }

        /// <summary>Prefer unit price when UOM was already spoken; then UOM / account assignment.</summary>
        public static string? PreferMandatoryAsk(SilaVoicePoDraftValidation validation)
        {
            if (validation.Clarifications.Count == 0)
            {
                return null;
            }

            // Price first: "order 10kgs of onion" should ask for price, not UOM, once KG is parsed.
            string[] priority = { "UNIT_PRICE", "UOM", "QUANTITY", "ACCOUNT_ASSIGNMENT", "SUPPLIER_UNRESOLVED", "MATERIAL_GROUP", "MATERIAL_UNRESOLVED" };
            foreach (string code in priority)
            {
                SilaVoicePoClarificationDto? hit = validation.Clarifications
                    .FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
                if (hit != null && !string.IsNullOrWhiteSpace(hit.Message))
                {
                    return hit.Message;
                }
            }

            return validation.Clarifications.FirstOrDefault()?.Message;
        }

        /// <summary>Hard fallback when validation status was wrongly OK but price/UOM are still blank.</summary>
        private static string? PreferMandatoryAskFromDraft(SilaVoicePoDraftDto draft)
        {
            for (int i = 0; i < draft.Lines.Count; i++)
            {
                SilaVoicePoDraftLineDto line = draft.Lines[i];
                string where = $"Line {(line.LineNumber > 0 ? line.LineNumber : i + 1)}";
                if (line.UnitPrice == null || line.UnitPrice <= 0)
                {
                    string cur = draft.Currency ?? "AED";
                    return $"{where}: what is the unit price in {cur}? Say the amount (for example 20) — {cur} is added automatically — or say 'use one {cur}' to default to 1.";
                }

                if (string.IsNullOrWhiteSpace(line.UnitOfMeasure))
                {
                    return $"{where}: what unit of measure should I use (for example KG, EA, L)?";
                }
            }

            return null;
        }

        private static bool TryParseUnitPriceAnswer(string normalized, string original, out decimal? unitPrice)
            => TryParseUnitPriceAnswer(normalized, original, out unitPrice, out _);

        private static bool TryParseUnitPriceAnswer(
            string normalized,
            string original,
            out decimal? unitPrice,
            out string? currency)
        {
            unitPrice = null;
            currency = null;
            string cleaned = Regex.Replace((original ?? string.Empty).Trim(), @"[.,;:!]+$", string.Empty).Trim();
            string cleanedNorm = NormalizeTranscript(cleaned);

            if (Regex.IsMatch(cleanedNorm, @"\b(use|default|go with)\b.*\b(one|1)\b.*\b(aed|price|dirham)?\b")
                || cleanedNorm is "use one aed" or "use 1 aed" or "default price" or "one aed" or "use one")
            {
                unitPrice = 1m;
                currency = "AED";
                return true;
            }

            if (TrySpokenAmountWord(cleanedNorm, out decimal spokenAmount))
            {
                unitPrice = spokenAmount;
                currency = "AED";
                return true;
            }

            // Whole utterance only — never match "at 25" inside "order 10 kg X at 25 AED".
            Match priced = Regex.Match(
                cleaned,
                @"^(?:(?:unit\s+)?price(?:\s+is)?|at|for(?:\s+the)?\s+price)\s+(?<amount>\d+(?:[.,]\d+)?)\s*(?<currency>aed|usd|eur|gbp|sar|dirhams?|dirham)?\s*$|^(?<amount2>\d+(?:[.,]\d+)?)\s*(?<currency2>aed|usd|eur|gbp|sar|dirhams?|dirham)?\s*$",
                RegexOptions.IgnoreCase);

            string? rawAmount = priced.Success
                ? (priced.Groups["amount"].Success && !string.IsNullOrWhiteSpace(priced.Groups["amount"].Value)
                    ? priced.Groups["amount"].Value
                    : priced.Groups["amount2"].Value)
                : null;
            if (priced.Success
                && !string.IsNullOrWhiteSpace(rawAmount)
                && decimal.TryParse(
                    rawAmount.Replace(',', '.'),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal amount)
                && amount > 0)
            {
                unitPrice = amount;
                string? rawCurrency = priced.Groups["currency"].Success && !string.IsNullOrWhiteSpace(priced.Groups["currency"].Value)
                    ? priced.Groups["currency"].Value
                    : priced.Groups["currency2"].Success && !string.IsNullOrWhiteSpace(priced.Groups["currency2"].Value)
                        ? priced.Groups["currency2"].Value
                        : null;
                if (!string.IsNullOrWhiteSpace(rawCurrency))
                {
                    string raw = rawCurrency.Trim().ToUpperInvariant();
                    currency = raw.StartsWith("DIRHAM", StringComparison.Ordinal) ? "AED" : raw;
                }
                else
                {
                    currency = "AED";
                }

                return true;
            }

            return false;
        }

        private static bool TrySpokenAmountWord(string normalized, out decimal amount)
        {
            amount = 0;
            // Common Chrome STT words for a short price reply.
            Dictionary<string, decimal> words = new(StringComparer.OrdinalIgnoreCase)
            {
                ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
                ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
                ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14, ["fifteen"] = 15,
                ["sixteen"] = 16, ["seventeen"] = 17, ["eighteen"] = 18, ["nineteen"] = 19, ["twenty"] = 20,
                ["thirty"] = 30, ["forty"] = 40, ["fifty"] = 50, ["sixty"] = 60, ["seventy"] = 70,
                ["eighty"] = 80, ["ninety"] = 90, ["hundred"] = 100
            };
            if (words.TryGetValue(normalized, out amount))
            {
                return true;
            }

            Match compound = Regex.Match(normalized, @"^(?<tens>twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety)[-\s]+(?<ones>one|two|three|four|five|six|seven|eight|nine)$", RegexOptions.IgnoreCase);
            if (compound.Success
                && words.TryGetValue(compound.Groups["tens"].Value, out decimal tens)
                && words.TryGetValue(compound.Groups["ones"].Value, out decimal ones))
            {
                amount = tens + ones;
                return true;
            }

            return false;
        }

        public static string SummarizeDraft(SilaVoicePoDraftDto draft)
        {
            if (draft.Lines.Count == 0)
            {
                return "The draft is empty.";
            }

            IEnumerable<string> parts = draft.Lines.Select(l =>
            {
                string priceBit = l.UnitPrice is > 0 ? $" @ {l.UnitPrice:0.####}" : " (price pending)";
                return $"{l.Quantity:0.####} {l.UnitOfMeasure ?? "?"} {l.Description ?? l.MaterialCode}{priceBit}".Trim();
            });
            SilaVoicePoDraftValidation check = ValidateDraft(draft);
            if (check.Status != VALIDATION_OK)
            {
                string? ask = PreferMandatoryAsk(check);
                return $"Draft: {string.Join("; ", parts)}.{(string.IsNullOrWhiteSpace(ask) ? string.Empty : " " + ask)}";
            }

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

        private static void ApplyPriceHint(string transcript, VoicePoParseResult result)
        {
            Match price = PricePattern.Match(transcript);
            if (!price.Success)
            {
                return;
            }

            string? raw = FirstNonEmptyGroup(price, "price4", "price", "price2", "price3");
            if (string.IsNullOrWhiteSpace(raw)
                || !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount)
                || amount <= 0)
            {
                return;
            }

            result.UnitPrice = amount;
            string? currency = FirstNonEmptyGroup(price, "currency4", "currency");
            if (!string.IsNullOrWhiteSpace(currency))
            {
                result.Currency = currency.Trim().ToUpperInvariant();
            }
            else if (string.IsNullOrWhiteSpace(result.Currency))
            {
                result.Currency = "AED";
            }

            string? priceUom = FirstNonEmptyGroup(price, "priceuom4", "priceuom", "priceuom2", "priceuom3");
            if (string.IsNullOrWhiteSpace(result.UnitOfMeasure) && !string.IsNullOrWhiteSpace(priceUom))
            {
                result.UnitOfMeasure = NormalizeUom(priceUom);
            }

            if (!string.IsNullOrWhiteSpace(result.MaterialHint))
            {
                result.MaterialHint = CleanMaterialHint(result.MaterialHint);
            }
        }

        private static string? FirstNonEmptyGroup(Match match, params string[] names)
        {
            foreach (string name in names)
            {
                if (match.Groups[name].Success && !string.IsNullOrWhiteSpace(match.Groups[name].Value))
                {
                    return match.Groups[name].Value;
                }
            }

            return null;
        }

        private static string? CleanMaterialHint(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string cleaned = PricePattern.Replace(value.Trim(), " ");
            // Strip leftover price cues ("onion for the", "onion at the", …).
            cleaned = Regex.Replace(
                cleaned,
                @"\b(?:for(?:\s+the)?|at(?:\s+the)?|priced?(?:\s+(?:at|is))?)\s*$",
                " ",
                RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"[^\w\s\-/&.]", " ");
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        private static string NormalizeUom(string uom)
        {
            string value = uom.Trim().TrimEnd('.').ToUpperInvariant();
            return value switch
            {
                "KGS" or "KG" or "KILOGRAM" or "KILOGRAMS" or "KILO" or "KILOS" => "KG",
                "GRAMS" or "GRAM" or "GRAMME" or "GRAMMES" or "G" => "G",
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
        SetUnitPrice = 100,
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
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
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
