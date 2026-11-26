using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryRecipeService
    {
        Task<CulinaryPlannerContextDto> GetContextAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? locationId, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> GenerateAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            CulinaryGenerateRecipeRequestDto request, CancellationToken cancellationToken);

        Task<CulinaryRefineResponseDto> RefineAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? draftId,
            CulinaryRefineRequestDto request, CancellationToken cancellationToken);

        Task<CulinaryRefineResponseDto> RegenerateSectionAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? draftId,
            CulinaryRegenerateSectionRequestDto request, CancellationToken cancellationToken);

        Task<CulinaryScaleV2ResultDto> ScaleV2Async(
            Guid organizationId, Guid userId, Guid? draftId,
            CulinaryScaleV2RequestDto request, CancellationToken cancellationToken);

        Task<CulinaryDocumentExtractDto> ExtractDocumentAsync(
            Guid organizationId, Guid userId, IFormFile file, CancellationToken cancellationToken);

        Task<CulinaryDishImagePreparedDto> PrepareDishImageAsync(
            Guid organizationId, Guid userId, IFormFile file, CancellationToken cancellationToken);

        Task CancelGenerationAsync(
            Guid organizationId, Guid userId, string generationReference, CancellationToken cancellationToken);

        Task<List<CulinaryRecipeDraftSummaryDto>> ListDraftsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? status, string? search, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> GetDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> SaveDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            CulinarySaveDraftRequestDto request, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> DuplicateDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> RestoreRevisionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, int revisionNumber, CancellationToken cancellationToken);

        Task<CulinaryRecipeDraftDetailDto> CancelDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, CancellationToken cancellationToken);

        Task<(byte[] Content, string ContentType, string FileName)> ExportDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string format, CancellationToken cancellationToken);
    }

    public class SilaCulinaryRecipeService : ISilaCulinaryRecipeService
    {
        private static readonly JsonSerializerOptions SnakeJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions CamelJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private static readonly Regex SecretPattern = new(
            @"(AIza[0-9A-Za-z\-_]{12,}|sk-ant-[0-9A-Za-z\-_]{10,}|Bearer\s+[0-9A-Za-z\-_\.]{12,})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IRepositoryWrapper _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIntegrationCredentialProtector _protector;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SilaCulinaryRecipeService(
            IRepositoryWrapper repository,
            IHttpClientFactory httpClientFactory,
            IIntegrationCredentialProtector protector,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _protector = protector;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CulinaryPlannerContextDto> GetContextAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? locationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration? cfgEntity = await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IsActive);
            SilaCulinaryAiConfiguration cfg = cfgEntity ?? SilaCulinaryAiSettings.CreateDefault(buyer.Id);

            List<Guid> allowedLocationIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, cancellationToken);
            List<InventoryLocation> locations = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && allowedLocationIds.Contains(x.Id))
                .OrderBy(x => x.LocationName)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, string> outlets = await _repository.BuyerOutlet
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToDictionaryAsync(x => x.Id, x => x.OutletName, cancellationToken);

            List<Recipe> existingRecipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status == Common.SILA_RECIPE_APPROVED)
                .OrderBy(x => x.Name)
                .Take(50)
                .ToListAsync(cancellationToken);

            List<CulinaryAvailableStockItemDto> availableStock = new();
            if (locationId.HasValue)
            {
                await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, userId, roleId, locationId.Value, cancellationToken);
                availableStock = await LoadBoundedStockAsync(buyer.Id, locationId.Value, 40, cancellationToken);
            }

            return new CulinaryPlannerContextDto
            {
                CanSelectProvider = CanUserSelectProvider(roleId),
                OperatingMode = cfg.OperatingMode,
                EnabledProviders = GetEnabledProviders(cfg),
                AllowExternalAi = cfg.AllowExternalAi,
                PromptVersion = SilaCulinaryAiSettings.PROMPT_VERSION_V1,
                Currency = string.IsNullOrWhiteSpace(buyer.Currency) ? "AED" : buyer.Currency,
                Locations = locations.Select(l => new CulinaryLocationContextDto
                {
                    LocationId = l.Id,
                    PropertyId = l.PropertyId,
                    LocationCode = l.LocationCode,
                    LocationName = l.LocationName,
                    LocationType = l.LocationType,
                    OutletId = l.OutletId,
                    OutletName = l.OutletId.HasValue && outlets.TryGetValue(l.OutletId.Value, out string? oName) ? oName : null
                }).ToList(),
                ExistingRecipes = existingRecipes.Select(r => new CulinaryExistingRecipeRefDto
                {
                    RecipeId = r.Id,
                    RecipeCode = r.RecipeCode,
                    Name = r.Name,
                    Category = r.Category,
                    ServingQty = r.ServingQty,
                    ServingUom = r.ServingUom,
                    Status = r.Status,
                    Version = r.Version
                }).ToList(),
                AvailableStock = availableStock
            };
        }

        public async Task<CulinaryRecipeDraftDetailDto> GenerateAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            CulinaryGenerateRecipeRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration cfg = await GetEffectiveConfigAsync(buyer.Id);
            EnsurePhase1AEnabled(cfg);
            ValidateGenerationRequest(request);
            await EnforceTenantBudgetsAsync(buyer.Id, cfg, cancellationToken);

            SilaCulinaryRecipeDraft? existingDraft = null;
            if (request.DraftId.HasValue)
            {
                existingDraft = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, request.DraftId.Value, cancellationToken);
            }

            InventoryLocation? selectedLocation = null;
            string? propertyName = null;
            string? outletName = null;
            if (request.LocationId.HasValue)
            {
                await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, userId, roleId, request.LocationId.Value, cancellationToken);
                selectedLocation = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, request.LocationId.Value);
                BuyerProperty? prop = await _repository.BuyerProperty.FindFirstByConditionAsync(
                    x => x.Id == selectedLocation.PropertyId && x.BuyerId == buyer.Id && x.IsActive);
                propertyName = prop?.PropertyName;
                if (selectedLocation.OutletId.HasValue)
                {
                    BuyerOutlet? outlet = await _repository.BuyerOutlet.FindFirstByConditionAsync(
                        x => x.Id == selectedLocation.OutletId.Value && x.BuyerId == buyer.Id && x.IsActive);
                    outletName = outlet?.OutletName;
                }
            }

            string? existingRecipeSummary = null;
            if (request.SourceRecipeId.HasValue)
            {
                existingRecipeSummary = await BuildExistingRecipeSummaryAsync(buyer.Id, request.SourceRecipeId.Value, cancellationToken);
            }

            List<string> availableIngredientInputs = new(request.IngredientsToInclude ?? new List<string>());
            if (string.Equals(request.GenerationMode, "AVAILABLE_INGREDIENTS", StringComparison.OrdinalIgnoreCase)
                && request.LocationId.HasValue)
            {
                List<CulinaryAvailableStockItemDto> stock = await LoadBoundedStockAsync(buyer.Id, request.LocationId.Value, 30, cancellationToken);
                foreach (CulinaryAvailableStockItemDto s in stock)
                {
                    string entry = $"{s.Description} ({s.OnHandQty:0.##} {s.BaseUom})";
                    if (!availableIngredientInputs.Contains(entry, StringComparer.OrdinalIgnoreCase))
                    {
                        availableIngredientInputs.Add(entry);
                    }
                }
            }

            object runtimeConfig = BuildRuntimeConfigPayload(cfg, request.PreferredProvider, roleId);
            string genRef = string.IsNullOrWhiteSpace(request.GenerationReference)
                ? $"GEN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}"
                : request.GenerationReference.Trim();
            string correlationId = Guid.NewGuid().ToString("N");

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, existingDraft?.Id ?? Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_REQUESTED,
                $"Mode={request.GenerationMode}; GenRef={genRef}; Preferred={request.PreferredProvider ?? "DEFAULT"}", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, existingDraft?.Id ?? Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_STARTED,
                $"GenRef={genRef}; CorrelationId={correlationId}", userId);
            await _repository.SaveAsync();

            string taskName = string.Equals(request.GenerationMode, "RECIPE_DOCUMENT", StringComparison.OrdinalIgnoreCase)
                ? "RECIPE_DOCUMENT_INTERPRETATION"
                : string.Equals(request.GenerationMode, "DISH_PHOTOGRAPH", StringComparison.OrdinalIgnoreCase)
                    ? "RECIPE_IMAGE_INTERPRETATION"
                    : "RECIPE_GENERATION";

            object pythonBody = new
            {
                task = taskName,
                generation_reference = genRef,
                input = new
                {
                    generation_mode = request.GenerationMode,
                    recipe_name = request.RecipeName,
                    recipe_description = request.RecipeDescription,
                    cuisine = request.Cuisine,
                    category = request.Category,
                    recipe_type = request.RecipeType,
                    servings = request.Servings,
                    serving_size = request.ServingSize,
                    serving_uom = request.ServingUom,
                    preparation_style = request.PreparationStyle,
                    cooking_method = request.CookingMethod,
                    spice_level = request.SpiceLevel,
                    dietary_preferences = request.DietaryPreferences ?? new List<string>(),
                    ingredients_to_include = availableIngredientInputs,
                    ingredients_to_exclude = request.IngredientsToExclude ?? new List<string>(),
                    allergens_to_avoid = request.AllergensToAvoid ?? new List<string>(),
                    preferred_ingredients = request.PreferredIngredients ?? new List<string>(),
                    target_ingredient_budget = request.TargetIngredientBudget,
                    target_cost_per_serving = request.TargetCostPerServing,
                    target_selling_price = request.TargetSellingPrice,
                    target_food_cost_percent = request.TargetFoodCostPercent,
                    property_name = propertyName ?? string.Empty,
                    kitchen_name = selectedLocation?.LocationName ?? string.Empty,
                    outlet_name = outletName ?? string.Empty,
                    additional_instructions = request.AdditionalInstructions,
                    existing_recipe_summary = existingRecipeSummary,
                    source_document_name = request.SourceDocumentName,
                    extracted_document_text = request.ExtractedDocumentText,
                    image_base64 = request.ImageBase64,
                    image_mime_type = request.ImageMimeType,
                    preferred_provider = string.IsNullOrWhiteSpace(request.PreferredProvider) ? null : request.PreferredProvider.Trim().ToUpperInvariant(),
                    currency = string.IsNullOrWhiteSpace(buyer.Currency) ? "AED" : buyer.Currency
                },
                runtime_config = runtimeConfig
            };

            PythonGenerateResponse parsed;
            try
            {
                parsed = await PostCulinaryEngineAsync<PythonGenerateResponse>(
                    "/v1/recipes/generate", pythonBody, cfg.RequestTimeoutSeconds + 15, cancellationToken);
            }
            catch (Exception ex)
            {
                string sanitized = SanitizeMessage(ex.Message);
                _repository.SilaCulinaryAiUsageLog.Create(new SilaCulinaryAiUsageLog
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    DraftId = existingDraft?.Id,
                    DraftReference = existingDraft?.DraftReference,
                    GenerationReference = genRef,
                    CorrelationId = correlationId,
                    Feature = taskName,
                    Provider = request.PreferredProvider ?? cfg.OperatingMode,
                    Model = cfg.OperatingMode == SilaCulinaryAiSettings.MODE_GEMINI ? (cfg.GeminiModel ?? "") :
                            cfg.OperatingMode == SilaCulinaryAiSettings.MODE_CLAUDE ? (cfg.ClaudeModel ?? "") : (cfg.LocalModel ?? ""),
                    Success = false,
                    ErrorCode = "GENERATION_FAILED",
                    SanitizedError = sanitized,
                    ActorUserId = userId,
                    IsActive = true
                });
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, existingDraft?.Id ?? Guid.Empty,
                    Common.SILA_AUDIT_CULINARY_RECIPE_FAILED,
                    $"GenRef={genRef}; Error={sanitized}", userId);
                await _repository.SaveAsync();
                throw new BadRequestCustomException("AI Recipe generation failed.", sanitized);
            }

            string payloadJson = JsonSerializer.Serialize(parsed.Recipe, SnakeJson);
            List<CulinaryValidationWarningDto> parsedWarnings = parsed.ValidationWarnings ?? new List<CulinaryValidationWarningDto>();
            string warningsJson = JsonSerializer.Serialize(parsedWarnings, SnakeJson);
            string validationStatus = parsed.ValidationStatus ?? "VALID_WITH_CHEF_REVIEW";

            SilaCulinaryRecipeDraft draft;
            if (existingDraft == null)
            {
                draft = new SilaCulinaryRecipeDraft
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    DraftReference = $"AIR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    RecipeName = Truncate(parsed.Recipe.Header.ProposedName, 200),
                    GenerationMode = request.GenerationMode,
                    Status = parsedWarnings.Any(w => w.Severity == "ERROR") ? "VALIDATION_REQUIRED" : "GENERATED",
                    PropertyId = selectedLocation?.PropertyId,
                    PropertyName = propertyName,
                    LocationId = selectedLocation?.Id,
                    KitchenName = selectedLocation?.LocationName,
                    OutletId = selectedLocation?.OutletId,
                    OutletName = outletName,
                    SourceRecipeId = request.SourceRecipeId,
                    SourceDocumentName = request.SourceDocumentName,
                    BaseServings = parsed.Recipe.Header.BaseServings > 0 ? parsed.Recipe.Header.BaseServings : request.Servings,
                    IngredientCount = parsed.Recipe.Ingredients.Count,
                    Provider = parsed.AiMetadata.Provider,
                    Model = parsed.AiMetadata.Model,
                    PromptVersion = parsed.AiMetadata.PromptVersion,
                    GenerationReference = parsed.AiMetadata.GenerationReference,
                    ValidationStatus = validationStatus,
                    RevisionNumber = 1,
                    PayloadJson = payloadJson,
                    ValidationWarningsJson = warningsJson,
                    ConversationJson = JsonSerializer.Serialize(new List<CulinaryConversationTurnDto>
                    {
                        new()
                        {
                            Role = "SYSTEM",
                            Message = $"Generated via {parsed.AiMetadata.Provider} ({parsed.AiMetadata.Model}).",
                            Provider = parsed.AiMetadata.Provider,
                            Model = parsed.AiMetadata.Model,
                            TimestampUtc = DateTime.UtcNow
                        }
                    }, CamelJson),
                    TotalInputTokens = parsed.Usage.InputTokens,
                    TotalOutputTokens = parsed.Usage.OutputTokens,
                    EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                    CreatedByUserId = userId,
                    CreatedByName = actorName,
                    IsActive = true
                };
                _repository.SilaCulinaryRecipeDraft.Create(draft);
            }
            else
            {
                draft = existingDraft;
                draft.RecipeName = Truncate(parsed.Recipe.Header.ProposedName, 200);
                draft.GenerationMode = request.GenerationMode;
                draft.Status = parsedWarnings.Any(w => w.Severity == "ERROR") ? "VALIDATION_REQUIRED" : "GENERATED";
                draft.BaseServings = parsed.Recipe.Header.BaseServings > 0 ? parsed.Recipe.Header.BaseServings : request.Servings;
                draft.IngredientCount = parsed.Recipe.Ingredients.Count;
                draft.Provider = parsed.AiMetadata.Provider;
                draft.Model = parsed.AiMetadata.Model;
                draft.PromptVersion = parsed.AiMetadata.PromptVersion;
                draft.GenerationReference = parsed.AiMetadata.GenerationReference;
                draft.ValidationStatus = validationStatus;
                draft.RevisionNumber += 1;
                draft.PayloadJson = payloadJson;
                draft.ValidationWarningsJson = warningsJson;
                draft.TotalInputTokens += parsed.Usage.InputTokens;
                draft.TotalOutputTokens += parsed.Usage.OutputTokens;
                draft.EstimatedCostUsd += parsed.Usage.EstimatedCostUsd ?? 0m;
            }

            _repository.SilaCulinaryRecipeRevision.Create(new SilaCulinaryRecipeRevision
            {
                Id = Guid.NewGuid(),
                DraftId = draft.Id,
                BuyerId = buyer.Id,
                RevisionNumber = draft.RevisionNumber,
                ChangeType = "GENERATED",
                ChangeSummary = $"Generated {draft.RecipeName} ({request.GenerationMode}) via {parsed.AiMetadata.Provider}/{parsed.AiMetadata.Model}.",
                PayloadJson = payloadJson,
                Provider = parsed.AiMetadata.Provider,
                Model = parsed.AiMetadata.Model,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            _repository.SilaCulinaryAiUsageLog.Create(new SilaCulinaryAiUsageLog
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftId = draft.Id,
                DraftReference = draft.DraftReference,
                GenerationReference = parsed.AiMetadata.GenerationReference,
                CorrelationId = correlationId,
                Feature = taskName,
                Provider = parsed.AiMetadata.Provider,
                Model = parsed.AiMetadata.Model,
                Success = true,
                UsedFallback = parsed.UsedFallback,
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.TotalTokens,
                EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                LatencyMs = parsed.Usage.LatencyMs,
                ValidationRetries = parsed.Usage.ValidationRetries,
                ActorUserId = userId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_PROVIDER,
                $"Draft={draft.DraftReference}; Provider={parsed.AiMetadata.Provider}; Model={parsed.AiMetadata.Model}; Fallback={parsed.UsedFallback}", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_RECEIVED,
                $"Draft={draft.DraftReference}; Tokens={parsed.Usage.TotalTokens}; LatencyMs={parsed.Usage.LatencyMs}", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_VALIDATED,
                $"Draft={draft.DraftReference}; ValidationStatus={validationStatus}; Warnings={parsedWarnings.Count}", userId);

            await _repository.SaveAsync();
            return await MapDetailDtoAsync(draft, cancellationToken);
        }

        public async Task<CulinaryRefineResponseDto> RefineAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? draftId,
            CulinaryRefineRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Instruction) || request.CurrentPayload == null)
            {
                throw new BadRequestCustomException("Invalid refinement request.", "Provide both an instruction and the current recipe draft.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration cfg = await GetEffectiveConfigAsync(buyer.Id);
            EnsurePhase1AEnabled(cfg);
            await EnforceTenantBudgetsAsync(buyer.Id, cfg, cancellationToken);

            SilaCulinaryRecipeDraft? draft = draftId.HasValue
                ? await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId.Value, cancellationToken)
                : null;

            string genRef = $"REF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            string correlationId = Guid.NewGuid().ToString("N");
            object runtimeConfig = BuildRuntimeConfigPayload(cfg, request.PreferredProvider, roleId);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft?.Id ?? Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_REFINED,
                $"Refinement requested; Draft={draft?.DraftReference ?? "UNSAVED"}; GenRef={genRef}", userId);

            object pythonBody = new
            {
                instruction = request.Instruction.Trim(),
                current_recipe = request.CurrentPayload,
                preferred_provider = string.IsNullOrWhiteSpace(request.PreferredProvider) ? null : request.PreferredProvider.Trim().ToUpperInvariant(),
                runtime_config = runtimeConfig
            };

            PythonRefineResponse parsed;
            try
            {
                parsed = await PostCulinaryEngineAsync<PythonRefineResponse>(
                    "/v1/recipes/refine", pythonBody, cfg.RequestTimeoutSeconds + 15, cancellationToken);
            }
            catch (Exception ex)
            {
                string sanitized = SanitizeMessage(ex.Message);
                _repository.SilaCulinaryAiUsageLog.Create(new SilaCulinaryAiUsageLog
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    DraftId = draft?.Id,
                    DraftReference = draft?.DraftReference,
                    GenerationReference = genRef,
                    CorrelationId = correlationId,
                    Feature = "RECIPE_REFINEMENT",
                    Provider = request.PreferredProvider ?? cfg.OperatingMode,
                    Model = "",
                    Success = false,
                    ErrorCode = "REFINEMENT_FAILED",
                    SanitizedError = sanitized,
                    ActorUserId = userId,
                    IsActive = true
                });
                await _repository.SaveAsync();
                throw new BadRequestCustomException("Conversational refinement failed.", sanitized);
            }

            _repository.SilaCulinaryAiUsageLog.Create(new SilaCulinaryAiUsageLog
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftId = draft?.Id,
                DraftReference = draft?.DraftReference,
                GenerationReference = genRef,
                CorrelationId = correlationId,
                Feature = "RECIPE_REFINEMENT",
                Provider = parsed.Provider,
                Model = parsed.Model,
                Success = true,
                UsedFallback = parsed.UsedFallback,
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.TotalTokens,
                EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                LatencyMs = parsed.Usage.LatencyMs,
                ValidationRetries = parsed.Usage.ValidationRetries,
                ActorUserId = userId,
                IsActive = true
            });
            await _repository.SaveAsync();

            return new CulinaryRefineResponseDto
            {
                ProposedPayload = parsed.ProposedRecipe,
                DiffSummary = parsed.DiffSummary,
                ValidationWarnings = parsed.ValidationWarnings,
                Provider = parsed.Provider,
                Model = parsed.Model,
                UsedFallback = parsed.UsedFallback,
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                LatencyMs = parsed.Usage.LatencyMs,
                SummaryMessage = parsed.SummaryMessage
            };
        }

        public async Task<CulinaryRefineResponseDto> RegenerateSectionAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? draftId,
            CulinaryRegenerateSectionRequestDto request, CancellationToken cancellationToken)
        {
            if (request.CurrentPayload == null || request.Sections == null || request.Sections.Count == 0)
            {
                throw new BadRequestCustomException("Invalid regeneration request.", "Specify at least one section to regenerate.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration cfg = await GetEffectiveConfigAsync(buyer.Id);
            EnsurePhase1AEnabled(cfg);
            await EnforceTenantBudgetsAsync(buyer.Id, cfg, cancellationToken);

            SilaCulinaryRecipeDraft? draft = draftId.HasValue
                ? await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId.Value, cancellationToken)
                : null;

            string genRef = $"REG-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            string correlationId = Guid.NewGuid().ToString("N");
            object runtimeConfig = BuildRuntimeConfigPayload(cfg, request.PreferredProvider, roleId);

            object pythonBody = new
            {
                current_recipe = request.CurrentPayload,
                sections = request.Sections,
                locked_fields = request.LockedFields ?? new List<string>(),
                locked_ingredient_refs = request.LockedIngredientRefs ?? new List<string>(),
                chef_guidance = request.ChefGuidance ?? string.Empty,
                preferred_provider = string.IsNullOrWhiteSpace(request.PreferredProvider) ? null : request.PreferredProvider.Trim().ToUpperInvariant(),
                runtime_config = runtimeConfig
            };

            PythonRefineResponse parsed = await PostCulinaryEngineAsync<PythonRefineResponse>(
                "/v1/recipes/regenerate-section", pythonBody, cfg.RequestTimeoutSeconds + 15, cancellationToken);

            _repository.SilaCulinaryAiUsageLog.Create(new SilaCulinaryAiUsageLog
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftId = draft?.Id,
                DraftReference = draft?.DraftReference,
                GenerationReference = genRef,
                CorrelationId = correlationId,
                Feature = "SELECTIVE_REGENERATION",
                Provider = parsed.Provider,
                Model = parsed.Model,
                Success = true,
                UsedFallback = parsed.UsedFallback,
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.TotalTokens,
                EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                LatencyMs = parsed.Usage.LatencyMs,
                ValidationRetries = parsed.Usage.ValidationRetries,
                ActorUserId = userId,
                IsActive = true
            });
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft?.Id ?? Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_REGEN,
                $"Sections={string.Join(",", request.Sections)}; Provider={parsed.Provider}", userId);
            await _repository.SaveAsync();

            return new CulinaryRefineResponseDto
            {
                ProposedPayload = parsed.ProposedRecipe,
                DiffSummary = parsed.DiffSummary,
                ValidationWarnings = parsed.ValidationWarnings,
                Provider = parsed.Provider,
                Model = parsed.Model,
                UsedFallback = parsed.UsedFallback,
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                EstimatedCostUsd = parsed.Usage.EstimatedCostUsd ?? 0m,
                LatencyMs = parsed.Usage.LatencyMs,
                SummaryMessage = parsed.SummaryMessage
            };
        }

        public async Task<CulinaryScaleV2ResultDto> ScaleV2Async(
            Guid organizationId, Guid userId, Guid? draftId,
            CulinaryScaleV2RequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            object pythonBody = new
            {
                recipe_name = request.RecipeName,
                base_servings = request.BaseServings,
                target_servings = request.TargetServings,
                base_serving_size = request.BaseServingSize,
                target_serving_size = request.TargetServingSize,
                prep_buffer_percent = request.PrepBufferPercent,
                use_nonlinear_rules = request.UseNonlinearRules,
                ingredients = request.Ingredients
            };

            CulinaryScaleV2ResultDto result = await PostCulinaryEngineAsync<CulinaryScaleV2ResultDto>(
                "/v1/engines/scale-v2", pythonBody, 15, cancellationToken);

            if (draftId.HasValue)
            {
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draftId.Value,
                    Common.SILA_AUDIT_CULINARY_RECIPE_SCALED,
                    $"Scaled {request.BaseServings} -> {request.TargetServings} servings (size {request.BaseServingSize} -> {request.TargetServingSize})",
                    userId);
                await _repository.SaveAsync();
            }

            return result;
        }

        public async Task<CulinaryDocumentExtractDto> ExtractDocumentAsync(
            Guid organizationId, Guid userId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestCustomException("No recipe document uploaded.", "Upload a PDF, DOCX, XLSX, or TXT file.");
            }
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new BadRequestCustomException("Document too large.", "Maximum allowed document size is 10 MB.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            string url = $"{GetCulinaryEngineBaseUrl()}/v1/recipes/extract-document";

            using MultipartFormDataContent form = new();
            await using Stream stream = file.OpenReadStream();
            using StreamContent sc = new(stream);
            sc.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            form.Add(sc, "file", file.FileName);

            using HttpResponseMessage resp = await client.PostAsync(url, form, cancellationToken);
            string raw = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                throw new BadRequestCustomException("Recipe document extraction failed.", ExtractPythonError(raw));
            }

            CulinaryDocumentExtractDto dto = JsonSerializer.Deserialize<CulinaryDocumentExtractDto>(raw, SnakeJson)
                ?? throw new BadRequestCustomException("Recipe document extraction failed.", "Empty extraction response.");

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_DOC,
                $"File={Truncate(file.FileName, 120)}; Type={dto.FileType}; Lines={dto.LineCount}", userId);
            await _repository.SaveAsync();
            return dto;
        }

        public async Task<CulinaryDishImagePreparedDto> PrepareDishImageAsync(
            Guid organizationId, Guid userId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestCustomException("No dish photograph uploaded.", "Select a JPG, PNG, or WEBP image.");
            }
            if (file.Length > 8 * 1024 * 1024)
            {
                throw new BadRequestCustomException("Image too large.", "Maximum allowed image size is 8 MB.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            string url = $"{GetCulinaryEngineBaseUrl()}/v1/recipes/prepare-dish-image";

            using MultipartFormDataContent form = new();
            await using Stream stream = file.OpenReadStream();
            using StreamContent sc = new(stream);
            sc.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType);
            form.Add(sc, "file", file.FileName);

            using HttpResponseMessage resp = await client.PostAsync(url, form, cancellationToken);
            string raw = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                throw new BadRequestCustomException("Dish image validation failed.", ExtractPythonError(raw));
            }

            CulinaryDishImagePreparedDto dto = JsonSerializer.Deserialize<CulinaryDishImagePreparedDto>(raw, SnakeJson)
                ?? throw new BadRequestCustomException("Dish image validation failed.", "Empty image validation response.");

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_IMAGE,
                $"Image={Truncate(file.FileName, 120)}; Dimensions={dto.Width}x{dto.Height}", userId);
            await _repository.SaveAsync();
            return dto;
        }

        public async Task CancelGenerationAsync(
            Guid organizationId, Guid userId, string generationReference, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            string url = $"{GetCulinaryEngineBaseUrl()}/v1/recipes/cancel/{Uri.EscapeDataString(generationReference)}";
            try
            {
                using HttpResponseMessage _ = await client.PostAsync(url, null, cancellationToken);
            }
            catch
            {
                // Best-effort cancellation signal to Python worker.
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, Guid.Empty,
                Common.SILA_AUDIT_CULINARY_RECIPE_CANCELLED,
                $"Generation cancelled: {Truncate(generationReference, 80)}", userId);
            await _repository.SaveAsync();
        }

        public async Task<List<CulinaryRecipeDraftSummaryDto>> ListDraftsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? status, string? search, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            List<Guid> allowedLocations = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, cancellationToken);
            bool fullAccess = SilaAccess.HasFullAccess(roleId);

            IQueryable<SilaCulinaryRecipeDraft> query = _repository.SilaCulinaryRecipeDraft
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (!fullAccess)
            {
                query = query.Where(x => x.CreatedByUserId == userId || (x.LocationId != null && allowedLocations.Contains(x.LocationId.Value)));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToUpperInvariant();
                query = query.Where(x => x.Status == st);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(x => x.RecipeName.Contains(term) || x.DraftReference.Contains(term));
            }

            List<SilaCulinaryRecipeDraft> rows = await query
                .OrderByDescending(x => x.DateUpdated)
                .Take(100)
                .ToListAsync(cancellationToken);

            return rows.Select(MapSummaryDto).ToList();
        }

        public async Task<CulinaryRecipeDraftDetailDto> GetDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            return await MapDetailDtoAsync(draft, cancellationToken);
        }

        public async Task<CulinaryRecipeDraftDetailDto> SaveDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            CulinarySaveDraftRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);

            if (request.ExpectedRevisionNumber > 0 && request.ExpectedRevisionNumber != draft.RevisionNumber)
            {
                throw new BadRequestCustomException(
                    "Concurrent modification detected.",
                    $"Draft revision is currently {draft.RevisionNumber}, but your editor submitted {request.ExpectedRevisionNumber}. Reload the latest revision before saving.");
            }

            // Deterministically validate & normalize via Python validator before saving.
            PythonValidationResponse validated = await PostCulinaryEngineAsync<PythonValidationResponse>(
                "/v1/recipes/validate", request.Payload, 15, cancellationToken);

            string payloadJson = JsonSerializer.Serialize(validated.Recipe, SnakeJson);
            List<CulinaryValidationWarningDto> validatedWarnings = validated.Warnings ?? new List<CulinaryValidationWarningDto>();
            string warningsJson = JsonSerializer.Serialize(validatedWarnings, SnakeJson);

            draft.RecipeName = Truncate(validated.Recipe.Header.ProposedName, 200);
            draft.BaseServings = validated.Recipe.Header.BaseServings;
            draft.IngredientCount = validated.Recipe.Ingredients.Count;
            draft.ValidationStatus = validated.ValidationStatus;
            draft.PayloadJson = payloadJson;
            draft.ValidationWarningsJson = warningsJson;
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                draft.Status = NormalizeDraftStatus(request.Status);
            }
            else if (draft.Status == "GENERATED" || draft.Status == "VALIDATION_REQUIRED")
            {
                draft.Status = validatedWarnings.Any(w => w.Severity == "ERROR") ? "VALIDATION_REQUIRED" : "CHEF_REVIEW";
            }

            if (request.Conversation != null)
            {
                draft.ConversationJson = JsonSerializer.Serialize(request.Conversation, CamelJson);
            }

            draft.RevisionNumber += 1;

            _repository.SilaCulinaryRecipeRevision.Create(new SilaCulinaryRecipeRevision
            {
                Id = Guid.NewGuid(),
                DraftId = draft.Id,
                BuyerId = buyer.Id,
                RevisionNumber = draft.RevisionNumber,
                ChangeType = string.IsNullOrWhiteSpace(request.ChangeType) ? "CHEF_EDIT" : Truncate(request.ChangeType, 60),
                ChangeSummary = string.IsNullOrWhiteSpace(request.ChangeSummary) ? "Chef updated recipe draft." : Truncate(request.ChangeSummary, 1000),
                PayloadJson = payloadJson,
                Provider = draft.Provider,
                Model = draft.Model,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_SAVED,
                $"Draft={draft.DraftReference}; Rev={draft.RevisionNumber}; Status={draft.Status}", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_REVISED,
                $"Draft={draft.DraftReference}; Change={Truncate(request.ChangeSummary, 150)}", userId);

            await _repository.SaveAsync();
            return await MapDetailDtoAsync(draft, cancellationToken);
        }

        public async Task<CulinaryRecipeDraftDetailDto> DuplicateDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft source = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);

            SilaCulinaryRecipeDraft copy = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftReference = $"AIR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                RecipeName = Truncate($"{source.RecipeName} (Copy)", 200),
                GenerationMode = source.GenerationMode,
                Status = "DRAFT",
                PropertyId = source.PropertyId,
                PropertyName = source.PropertyName,
                LocationId = source.LocationId,
                KitchenName = source.KitchenName,
                OutletId = source.OutletId,
                OutletName = source.OutletName,
                SourceRecipeId = source.SourceRecipeId,
                SourceDocumentName = source.SourceDocumentName,
                BaseServings = source.BaseServings,
                IngredientCount = source.IngredientCount,
                Provider = source.Provider,
                Model = source.Model,
                PromptVersion = source.PromptVersion,
                GenerationReference = source.GenerationReference,
                ValidationStatus = source.ValidationStatus,
                RevisionNumber = 1,
                PayloadJson = source.PayloadJson,
                ValidationWarningsJson = source.ValidationWarningsJson,
                ConversationJson = source.ConversationJson,
                TotalInputTokens = 0,
                TotalOutputTokens = 0,
                EstimatedCostUsd = 0m,
                CreatedByUserId = userId,
                CreatedByName = actorName,
                IsActive = true
            };
            _repository.SilaCulinaryRecipeDraft.Create(copy);
            _repository.SilaCulinaryRecipeRevision.Create(new SilaCulinaryRecipeRevision
            {
                Id = Guid.NewGuid(),
                DraftId = copy.Id,
                BuyerId = buyer.Id,
                RevisionNumber = 1,
                ChangeType = "DUPLICATED",
                ChangeSummary = $"Duplicated from {source.DraftReference} (Rev {source.RevisionNumber}).",
                PayloadJson = source.PayloadJson,
                Provider = source.Provider,
                Model = source.Model,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, copy.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_DUPLICATED,
                $"Source={source.DraftReference}; NewDraft={copy.DraftReference}", userId);

            await _repository.SaveAsync();
            return await MapDetailDtoAsync(copy, cancellationToken);
        }

        public async Task<CulinaryRecipeDraftDetailDto> RestoreRevisionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, int revisionNumber, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeRevision? rev = await _repository.SilaCulinaryRecipeRevision.FindFirstByConditionAsync(
                x => x.DraftId == draft.Id && x.BuyerId == buyer.Id && x.RevisionNumber == revisionNumber && x.IsActive);
            if (rev == null)
            {
                throw new NotFoundCustomException("Revision not found.", $"Revision #{revisionNumber} was not found for this draft.");
            }

            CulinaryStructuredRecipePayloadDto payload = JsonSerializer.Deserialize<CulinaryStructuredRecipePayloadDto>(rev.PayloadJson, SnakeJson)
                ?? new CulinaryStructuredRecipePayloadDto();

            draft.RecipeName = Truncate(payload.Header.ProposedName, 200);
            draft.BaseServings = payload.Header.BaseServings;
            draft.IngredientCount = payload.Ingredients.Count;
            draft.PayloadJson = rev.PayloadJson;
            draft.Status = "CHEF_REVIEW";
            draft.RevisionNumber += 1;

            _repository.SilaCulinaryRecipeRevision.Create(new SilaCulinaryRecipeRevision
            {
                Id = Guid.NewGuid(),
                DraftId = draft.Id,
                BuyerId = buyer.Id,
                RevisionNumber = draft.RevisionNumber,
                ChangeType = "RESTORED",
                ChangeSummary = $"Restored snapshot from Revision #{revisionNumber}.",
                PayloadJson = rev.PayloadJson,
                Provider = draft.Provider,
                Model = draft.Model,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_RESTORED,
                $"Draft={draft.DraftReference}; RestoredFromRev={revisionNumber}; NewRev={draft.RevisionNumber}", userId);

            await _repository.SaveAsync();
            return await MapDetailDtoAsync(draft, cancellationToken);
        }

        public async Task<CulinaryRecipeDraftDetailDto> CancelDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await GetAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            draft.Status = "CANCELLED";
            draft.RevisionNumber += 1;

            _repository.SilaCulinaryRecipeRevision.Create(new SilaCulinaryRecipeRevision
            {
                Id = Guid.NewGuid(),
                DraftId = draft.Id,
                BuyerId = buyer.Id,
                RevisionNumber = draft.RevisionNumber,
                ChangeType = "CANCELLED",
                ChangeSummary = "Draft cancelled by user.",
                PayloadJson = draft.PayloadJson,
                Provider = draft.Provider,
                Model = draft.Model,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                Common.SILA_AUDIT_CULINARY_RECIPE_CANCELLED,
                $"Draft={draft.DraftReference} cancelled", userId);

            await _repository.SaveAsync();
            return await MapDetailDtoAsync(draft, cancellationToken);
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string format, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto detail = await GetDraftAsync(organizationId, userId, roleId, draftId, cancellationToken);
            string fmt = (format ?? "JSON").Trim().ToUpperInvariant();
            if (fmt == "CSV")
            {
                StringBuilder sb = new();
                sb.AppendLine("TempRef,Description,NormalizedDescription,Category,Quantity,RecipeUom,QuantityBasis,YieldPercent,ScalingMethod,MaterialMappingStatus");
                foreach (CulinaryIngredientLineDto line in detail.Payload.Ingredients)
                {
                    sb.AppendLine(string.Join(",",
                        CsvCell(line.TempRef),
                        CsvCell(line.Description),
                        CsvCell(line.NormalizedDescription),
                        CsvCell(line.Category),
                        line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        CsvCell(line.RecipeUom),
                        CsvCell(line.QuantityBasis),
                        line.YieldPercent.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        CsvCell(line.ScalingMethod),
                        CsvCell(line.MaterialMappingStatus)));
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"{detail.DraftReference}.csv");
            }

            string json = JsonSerializer.Serialize(detail, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return (Encoding.UTF8.GetBytes(json), "application/json", $"{detail.DraftReference}.json");
        }

        private async Task<SilaCulinaryAiConfiguration> GetEffectiveConfigAsync(Guid buyerId)
        {
            SilaCulinaryAiConfiguration? entity = await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive);
            return entity ?? SilaCulinaryAiSettings.CreateDefault(buyerId);
        }

        private static void EnsurePhase1AEnabled(SilaCulinaryAiConfiguration cfg)
        {
            SilaCulinaryAiFeatureFlagsDto flags = SilaCulinaryAiSettings.ParseFeatureFlags(cfg.FeatureFlagsJson);
            if (!flags.CulinarySuiteEnabled)
            {
                throw new ForBiddenCustomException(
                    "Culinary Intelligence Suite disabled.",
                    "Ask your administrator to enable the Culinary Intelligence Suite in AI Engine Setup.");
            }
        }

        private static void ValidateGenerationRequest(CulinaryGenerateRecipeRequestDto request)
        {
            if (request.Servings <= 0 || request.Servings > 10000)
            {
                throw new BadRequestCustomException("Invalid serving count.", "Servings must be greater than 0 and at most 10,000.");
            }
            if (request.ServingSize <= 0 || request.ServingSize > 100000)
            {
                throw new BadRequestCustomException("Invalid serving size.", "Serving size must be greater than 0.");
            }
            bool hasInput = !string.IsNullOrWhiteSpace(request.RecipeName)
                || !string.IsNullOrWhiteSpace(request.AdditionalInstructions)
                || !string.IsNullOrWhiteSpace(request.ExtractedDocumentText)
                || !string.IsNullOrWhiteSpace(request.ImageBase64)
                || request.SourceRecipeId.HasValue
                || (request.IngredientsToInclude != null && request.IngredientsToInclude.Count > 0);
            if (!hasInput)
            {
                throw new BadRequestCustomException("Recipe input required.", "Enter a recipe name, chef instructions, document, photo, or ingredients.");
            }
        }

        private async Task EnforceTenantBudgetsAsync(Guid buyerId, SilaCulinaryAiConfiguration cfg, CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            DateTime dayStart = now.Date;
            DateTime monthStart = new(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            List<SilaCulinaryAiUsageLog> monthLogs = await _repository.SilaCulinaryAiUsageLog
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.DateCreated >= monthStart)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryAiUsageLog> todayLogs = monthLogs.Where(x => x.DateCreated >= dayStart).ToList();

            if (cfg.DailyRequestLimit > 0 && todayLogs.Count >= cfg.DailyRequestLimit)
            {
                throw new BadRequestCustomException("Daily AI request limit reached.", $"Daily limit of {cfg.DailyRequestLimit} requests has been reached.");
            }
            if (cfg.MonthlyRequestLimit > 0 && monthLogs.Count >= cfg.MonthlyRequestLimit)
            {
                throw new BadRequestCustomException("Monthly AI request limit reached.", $"Monthly limit of {cfg.MonthlyRequestLimit} requests has been reached.");
            }
            if (cfg.DailyTokenBudget > 0 && todayLogs.Sum(x => (long)x.TotalTokens) >= cfg.DailyTokenBudget)
            {
                throw new BadRequestCustomException("Daily AI token budget exceeded.", $"Daily token budget of {cfg.DailyTokenBudget:N0} tokens has been reached.");
            }
            if (cfg.MonthlyCostBudgetUsd > 0 && monthLogs.Sum(x => x.EstimatedCostUsd) >= cfg.MonthlyCostBudgetUsd)
            {
                throw new BadRequestCustomException("Monthly AI cost budget exceeded.", $"Monthly cost budget of ${cfg.MonthlyCostBudgetUsd:0.00} has been reached.");
            }
        }

        private object BuildRuntimeConfigPayload(SilaCulinaryAiConfiguration cfg, string? preferredProvider, Guid roleId)
        {
            if (!string.IsNullOrWhiteSpace(preferredProvider))
            {
                string reqProv = preferredProvider.Trim().ToUpperInvariant();
                if (!CanUserSelectProvider(roleId) && !string.Equals(reqProv, cfg.OperatingMode, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ForBiddenCustomException(
                        "Provider selection not permitted.",
                        "Your role must use the administrator-configured AI engine.");
                }
                if (reqProv == SilaCulinaryAiSettings.MODE_GEMINI && (!cfg.AllowExternalAi || !cfg.GeminiEnabled))
                {
                    throw new ForBiddenCustomException("Gemini provider not enabled.", "Gemini is disabled or external AI consent is off.");
                }
                if (reqProv == SilaCulinaryAiSettings.MODE_CLAUDE && (!cfg.AllowExternalAi || !cfg.ClaudeEnabled))
                {
                    throw new ForBiddenCustomException("Claude provider not enabled.", "Claude is disabled or external AI consent is off.");
                }
                if (reqProv == SilaCulinaryAiSettings.MODE_LOCAL && !cfg.LocalEnabled)
                {
                    throw new BadRequestCustomException("Local AI disabled.", "Enable Local AI in AI Engine Setup.");
                }
            }

            string? geminiKey = cfg.AllowExternalAi && cfg.GeminiEnabled ? UnprotectOrNull(cfg.ProtectedGeminiApiKey) : null;
            string? claudeKey = cfg.AllowExternalAi && cfg.ClaudeEnabled ? UnprotectOrNull(cfg.ProtectedClaudeApiKey) : null;

            return new
            {
                operating_mode = cfg.OperatingMode,
                allow_external_ai = cfg.AllowExternalAi,
                local_enabled = cfg.LocalEnabled,
                local_endpoint = cfg.LocalEndpoint,
                local_model = cfg.LocalModel,
                gemini_enabled = cfg.GeminiEnabled,
                gemini_model = cfg.GeminiModel,
                gemini_api_key = geminiKey,
                claude_enabled = cfg.ClaudeEnabled,
                claude_model = cfg.ClaudeModel,
                claude_api_key = claudeKey,
                request_timeout_seconds = cfg.RequestTimeoutSeconds,
                max_output_tokens = cfg.MaxOutputTokens,
                temperature = cfg.Temperature,
                retry_count = cfg.RetryCount,
                fallback_policy = cfg.FallbackPolicy,
                task_routing = SilaCulinaryAiSettings.ParseTaskRouting(cfg.TaskRoutingJson)
            };
        }

        private string? UnprotectOrNull(string? encrypted)
        {
            if (string.IsNullOrWhiteSpace(encrypted)) return null;
            try { return _protector.Unprotect(encrypted); }
            catch { return null; }
        }

        private static bool CanUserSelectProvider(Guid roleId) =>
            SilaAccess.HasFullAccess(roleId) || roleId == Common.OUTLET_MANAGER_ROLE_ID;

        private static List<string> GetEnabledProviders(SilaCulinaryAiConfiguration cfg)
        {
            List<string> list = new();
            if (cfg.LocalEnabled) list.Add(SilaCulinaryAiSettings.MODE_LOCAL);
            if (cfg.AllowExternalAi && cfg.GeminiEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedGeminiApiKey))
                list.Add(SilaCulinaryAiSettings.MODE_GEMINI);
            if (cfg.AllowExternalAi && cfg.ClaudeEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedClaudeApiKey))
                list.Add(SilaCulinaryAiSettings.MODE_CLAUDE);
            if (cfg.OperatingMode == SilaCulinaryAiSettings.MODE_HYBRID)
                list.Add(SilaCulinaryAiSettings.MODE_HYBRID);
            return list;
        }

        private async Task<List<CulinaryAvailableStockItemDto>> LoadBoundedStockAsync(
            Guid buyerId, Guid locationId, int limit, CancellationToken cancellationToken)
        {
            var rows = await (
                from b in _repository.InventoryBalance.FindByCondition(x => x.BuyerId == buyerId && x.LocationId == locationId && x.IsActive && x.OnHandQty > 0)
                join m in _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                    on b.MaterialId equals m.Id
                orderby b.OnHandQty descending
                select new CulinaryAvailableStockItemDto
                {
                    MaterialId = m.Id,
                    MaterialCode = m.MaterialCode,
                    Description = m.Description,
                    OnHandQty = b.OnHandQty,
                    BaseUom = b.BaseUom
                })
                .Take(limit)
                .ToListAsync(cancellationToken);
            return rows;
        }

        private async Task<string?> BuildExistingRecipeSummaryAsync(Guid buyerId, Guid recipeId, CancellationToken cancellationToken)
        {
            Recipe? recipe = await _repository.Recipe.FindFirstByConditionAsync(
                x => x.Id == recipeId && x.BuyerId == buyerId && x.IsActive);
            if (recipe == null)
            {
                throw new NotFoundCustomException("Referenced recipe not found.", "Select an active recipe belonging to your organization.");
            }
            List<RecipeIngredient> lines = await _repository.RecipeIngredient
                .FindByCondition(x => x.RecipeId == recipe.Id && x.IsActive && x.Version == recipe.Version)
                .OrderBy(x => x.Sequence)
                .Take(40)
                .ToListAsync(cancellationToken);

            StringBuilder sb = new();
            sb.AppendLine($"Reference Recipe: {recipe.Name} ({recipe.RecipeCode}), Category: {recipe.Category}, Servings: {recipe.ServingQty} {recipe.ServingUom}");
            foreach (RecipeIngredient line in lines)
            {
                sb.AppendLine($"- {line.ItemName}: {line.Quantity:0.###} {line.Uom}");
            }
            return sb.ToString();
        }

        private async Task<SilaCulinaryRecipeDraft> GetAuthorizedDraftAsync(
            Guid buyerId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeDraft? draft = await _repository.SilaCulinaryRecipeDraft
                .FindFirstByConditionAsync(x => x.Id == draftId && x.BuyerId == buyerId && x.IsActive);
            if (draft == null)
            {
                throw new NotFoundCustomException("AI Recipe Draft not found.", "The requested recipe draft does not exist in your tenant.");
            }

            if (!SilaAccess.HasFullAccess(roleId))
            {
                if (draft.LocationId.HasValue)
                {
                    await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyerId, userId, roleId, draft.LocationId.Value, cancellationToken);
                }
                else if (draft.CreatedByUserId != userId)
                {
                    throw new ForBiddenCustomException("Draft access denied.", "You can only open drafts you created or drafts for your assigned kitchens.");
                }
            }

            return draft;
        }

        private async Task<T> PostCulinaryEngineAsync<T>(
            string path, object body, int timeoutSeconds, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 150)));

            string url = $"{GetCulinaryEngineBaseUrl()}{path}";
            string json = JsonSerializer.Serialize(body, SnakeJson);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync(url, content, cts.Token);
            string raw = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractPythonError(raw));
            }

            T? result = JsonSerializer.Deserialize<T>(raw, SnakeJson);
            if (result == null)
            {
                throw new InvalidOperationException("Culinary engine returned an empty response.");
            }
            return result;
        }

        private string GetCulinaryEngineBaseUrl() =>
            (_configuration[Common.CULINARY_ENGINE_URL] ?? "http://127.0.0.1:8013").TrimEnd('/');

        private void AddWorkflowEvent(
            Guid buyerId, string refType, Guid refId, string action, string comment, Guid actorUserId)
        {
            _repository.InventoryWorkflowEvent.Create(new InventoryWorkflowEvent
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ReferenceType = refType,
                ReferenceId = refId,
                Action = action,
                Comment = SanitizeMessage(Truncate(comment, 490)),
                ActorUserId = actorUserId,
                IsActive = true
            });
        }

        private static CulinaryRecipeDraftSummaryDto MapSummaryDto(SilaCulinaryRecipeDraft d) => new()
        {
            Id = d.Id,
            DraftReference = d.DraftReference,
            RecipeName = d.RecipeName,
            GenerationMode = d.GenerationMode,
            Status = d.Status,
            PropertyName = d.PropertyName,
            KitchenName = d.KitchenName,
            BaseServings = d.BaseServings,
            IngredientCount = d.IngredientCount,
            Provider = d.Provider,
            Model = d.Model,
            ValidationStatus = d.ValidationStatus,
            RevisionNumber = d.RevisionNumber,
            TotalTokens = d.TotalInputTokens + d.TotalOutputTokens,
            EstimatedCostUsd = d.EstimatedCostUsd,
            CreatedByName = d.CreatedByName,
            CreatedOnUtc = d.DateCreated,
            UpdatedOnUtc = d.DateUpdated
        };

        private async Task<CulinaryRecipeDraftDetailDto> MapDetailDtoAsync(SilaCulinaryRecipeDraft d, CancellationToken cancellationToken)
        {
            List<SilaCulinaryRecipeRevision> revisions = await _repository.SilaCulinaryRecipeRevision
                .FindByCondition(x => x.DraftId == d.Id && x.BuyerId == d.BuyerId && x.IsActive)
                .OrderByDescending(x => x.RevisionNumber)
                .ToListAsync(cancellationToken);

            CulinaryStructuredRecipePayloadDto payload = string.IsNullOrWhiteSpace(d.PayloadJson)
                ? new CulinaryStructuredRecipePayloadDto()
                : JsonSerializer.Deserialize<CulinaryStructuredRecipePayloadDto>(d.PayloadJson, SnakeJson) ?? new CulinaryStructuredRecipePayloadDto();

            List<CulinaryValidationWarningDto> warnings = string.IsNullOrWhiteSpace(d.ValidationWarningsJson)
                ? new List<CulinaryValidationWarningDto>()
                : JsonSerializer.Deserialize<List<CulinaryValidationWarningDto>>(d.ValidationWarningsJson, SnakeJson) ?? new List<CulinaryValidationWarningDto>();

            List<CulinaryConversationTurnDto> conversation = string.IsNullOrWhiteSpace(d.ConversationJson)
                ? new List<CulinaryConversationTurnDto>()
                : JsonSerializer.Deserialize<List<CulinaryConversationTurnDto>>(d.ConversationJson, CamelJson) ?? new List<CulinaryConversationTurnDto>();

            return new CulinaryRecipeDraftDetailDto
            {
                Id = d.Id,
                DraftReference = d.DraftReference,
                RecipeName = d.RecipeName,
                GenerationMode = d.GenerationMode,
                Status = d.Status,
                PropertyId = d.PropertyId,
                PropertyName = d.PropertyName,
                LocationId = d.LocationId,
                KitchenName = d.KitchenName,
                OutletId = d.OutletId,
                OutletName = d.OutletName,
                SourceRecipeId = d.SourceRecipeId,
                SourceDocumentName = d.SourceDocumentName,
                BaseServings = d.BaseServings,
                IngredientCount = d.IngredientCount,
                Provider = d.Provider,
                Model = d.Model,
                PromptVersion = d.PromptVersion,
                GenerationReference = d.GenerationReference,
                ValidationStatus = d.ValidationStatus,
                RevisionNumber = d.RevisionNumber,
                TotalInputTokens = d.TotalInputTokens,
                TotalOutputTokens = d.TotalOutputTokens,
                TotalTokens = d.TotalInputTokens + d.TotalOutputTokens,
                EstimatedCostUsd = d.EstimatedCostUsd,
                CreatedByName = d.CreatedByName,
                CreatedOnUtc = d.DateCreated,
                UpdatedOnUtc = d.DateUpdated,
                Payload = payload,
                ValidationWarnings = warnings,
                Conversation = conversation,
                Revisions = revisions.Select(r => new CulinaryRecipeRevisionDto
                {
                    Id = r.Id,
                    RevisionNumber = r.RevisionNumber,
                    ChangeType = r.ChangeType,
                    ChangeSummary = r.ChangeSummary,
                    Provider = r.Provider,
                    Model = r.Model,
                    ActorName = r.ActorName,
                    CreatedOnUtc = r.DateCreated,
                    Payload = JsonSerializer.Deserialize<CulinaryStructuredRecipePayloadDto>(r.PayloadJson, SnakeJson) ?? new CulinaryStructuredRecipePayloadDto()
                }).ToList()
            };
        }

        private static string NormalizeDraftStatus(string status)
        {
            string s = status.Trim().ToUpperInvariant();
            string[] allowed =
            {
                "DRAFT", "GENERATING", "GENERATED", "VALIDATION_REQUIRED",
                "CHEF_REVIEW", "READY_FOR_MATERIAL_MAPPING", "CANCELLED", "FAILED"
            };
            return allowed.Contains(s) ? s : "CHEF_REVIEW";
        }

        private static string SanitizeMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return "Unknown error.";
            return SecretPattern.Replace(msg, "[REDACTED]");
        }

        private static string ExtractPythonError(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Culinary engine call failed.";
            try
            {
                using JsonDocument doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("detail", out JsonElement detail))
                {
                    return SanitizeMessage(detail.ToString());
                }
            }
            catch
            {
                // Non-JSON response.
            }
            return SanitizeMessage(Truncate(raw, 400));
        }

        private static string Truncate(string? value, int maxLen)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLen ? value : value[..maxLen];
        }

        private static string CsvCell(string? value)
        {
            string s = value ?? string.Empty;
            if (s.StartsWith("=") || s.StartsWith("+") || s.StartsWith("-") || s.StartsWith("@"))
            {
                s = "'" + s;
            }
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private sealed class PythonAiMetadata
        {
            [JsonPropertyName("provider")] public string Provider { get; set; } = string.Empty;
            [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
            [JsonPropertyName("prompt_version")] public string PromptVersion { get; set; } = string.Empty;
            [JsonPropertyName("generation_reference")] public string GenerationReference { get; set; } = string.Empty;
        }

        private sealed class PythonUsage
        {
            [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
            [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
            [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
            [JsonPropertyName("estimated_cost_usd")] public decimal? EstimatedCostUsd { get; set; }
            [JsonPropertyName("latency_ms")] public int LatencyMs { get; set; }
            [JsonPropertyName("validation_retries")] public int ValidationRetries { get; set; }
        }

        private sealed class PythonGenerateResponse
        {
            [JsonPropertyName("recipe")] public CulinaryStructuredRecipePayloadDto Recipe { get; set; } = new();
            [JsonPropertyName("ai_metadata")] public PythonAiMetadata AiMetadata { get; set; } = new();
            [JsonPropertyName("validation_warnings")] public List<CulinaryValidationWarningDto> ValidationWarnings { get; set; } = new();
            [JsonPropertyName("validation_status")] public string ValidationStatus { get; set; } = "VALID_WITH_CHEF_REVIEW";
            [JsonPropertyName("used_fallback")] public bool UsedFallback { get; set; }
            [JsonPropertyName("usage")] public PythonUsage Usage { get; set; } = new();
        }

        private sealed class PythonRefineResponse
        {
            [JsonPropertyName("proposed_recipe")] public CulinaryStructuredRecipePayloadDto ProposedRecipe { get; set; } = new();
            [JsonPropertyName("diff_summary")] public List<CulinaryRecipeDiffItemDto> DiffSummary { get; set; } = new();
            [JsonPropertyName("validation_warnings")] public List<CulinaryValidationWarningDto> ValidationWarnings { get; set; } = new();
            [JsonPropertyName("provider")] public string Provider { get; set; } = string.Empty;
            [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
            [JsonPropertyName("used_fallback")] public bool UsedFallback { get; set; }
            [JsonPropertyName("usage")] public PythonUsage Usage { get; set; } = new();
            [JsonPropertyName("summary_message")] public string SummaryMessage { get; set; } = string.Empty;
        }

        private sealed class PythonValidationResponse
        {
            [JsonPropertyName("validation_status")] public string ValidationStatus { get; set; } = "VALID_WITH_CHEF_REVIEW";
            [JsonPropertyName("warnings")] public List<CulinaryValidationWarningDto> Warnings { get; set; } = new();
            [JsonPropertyName("recipe")] public CulinaryStructuredRecipePayloadDto Recipe { get; set; } = new();
        }
    }
}
