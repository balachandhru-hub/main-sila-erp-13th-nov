using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryMaterialService
    {
        Task<SilaCulinaryMaterialWorkspaceDto> GetWorkspaceAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialWorkspaceDto> MatchDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialMatchRequestDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialMappingLineDto> MatchSingleIngredientAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialMatchRequestDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialWorkspaceDto> RecalculateAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialRecalcDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialMappingLineDto> ConfirmMappingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialActionDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialWorkspaceDto> BulkAcceptExactAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialBulkAcceptDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialMappingLineDto> RejectMappingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialActionDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialMappingLineDto> PrepareRequestDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialRequestDraftCreateDto request, CancellationToken cancellationToken);

        Task<object> SuggestSubstitutionsAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialMappingLineDto> ApplySubstitutionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinarySubstitutionApplyDto request, CancellationToken cancellationToken);

        Task<List<SilaCulinaryMaterialSearchItemDto>> SearchMaterialsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? query, Guid? propertyId, int limit, CancellationToken cancellationToken);

        Task<List<SilaCulinaryMaterialHistoryDto>> GetMappingHistoryAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string? ingredientTempRef, CancellationToken cancellationToken);

        Task<List<SilaCulinaryMaterialAliasDto>> ListAliasesAsync(
            Guid organizationId, string? status, string? search, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialAliasDto> CreateAliasAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryMaterialAliasCreateDto request, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialAliasDto> ApproveAliasAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid aliasId, bool approve, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialIndexStatusDto> GetIndexStatusAsync(
            Guid organizationId, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialIndexStatusDto> RebuildIndexAsync(
            Guid organizationId, Guid userId, Guid roleId, bool incrementalOnly, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialDashboardDto> GetDashboardAsync(
            Guid organizationId, CancellationToken cancellationToken);

        Task<SilaCulinaryPhase1CHandoverDto> GetPhase1CHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<(byte[] Content, string ContentType, string FileName)> ExportReportAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string format, string? filterStatus, CancellationToken cancellationToken);
    }

    public class SilaCulinaryMaterialService : ISilaCulinaryMaterialService
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

        private readonly IRepositoryWrapper _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIntegrationCredentialProtector _protector;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SilaCulinaryMaterialService(
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

        public static bool CanViewCost(Guid roleId) =>
            SilaAccess.HasFullAccess(roleId);

        public static bool CanManageAliases(Guid roleId) =>
            SilaAccess.IsAdministrator(roleId)
            || roleId == Common.FNB_DIRECTOR_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID
            || roleId == Common.STORE_MANAGER_ROLE_ID;

        public async Task<SilaCulinaryMaterialWorkspaceDto> GetWorkspaceAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);

            List<SilaCulinaryMaterialMapping> existingMappings = await _repository.SilaCulinaryMaterialMapping
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            bool changed = await SyncDraftLinesWithMappingsAsync(
                buyer.Id, draft, payload, existingMappings, userId, cancellationToken);
            if (changed)
            {
                await _repository.SaveAsync();
            }

            return await BuildWorkspaceDtoAsync(buyer, draft, payload, existingMappings, cfg, userId, roleId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialWorkspaceDto> MatchDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialMatchRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialConfigDto matCfg = SilaCulinaryAiSettings.ParseMaterialConfig(cfg.MaterialConfigJson);

            if (payload.Ingredients.Count == 0)
            {
                throw new BadRequestCustomException("No ingredients in draft.", "Add or generate recipe ingredients in Phase 1A before running material matching.");
            }
            if (payload.Ingredients.Count > matCfg.MaxIngredientsPerBulk)
            {
                throw new BadRequestCustomException("Ingredient limit exceeded.", $"Maximum {matCfg.MaxIngredientsPerBulk} ingredients per bulk run.");
            }

            List<SilaCulinaryMaterialMapping> existingMappings = await _repository.SilaCulinaryMaterialMapping
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            bool syncModified = await SyncDraftLinesWithMappingsAsync(buyer.Id, draft, payload, existingMappings, userId, cancellationToken);
            if (syncModified)
            {
                await _repository.SaveAsync();
            }

            HashSet<string>? refFilter = request.IngredientRefs != null && request.IngredientRefs.Count > 0
                ? new HashSet<string>(request.IngredientRefs, StringComparer.OrdinalIgnoreCase)
                : null;

            List<SilaCulinaryMaterialMapping> targets = existingMappings
                .Where(m => refFilter == null || refFilter.Contains(m.IngredientTempRef))
                .Where(m => !request.UnresolvedOnly || !string.Equals(m.MappingStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                .Where(m => request.OverwriteConfirmed || !string.Equals(m.MappingStatus, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (targets.Count == 0)
            {
                return await BuildWorkspaceDtoAsync(buyer, draft, payload, existingMappings, cfg, userId, roleId, cancellationToken);
            }

            Guid? effectivePropertyId = request.PropertyId ?? draft.PropertyId;
            string? effectivePlantCode = await ResolvePlantCodeAsync(buyer.Id, effectivePropertyId, draft.LocationId, cancellationToken);

            await EnsureTenantIndexSyncedAsync(buyer.Id, effectivePropertyId, draft.LocationId, matCfg, cancellationToken);

            List<PythonIngredientQuery> queries = new();
            Dictionary<string, CulinaryIngredientLineDto> lineByRef = payload.Ingredients
                .ToDictionary(i => i.TempRef, StringComparer.OrdinalIgnoreCase);

            foreach (SilaCulinaryMaterialMapping m in targets)
            {
                lineByRef.TryGetValue(m.IngredientTempRef, out CulinaryIngredientLineDto? line);
                queries.Add(new PythonIngredientQuery
                {
                    IngredientTempRef = m.IngredientTempRef,
                    Description = m.OriginalDescription,
                    NormalizedDescription = line?.NormalizedDescription,
                    Quantity = m.RecipeQuantity,
                    RecipeUom = m.RecipeUom,
                    Category = line?.Category,
                    IngredientForm = line?.IngredientForm,
                    BrandPreference = line?.BrandPreference,
                    PropertyId = effectivePropertyId?.ToString(),
                    PlantCode = effectivePlantCode
                });
            }

            PythonLlmSettings? llmSettings = BuildAuthorizedLlmSettings(cfg, request.UseAiDisambiguation);
            PythonMatchBulkRequest pyReq = new()
            {
                TenantId = buyer.Id.ToString(),
                DraftId = draft.Id.ToString(),
                DraftReference = draft.DraftReference,
                PropertyId = effectivePropertyId?.ToString(),
                PlantCode = effectivePlantCode,
                Config = ToPythonConfig(matCfg),
                Llm = llmSettings,
                UseAiDisambiguation = request.UseAiDisambiguation && llmSettings != null,
                Ingredients = queries
            };

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_REQUESTED,
                $"Requested material matching for {queries.Count} ingredient(s) in {draft.DraftReference}.",
                userId);

            PythonMatchBulkResponse pyRes = await PostCulinaryEngineAsync<PythonMatchBulkResponse>(
                "/v1/materials/match-bulk", pyReq, Math.Max(cfg.RequestTimeoutSeconds, 45), cancellationToken);

            Dictionary<string, PythonIngredientMatchResult> resByRef = pyRes.Results
                .ToDictionary(r => r.IngredientTempRef, StringComparer.OrdinalIgnoreCase);

            Dictionary<Guid, ItemBuyerMaster> allMaterials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (SilaCulinaryMaterialMapping m in targets)
            {
                if (!resByRef.TryGetValue(m.IngredientTempRef, out PythonIngredientMatchResult? matched))
                {
                    continue;
                }

                string prevStatus = m.MappingStatus;
                string? prevCode = m.MaterialCode;
                ApplyPythonMatchResult(m, matched, allMaterials, draft, actorName);
                m.MappingRevision += 1;

                _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MappingId = m.Id,
                    DraftId = draft.Id,
                    IngredientTempRef = m.IngredientTempRef,
                    MappingRevision = m.MappingRevision,
                    Action = "MATCH_EXECUTED",
                    PreviousStatus = prevStatus,
                    NewStatus = m.MappingStatus,
                    PreviousMaterialCode = prevCode,
                    NewMaterialCode = m.MaterialCode,
                    Reason = $"Method={m.MatchMethod}; Score={m.MatchScore:0.####}",
                    ActorUserId = userId,
                    ActorName = actorName,
                    IsActive = true
                });

                LogMatchMethodWorkflowEvent(buyer.Id, draft.Id, m, userId);

                if (lineByRef.TryGetValue(m.IngredientTempRef, out CulinaryIngredientLineDto? line))
                {
                    line.MaterialMappingStatus = m.MappingStatus;
                    line.MaterialReference = m.MaterialCode;
                }
            }

            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);
            await _repository.SaveAsync();

            return await BuildWorkspaceDtoAsync(buyer, draft, payload, existingMappings, cfg, userId, roleId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialMappingLineDto> MatchSingleIngredientAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialMatchRequestDto request, CancellationToken cancellationToken)
        {
            request.IngredientRefs = new List<string> { ingredientTempRef };
            SilaCulinaryMaterialWorkspaceDto ws = await MatchDraftAsync(
                organizationId, userId, roleId, actorName, draftId, request, cancellationToken);
            return ws.Lines.FirstOrDefault(l => string.Equals(l.IngredientTempRef, ingredientTempRef, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundCustomException("Ingredient mapping not found.", $"Ingredient {ingredientTempRef} was not found.");
        }

        public async Task<SilaCulinaryMaterialWorkspaceDto> RecalculateAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialRecalcDto request, CancellationToken cancellationToken)
        {
            if (request.Servings <= 0)
            {
                throw new BadRequestCustomException("Invalid servings.", "Servings must be greater than zero.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);

            decimal oldServings = draft.BaseServings > 0 ? draft.BaseServings : 1m;
            decimal ratio = request.Servings / oldServings;

            List<SilaCulinaryMaterialMapping> existingMappings = await _repository.SilaCulinaryMaterialMapping
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (CulinaryIngredientLineDto line in payload.Ingredients)
            {
                line.Quantity = Math.Round(line.Quantity * ratio, 4, MidpointRounding.AwayFromZero);
                if (line.RawQuantity.HasValue)
                {
                    line.RawQuantity = Math.Round(line.RawQuantity.Value * ratio, 4, MidpointRounding.AwayFromZero);
                }
                if (line.EdibleQuantity.HasValue)
                {
                    line.EdibleQuantity = Math.Round(line.EdibleQuantity.Value * ratio, 4, MidpointRounding.AwayFromZero);
                }
            }

            foreach (SilaCulinaryMaterialMapping m in existingMappings)
            {
                CulinaryIngredientLineDto? line = payload.Ingredients.FirstOrDefault(
                    x => string.Equals(x.TempRef, m.IngredientTempRef, StringComparison.OrdinalIgnoreCase));
                decimal newQty = line?.Quantity ?? Math.Round(m.RecipeQuantity * ratio, 4, MidpointRounding.AwayFromZero);
                m.RecipeQuantity = newQty;
                if (m.ConversionFactor.HasValue && m.ConversionFactor.Value > 0)
                {
                    m.ConvertedBaseQuantity = Math.Round(newQty * m.ConversionFactor.Value, 4, MidpointRounding.AwayFromZero);
                }
            }

            draft.BaseServings = request.Servings;
            payload.Header.BaseServings = request.Servings;
            if (!string.IsNullOrWhiteSpace(request.ServingSize)
                && decimal.TryParse(request.ServingSize.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedSize)
                && parsedSize > 0)
            {
                payload.Header.ServingSize = parsedSize;
            }
            if (request.PropertyId.HasValue)
            {
                draft.PropertyId = request.PropertyId;
            }
            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);

            await _repository.SaveAsync();

            return await BuildWorkspaceDtoAsync(buyer, draft, payload, existingMappings, cfg, userId, roleId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialMappingLineDto> ConfirmMappingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialActionDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryMaterialMapping mapping = await LoadMappingLineAsync(buyer.Id, draft.Id, ingredientTempRef);

            EnsureRevisionMatches(mapping, request.ExpectedMappingRevision);

            Guid targetMaterialId = request.MaterialId ?? mapping.MaterialId
                ?? throw new BadRequestCustomException("Material selection required.", "Select an eligible material before confirming.");

            ItemBuyerMaster material = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == targetMaterialId && x.IsActive)
                ?? throw new NotFoundCustomException("Material ineligible or inactive.", "The selected material is no longer active in Material Master.");

            bool plantEligible = await IsMaterialPlantEligibleAsync(buyer.Id, material.Id, draft.PropertyId, draft.LocationId, cancellationToken);
            if (!plantEligible)
            {
                throw new BadRequestCustomException(
                    "Material not extended to plant.",
                    $"Material {material.MaterialCode} is not extended to the selected property/kitchen. Request a material extension instead.");
            }

            List<MaterialUomConversion> conversions = await _repository.MaterialUomConversion
                .FindByCondition(x => x.MaterialId == material.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            var uomCheck = ComputeDeterministicUomConversion(
                mapping.RecipeQuantity, mapping.RecipeUom, material.BaseUnitOfMeasure, conversions);

            if (!uomCheck.Compatible)
            {
                mapping.UomCompatible = false;
                mapping.MappingStatus = "UOM_CONFLICT";
                mapping.MappingRevision += 1;
                await _repository.SaveAsync();
                throw new BadRequestCustomException("UOM conversion conflict.", uomCheck.Reason);
            }

            string prevStatus = mapping.MappingStatus;
            string? prevCode = mapping.MaterialCode;
            bool isAlternative = mapping.MaterialId.HasValue && mapping.MaterialId.Value != material.Id;

            mapping.MaterialId = material.Id;
            mapping.MaterialCode = material.MaterialCode;
            mapping.MaterialDescription = material.Description;
            mapping.BaseUom = material.BaseUnitOfMeasure;
            mapping.ConvertedBaseQuantity = uomCheck.ConvertedQty;
            mapping.ConversionFactor = uomCheck.Factor;
            mapping.UomCompatible = true;
            mapping.SpecificationCompatible = true;
            mapping.MappingStatus = "CONFIRMED";
            mapping.MissingClassification = "NONE";
            mapping.RecommendedRequestType = null;
            mapping.OverrideReason = string.IsNullOrWhiteSpace(request.Reason) ? mapping.OverrideReason : request.Reason.Trim();
            mapping.ConfirmedByUserId = userId;
            mapping.ConfirmedByName = actorName;
            mapping.ConfirmedOnUtc = DateTime.UtcNow;
            mapping.MappingRevision += 1;

            _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MappingId = mapping.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                MappingRevision = mapping.MappingRevision,
                Action = isAlternative ? "ALTERNATIVE_CONFIRMED" : "CHEF_CONFIRMED",
                PreviousStatus = prevStatus,
                NewStatus = "CONFIRMED",
                PreviousMaterialCode = prevCode,
                NewMaterialCode = material.MaterialCode,
                Reason = request.Reason,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            _repository.SilaCulinaryMaterialFeedback.Create(new SilaCulinaryMaterialFeedback
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                IngredientDescription = mapping.OriginalDescription,
                SuggestedMaterialId = mapping.MaterialId,
                SuggestedMaterialCode = prevCode,
                SelectedMaterialId = material.Id,
                SelectedMaterialCode = material.MaterialCode,
                FeedbackType = isAlternative ? "ALTERNATIVE_SELECTED" : "ACCEPTED",
                Notes = request.Reason,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            if (request.SuggestAlias && !string.IsNullOrWhiteSpace(mapping.NormalizedDescription))
            {
                await CreatePendingAliasIfNotDuplicateAsync(
                    buyer.Id, mapping.OriginalDescription, material, draft.PropertyId, userId, actorName, cancellationToken);
            }

            UpdateDraftIngredientMappingRef(payload, mapping);
            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_CONFIRMED,
                $"Confirmed mapping {mapping.IngredientTempRef} -> {material.MaterialCode} ({material.Description}).",
                userId);

            await _repository.SaveAsync();

            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialWorkspaceDto ws = await BuildWorkspaceDtoAsync(
                buyer, draft, payload, new List<SilaCulinaryMaterialMapping> { mapping }, cfg, userId, roleId, cancellationToken);
            return ws.Lines.First();
        }

        public async Task<SilaCulinaryMaterialWorkspaceDto> BulkAcceptExactAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryMaterialBulkAcceptDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);

            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            HashSet<string>? filter = request.IngredientRefs != null && request.IngredientRefs.Count > 0
                ? new HashSet<string>(request.IngredientRefs, StringComparer.OrdinalIgnoreCase)
                : null;

            List<SilaCulinaryMaterialMapping> eligible = mappings.Where(m =>
                (filter == null || filter.Contains(m.IngredientTempRef))
                && (m.MappingStatus == "EXACT_MATCH" || m.MappingStatus == "VERIFIED_ALIAS_MATCH")
                && m.MaterialId.HasValue
                && m.SpecificationCompatible
                && m.UomCompatible).ToList();

            foreach (SilaCulinaryMaterialMapping m in eligible)
            {
                List<SilaCulinaryMaterialCandidateDto> cands = DeserializeCandidates(m.CandidatesJson);
                int exactEligibleCount = cands.Count(c =>
                    c.MaterialActive && c.PlantEligible && c.SpecificationCompatible && c.UomCompatible
                    && (c.ExactScore >= 0.99 || c.AliasScore >= 0.99));
                if (exactEligibleCount > 1)
                {
                    continue;
                }

                ItemBuyerMaster? mat = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == m.MaterialId!.Value && x.IsActive);
                if (mat == null) continue;

                bool plantOk = await IsMaterialPlantEligibleAsync(buyer.Id, mat.Id, draft.PropertyId, draft.LocationId, cancellationToken);
                if (!plantOk) continue;

                string prev = m.MappingStatus;
                m.MappingStatus = "CONFIRMED";
                m.ConfirmedByUserId = userId;
                m.ConfirmedByName = actorName;
                m.ConfirmedOnUtc = DateTime.UtcNow;
                m.MappingRevision += 1;

                _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MappingId = m.Id,
                    DraftId = draft.Id,
                    IngredientTempRef = m.IngredientTempRef,
                    MappingRevision = m.MappingRevision,
                    Action = "BULK_EXACT_CONFIRMED",
                    PreviousStatus = prev,
                    NewStatus = "CONFIRMED",
                    PreviousMaterialCode = m.MaterialCode,
                    NewMaterialCode = m.MaterialCode,
                    Reason = "Bulk accepted unambiguous exact/verified-alias match.",
                    ActorUserId = userId,
                    ActorName = actorName,
                    IsActive = true
                });

                UpdateDraftIngredientMappingRef(payload, m);
            }

            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);
            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_CONFIRMED,
                $"Bulk accepted {eligible.Count} unambiguous exact/alias match(es) on {draft.DraftReference}.",
                userId);

            await _repository.SaveAsync();
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            return await BuildWorkspaceDtoAsync(buyer, draft, payload, mappings, cfg, userId, roleId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialMappingLineDto> RejectMappingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialActionDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryMaterialMapping mapping = await LoadMappingLineAsync(buyer.Id, draft.Id, ingredientTempRef);

            EnsureRevisionMatches(mapping, request.ExpectedMappingRevision);

            string prevStatus = mapping.MappingStatus;
            string? prevCode = mapping.MaterialCode;

            mapping.MappingStatus = "REJECTED";
            mapping.OverrideReason = request.Reason;
            mapping.MappingRevision += 1;

            _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MappingId = mapping.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                MappingRevision = mapping.MappingRevision,
                Action = "CHEF_REJECTED",
                PreviousStatus = prevStatus,
                NewStatus = "REJECTED",
                PreviousMaterialCode = prevCode,
                NewMaterialCode = null,
                Reason = request.Reason,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            _repository.SilaCulinaryMaterialFeedback.Create(new SilaCulinaryMaterialFeedback
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                IngredientDescription = mapping.OriginalDescription,
                SuggestedMaterialId = mapping.MaterialId,
                SuggestedMaterialCode = prevCode,
                FeedbackType = string.IsNullOrWhiteSpace(request.FeedbackType) ? "REJECTED" : request.FeedbackType.Trim().ToUpperInvariant(),
                Notes = request.Reason,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            UpdateDraftIngredientMappingRef(payload, mapping);
            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_REJECTED,
                $"Rejected candidate {prevCode ?? "none"} for {mapping.OriginalDescription}.",
                userId);

            await _repository.SaveAsync();
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialWorkspaceDto ws = await BuildWorkspaceDtoAsync(
                buyer, draft, payload, new List<SilaCulinaryMaterialMapping> { mapping }, cfg, userId, roleId, cancellationToken);
            return ws.Lines.First();
        }

        public async Task<SilaCulinaryMaterialMappingLineDto> PrepareRequestDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinaryMaterialRequestDraftCreateDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryMaterialMapping mapping = await LoadMappingLineAsync(buyer.Id, draft.Id, ingredientTempRef);

            EnsureRevisionMatches(mapping, request.ExpectedMappingRevision);

            // Bounded duplicate check before preparing Phase 1C handover draft
            List<ItemBuyerMaster> allTenantMaterials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .ToListAsync(cancellationToken);

            string normProposed = (request.ProposedDescription ?? mapping.OriginalDescription).Trim().ToLowerInvariant();
            List<string> dupEvidence = new();
            List<string> checkedCodes = new();
            foreach (ItemBuyerMaster mat in allTenantMaterials)
            {
                string matNorm = (mat.Description ?? string.Empty).Trim().ToLowerInvariant();
                if (matNorm.Contains(normProposed, StringComparison.OrdinalIgnoreCase)
                    || normProposed.Contains(matNorm, StringComparison.OrdinalIgnoreCase))
                {
                    checkedCodes.Add(mat.MaterialCode);
                    dupEvidence.Add($"Potential existing match: {mat.MaterialCode} ({mat.Description}, active={mat.IsActive})");
                    if (dupEvidence.Count >= 5) break;
                }
            }

            SilaCulinaryMaterialRequestDraftDto reqDraft = new()
            {
                RequestType = string.IsNullOrWhiteSpace(request.RequestType) ? "NEW_MATERIAL" : request.RequestType.Trim().ToUpperInvariant(),
                ProposedDescription = string.IsNullOrWhiteSpace(request.ProposedDescription) ? mapping.OriginalDescription : request.ProposedDescription.Trim(),
                ProposedLongDescription = request.ProposedLongDescription,
                IngredientCategory = mapping.MaterialDescription,
                ProposedMaterialGroup = request.ProposedMaterialGroup,
                ProposedBaseUom = string.IsNullOrWhiteSpace(request.ProposedBaseUom) ? mapping.RecipeUom : request.ProposedBaseUom.Trim().ToUpperInvariant(),
                ProposedPurchasingUom = request.ProposedPurchasingUom,
                ProposedPackSize = request.ProposedPackSize,
                BrandPreference = request.BrandPreference,
                ProductSpecification = request.ProductSpecification,
                StorageRequirement = request.StorageRequirement,
                PotentialAllergens = request.PotentialAllergens ?? new List<string>(),
                DraftId = draft.Id,
                DraftReference = draft.DraftReference,
                IngredientTempRef = mapping.IngredientTempRef,
                RequiredQuantity = mapping.RecipeQuantity,
                RecipeUom = mapping.RecipeUom,
                PropertyId = draft.PropertyId,
                PlantCode = draft.KitchenName,
                RequestedBy = actorName,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Missing or incompatible material in Material Master (Phase 1B draft for Phase 1C)." : request.Reason.Trim(),
                DuplicateCheckEvidence = dupEvidence,
                CandidateMaterialCodesChecked = checkedCodes,
                IsAiSuggested = false
            };

            string prevStatus = mapping.MappingStatus;
            mapping.MappingStatus = "PENDING_MATERIAL_REQUEST";
            mapping.RecommendedRequestType = reqDraft.RequestType;
            mapping.MissingReason = reqDraft.Reason;
            mapping.RequestDraftJson = JsonSerializer.Serialize(reqDraft, CamelJson);
            mapping.MappingRevision += 1;

            _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MappingId = mapping.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                MappingRevision = mapping.MappingRevision,
                Action = "PHASE1C_REQUEST_DRAFT_PREPARED",
                PreviousStatus = prevStatus,
                NewStatus = "PENDING_MATERIAL_REQUEST",
                PreviousMaterialCode = mapping.MaterialCode,
                Reason = $"{reqDraft.RequestType}: {reqDraft.ProposedDescription}",
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            UpdateDraftIngredientMappingRef(payload, mapping);
            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_REQ_DRAFT,
                $"Prepared Phase 1C {reqDraft.RequestType} request draft for {mapping.OriginalDescription}.",
                userId);

            await _repository.SaveAsync();
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialWorkspaceDto ws = await BuildWorkspaceDtoAsync(
                buyer, draft, payload, new List<SilaCulinaryMaterialMapping> { mapping }, cfg, userId, roleId, cancellationToken);
            return ws.Lines.First();
        }

        public async Task<object> SuggestSubstitutionsAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryMaterialMapping mapping = await LoadMappingLineAsync(buyer.Id, draft.Id, ingredientTempRef);
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialConfigDto matCfg = SilaCulinaryAiSettings.ParseMaterialConfig(cfg.MaterialConfigJson);

            await EnsureTenantIndexSyncedAsync(buyer.Id, draft.PropertyId, draft.LocationId, matCfg, cancellationToken);

            object pyReq = new
            {
                tenant_id = buyer.Id.ToString(),
                ingredient = new PythonIngredientQuery
                {
                    IngredientTempRef = mapping.IngredientTempRef,
                    Description = mapping.OriginalDescription,
                    NormalizedDescription = mapping.NormalizedDescription,
                    Quantity = mapping.RecipeQuantity > 0 ? mapping.RecipeQuantity : 1m,
                    RecipeUom = string.IsNullOrWhiteSpace(mapping.RecipeUom) ? "EA" : mapping.RecipeUom,
                    PropertyId = draft.PropertyId?.ToString(),
                    ExplicitMaterialCode = mapping.MaterialCode
                },
                config = ToPythonConfig(matCfg),
                provider_config = BuildAuthorizedLlmSettings(cfg, false)
            };

            JsonElement pyRes = await PostCulinaryEngineAsync<JsonElement>(
                "/v1/materials/substitutions", pyReq, cfg.RequestTimeoutSeconds, cancellationToken);

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_SUB_SUGGESTED,
                $"Generated substitution review options for {mapping.OriginalDescription}.",
                userId);
            await _repository.SaveAsync();

            return pyRes;
        }

        public async Task<SilaCulinaryMaterialMappingLineDto> ApplySubstitutionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            string ingredientTempRef, SilaCulinarySubstitutionApplyDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryMaterialMapping mapping = await LoadMappingLineAsync(buyer.Id, draft.Id, ingredientTempRef);

            EnsureRevisionMatches(mapping, request.ExpectedMappingRevision);

            ItemBuyerMaster? material = null;
            if (request.ProposedMaterialId.HasValue)
            {
                material = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == request.ProposedMaterialId.Value && x.IsActive);
            }
            else if (!string.IsNullOrWhiteSpace(request.ProposedMaterialCode))
            {
                material = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.MaterialCode == request.ProposedMaterialCode.Trim() && x.IsActive);
            }

            string prevStatus = mapping.MappingStatus;
            string? prevCode = mapping.MaterialCode;
            decimal ratio = request.ReplacementRatio ?? 1m;
            if (ratio <= 0) ratio = 1m;

            mapping.IsSubstitution = true;
            mapping.SubstitutedFromMaterialCode = prevCode;
            mapping.ReplacementRatio = ratio;
            mapping.OverrideReason = request.Reason ?? $"Chef applied substitution: {request.ProposedIngredient}";
            mapping.RecipeQuantity = Math.Round(mapping.RecipeQuantity * ratio, 4, MidpointRounding.AwayFromZero);

            if (material != null)
            {
                mapping.MaterialId = material.Id;
                mapping.MaterialCode = material.MaterialCode;
                mapping.MaterialDescription = material.Description;
                mapping.BaseUom = material.BaseUnitOfMeasure;
                mapping.MappingStatus = "CONFIRMED";
                mapping.ConfirmedByUserId = userId;
                mapping.ConfirmedByName = actorName;
                mapping.ConfirmedOnUtc = DateTime.UtcNow;
            }
            else
            {
                mapping.MaterialId = null;
                mapping.MaterialCode = null;
                mapping.MaterialDescription = request.ProposedIngredient;
                mapping.MappingStatus = "SUGGESTED_MATCH";
            }
            mapping.MappingRevision += 1;

            CulinaryIngredientLineDto? line = payload.Ingredients.FirstOrDefault(
                x => string.Equals(x.TempRef, mapping.IngredientTempRef, StringComparison.OrdinalIgnoreCase));
            if (line != null)
            {
                line.Quantity = mapping.RecipeQuantity;
                line.PreparationNotes = string.IsNullOrWhiteSpace(line.PreparationNotes)
                    ? $"Substituted with {request.ProposedIngredient}"
                    : $"{line.PreparationNotes} | Substituted with {request.ProposedIngredient}";
                line.MaterialMappingStatus = mapping.MappingStatus;
                line.MaterialReference = mapping.MaterialCode;
            }
            draft.PayloadJson = JsonSerializer.Serialize(payload, SnakeJson);

            _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MappingId = mapping.Id,
                DraftId = draft.Id,
                IngredientTempRef = mapping.IngredientTempRef,
                MappingRevision = mapping.MappingRevision,
                Action = "SUBSTITUTION_APPLIED",
                PreviousStatus = prevStatus,
                NewStatus = mapping.MappingStatus,
                PreviousMaterialCode = prevCode,
                NewMaterialCode = mapping.MaterialCode,
                Reason = mapping.OverrideReason,
                ActorUserId = userId,
                ActorName = actorName,
                IsActive = true
            });

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draft.Id,
                Common.SILA_AUDIT_CULINARY_MAT_SUB_ACCEPTED,
                $"Applied substitution on draft {draft.DraftReference}: {mapping.OriginalDescription} -> {request.ProposedIngredient}.",
                userId);

            await _repository.SaveAsync();
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialWorkspaceDto ws = await BuildWorkspaceDtoAsync(
                buyer, draft, payload, new List<SilaCulinaryMaterialMapping> { mapping }, cfg, userId, roleId, cancellationToken);
            return ws.Lines.First();
        }

        public async Task<List<SilaCulinaryMaterialSearchItemDto>> SearchMaterialsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? query, Guid? propertyId, int limit, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            int cappedLimit = Math.Clamp(limit, 1, 50);
            string q = (query ?? string.Empty).Trim();

            IQueryable<ItemBuyerMaster> dbQuery = _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                string pattern = $"%{q}%";
                dbQuery = dbQuery.Where(x =>
                    EF.Functions.Like(x.Description, pattern)
                    || EF.Functions.Like(x.MaterialCode, pattern)
                    || (x.MaterialGroup != null && EF.Functions.Like(x.MaterialGroup, pattern))
                    || (x.Barcode != null && EF.Functions.Like(x.Barcode, pattern)));
            }

            List<ItemBuyerMaster> items = await dbQuery
                .OrderBy(x => x.Description)
                .Take(cappedLimit)
                .ToListAsync(cancellationToken);

            bool canViewCost = CanViewCost(roleId);
            List<Guid> matIds = items.Select(x => x.Id).ToList();
            Dictionary<Guid, SilaCulinaryMaterialStockDto> stocks = await LoadStockByMaterialAsync(
                buyer.Id, userId, roleId, propertyId, null, matIds, cancellationToken);
            Dictionary<Guid, SilaCulinaryMaterialPriceDto> prices = await LoadPriceByMaterialAsync(
                buyer.Id, null, null, items, canViewCost, cancellationToken);
            Dictionary<Guid, bool> plantMap = await LoadPlantEligibilityMapAsync(
                buyer.Id, propertyId, null, matIds, cancellationToken);

            return items.Select(m => new SilaCulinaryMaterialSearchItemDto
            {
                MaterialId = m.Id,
                MaterialCode = m.MaterialCode,
                Description = m.Description,
                MaterialGroup = m.MaterialGroup,
                ProductType = m.ProductType,
                BaseUom = m.BaseUnitOfMeasure,
                OrderUom = m.OrderUnitOfMeasure,
                AlternateUom = m.AlternateUnitOfMeasure,
                Barcode = m.Barcode,
                ImageUrl = null,
                IsActive = m.IsActive,
                PlantEligible = plantMap.GetValueOrDefault(m.Id, true),
                Stock = stocks.GetValueOrDefault(m.Id),
                Price = prices.GetValueOrDefault(m.Id)
            }).ToList();
        }

        public async Task<List<SilaCulinaryMaterialHistoryDto>> GetMappingHistoryAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string? ingredientTempRef, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            _ = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);

            IQueryable<SilaCulinaryMaterialMappingHistory> q = _repository.SilaCulinaryMaterialMappingHistory
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draftId && x.IsActive);

            if (!string.IsNullOrWhiteSpace(ingredientTempRef))
            {
                q = q.Where(x => x.IngredientTempRef == ingredientTempRef.Trim());
            }

            List<SilaCulinaryMaterialMappingHistory> rows = await q
                .OrderByDescending(x => x.DateCreated)
                .Take(200)
                .ToListAsync(cancellationToken);

            return rows.Select(h => new SilaCulinaryMaterialHistoryDto
            {
                Id = h.Id,
                IngredientTempRef = h.IngredientTempRef,
                MappingRevision = h.MappingRevision,
                Action = h.Action,
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                PreviousMaterialCode = h.PreviousMaterialCode,
                NewMaterialCode = h.NewMaterialCode,
                Reason = h.Reason,
                ActorName = h.ActorName,
                CreatedOn = h.DateCreated
            }).ToList();
        }

        public async Task<List<SilaCulinaryMaterialAliasDto>> ListAliasesAsync(
            Guid organizationId, string? status, string? search, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            IQueryable<SilaCulinaryMaterialAlias> q = _repository.SilaCulinaryMaterialAlias
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (!string.IsNullOrWhiteSpace(status))
            {
                string s = status.Trim().ToUpperInvariant();
                q = q.Where(x => x.ApprovalStatus == s);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                string pat = $"%{search.Trim()}%";
                q = q.Where(x => EF.Functions.Like(x.AliasText, pat) || EF.Functions.Like(x.MaterialCode, pat));
            }

            List<SilaCulinaryMaterialAlias> rows = await q.OrderByDescending(x => x.DateUpdated).Take(200).ToListAsync(cancellationToken);
            List<Guid> matIds = rows.Select(r => r.MaterialId).Distinct().ToList();
            Dictionary<Guid, string> descById = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

            return rows.Select(r => MapAliasDto(r, descById.GetValueOrDefault(r.MaterialId))).ToList();
        }

        public async Task<SilaCulinaryMaterialAliasDto> CreateAliasAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryMaterialAliasCreateDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AliasText))
            {
                throw new BadRequestCustomException("Alias text required.", "Provide an alias phrase.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            ItemBuyerMaster material = await _repository.ItemBuyerMaster
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == request.MaterialId && x.IsActive)
                ?? throw new NotFoundCustomException("Material not found.", "Select an active material.");

            string normalized = NormalizeBasic(request.AliasText);
            SilaCulinaryMaterialAlias? conflict = await _repository.SilaCulinaryMaterialAlias
                .FindFirstByConditionAsync(x =>
                    x.BuyerId == buyer.Id
                    && x.IsActive
                    && x.NormalizedAlias == normalized
                    && x.PropertyId == request.PropertyId
                    && x.ApprovalStatus == "APPROVED");

            if (conflict != null && conflict.MaterialId != material.Id)
            {
                throw new BadRequestCustomException(
                    "Alias conflict.",
                    $"Alias '{request.AliasText}' is already approved for material {conflict.MaterialCode}.");
            }

            bool autoApprove = CanManageAliases(roleId);
            SilaCulinaryMaterialAlias alias = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                AliasText = request.AliasText.Trim(),
                NormalizedAlias = normalized,
                MaterialId = material.Id,
                MaterialCode = material.MaterialCode,
                PropertyId = request.PropertyId,
                Language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim().ToLowerInvariant(),
                Source = autoApprove ? "AUTHORIZED_ADMIN" : "CHEF_SUBMITTED",
                ApprovalStatus = autoApprove ? "APPROVED" : "PENDING",
                Revision = 1,
                CreatedByUserId = userId,
                CreatedByName = actorName,
                ApprovedByUserId = autoApprove ? userId : null,
                ApprovedByName = autoApprove ? actorName : null,
                ApprovedOnUtc = autoApprove ? DateTime.UtcNow : null,
                EffectiveFromUtc = DateTime.UtcNow,
                IsActive = true
            };

            _repository.SilaCulinaryMaterialAlias.Create(alias);
            await _repository.SaveAsync();
            return MapAliasDto(alias, material.Description);
        }

        public async Task<SilaCulinaryMaterialAliasDto> ApproveAliasAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid aliasId, bool approve, CancellationToken cancellationToken)
        {
            if (!CanManageAliases(roleId))
            {
                throw new ForBiddenCustomException("Approval restricted.", "Only authorized culinary or master-data administrators can approve or deactivate aliases.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialAlias alias = await _repository.SilaCulinaryMaterialAlias
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == aliasId && x.IsActive)
                ?? throw new NotFoundCustomException("Alias not found.", "Alias record does not exist.");

            alias.ApprovalStatus = approve ? "APPROVED" : "INACTIVE";
            alias.ApprovedByUserId = userId;
            alias.ApprovedByName = actorName;
            alias.ApprovedOnUtc = DateTime.UtcNow;
            alias.Revision += 1;
            await _repository.SaveAsync();

            ItemBuyerMaster? mat = await _repository.ItemBuyerMaster.FindFirstByConditionAsync(x => x.Id == alias.MaterialId);
            return MapAliasDto(alias, mat?.Description);
        }

        public async Task<SilaCulinaryMaterialIndexStatusDto> GetIndexStatusAsync(
            Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialIndexState? state = await _repository.SilaCulinaryMaterialIndexState
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IsActive);

            int activeCount = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .CountAsync(cancellationToken);

            if (state == null)
            {
                return new SilaCulinaryMaterialIndexStatusDto
                {
                    BuyerId = buyer.Id,
                    Status = "EMPTY",
                    ActiveMaterialCount = activeCount
                };
            }

            return new SilaCulinaryMaterialIndexStatusDto
            {
                BuyerId = buyer.Id,
                Status = state.Status,
                IndexedMaterialCount = state.IndexedMaterialCount,
                ActiveMaterialCount = activeCount,
                PendingUpdatesCount = Math.Max(0, activeCount - state.IndexedMaterialCount),
                EmbeddingModelVersion = state.EmbeddingModelVersion,
                RerankingModelVersion = state.RerankingModelVersion,
                SemanticBackend = state.SemanticBackend,
                BuildDurationMs = state.BuildDurationMs,
                LastBuildCompletedUtc = state.LastBuildCompletedUtc,
                LastIncrementalSyncUtc = state.LastIncrementalSyncUtc,
                LastError = state.LastError
            };
        }

        public async Task<SilaCulinaryMaterialIndexStatusDto> RebuildIndexAsync(
            Guid organizationId, Guid userId, Guid roleId, bool incrementalOnly, CancellationToken cancellationToken)
        {
            if (!SilaAccess.IsAdministrator(roleId) && roleId != Common.COST_CONTROLLER_ROLE_ID && roleId != Common.FNB_DIRECTOR_ROLE_ID)
            {
                throw new ForBiddenCustomException("Index administration restricted.", "Only authorized administrators can rebuild or sync the material index.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration cfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryMaterialConfigDto matCfg = SilaCulinaryAiSettings.ParseMaterialConfig(cfg.MaterialConfigJson);

            SilaCulinaryMaterialIndexState? state = await _repository.SilaCulinaryMaterialIndexState
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IsActive);
            bool isNew = state == null;
            state ??= new SilaCulinaryMaterialIndexState
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                IsActive = true
            };

            state.Status = "BUILDING";
            state.LastBuildStartedUtc = DateTime.UtcNow;
            if (isNew) _repository.SilaCulinaryMaterialIndexState.Create(state);

            AddWorkflowEvent(
                buyer.Id,
                Common.SILA_REF_CULINARY_MATERIAL_INDEX,
                state.Id,
                Common.SILA_AUDIT_CULINARY_MAT_INDEX_START,
                $"Started {(incrementalOnly ? "incremental sync" : "full rebuild")} of tenant material index.",
                userId);
            await _repository.SaveAsync();

            try
            {
                PythonIndexStatus pyStatus = await SyncOrBuildPythonIndexAsync(
                    buyer.Id, null, null, matCfg, forceRebuild: !incrementalOnly, cancellationToken);

                state.Status = "READY";
                state.IndexedMaterialCount = pyStatus.MaterialCount;
                state.ActiveMaterialCount = pyStatus.MaterialCount;
                state.PendingUpdatesCount = 0;
                state.EmbeddingModelVersion = pyStatus.EmbeddingModel;
                state.RerankingModelVersion = matCfg.RerankingModel;
                state.SemanticBackend = pyStatus.SemanticBackend;
                state.BuildDurationMs = pyStatus.BuildDurationMs;
                state.LastBuildCompletedUtc = DateTime.UtcNow;
                state.LastIncrementalSyncUtc = DateTime.UtcNow;
                state.LastError = null;

                AddWorkflowEvent(
                    buyer.Id,
                    Common.SILA_REF_CULINARY_MATERIAL_INDEX,
                    state.Id,
                    Common.SILA_AUDIT_CULINARY_MAT_INDEX_DONE,
                    $"Completed index {(incrementalOnly ? "sync" : "rebuild")}: {pyStatus.MaterialCount} materials in {pyStatus.BuildDurationMs:0.#} ms ({pyStatus.SemanticBackend}).",
                    userId);
                await _repository.SaveAsync();
            }
            catch (Exception ex)
            {
                state.Status = "FAILED";
                state.LastError = ex.Message.Length > 950 ? ex.Message[..950] : ex.Message;
                AddWorkflowEvent(
                    buyer.Id,
                    Common.SILA_REF_CULINARY_MATERIAL_INDEX,
                    state.Id,
                    Common.SILA_AUDIT_CULINARY_MAT_INDEX_FAIL,
                    $"Index build failed: {state.LastError}",
                    userId);
                await _repository.SaveAsync();
                throw;
            }

            return await GetIndexStatusAsync(organizationId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialDashboardDto> GetDashboardAsync(
            Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            int total = mappings.Count;
            int exact = mappings.Count(x => x.MatchMethod == "EXACT_CODE" || x.MatchMethod == "EXACT_DESCRIPTION" || x.MatchMethod == "BARCODE");
            int alias = mappings.Count(x => x.MatchMethod == "VERIFIED_ALIAS" || x.MatchMethod == "ALIAS");
            int fuzzy = mappings.Count(x => x.MatchMethod == "FUZZY");
            int semantic = mappings.Count(x => x.MatchMethod == "SEMANTIC" || x.MatchMethod == "RERANKED");
            int ai = mappings.Count(x => x.MatchMethod == "AI_ASSISTED");

            int confirmed = mappings.Count(x => x.MappingStatus == "CONFIRMED");
            int rejected = mappings.Count(x => x.MappingStatus == "REJECTED");
            int missing = mappings.Count(x => x.MappingStatus == "UNMATCHED" || x.MappingStatus == "PENDING_MATERIAL_REQUEST");
            int uomConf = mappings.Count(x => !x.UomCompatible || x.MappingStatus == "UOM_CONFLICT");
            int pending = mappings.Count(x => x.MappingStatus != "CONFIRMED" && x.MappingStatus != "REJECTED" && x.MappingStatus != "UNMATCHED" && x.MappingStatus != "PENDING_MATERIAL_REQUEST");

            int activeAliases = await _repository.SilaCulinaryMaterialAlias
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ApprovalStatus == "APPROVED")
                .CountAsync(cancellationToken);

            SilaCulinaryMaterialIndexStatusDto indexHealth = await GetIndexStatusAsync(organizationId, cancellationToken);

            double localPct = total > 0 ? Math.Round(((double)(total - ai) / total) * 100.0, 1) : 100.0;
            double extPct = total > 0 ? Math.Round(((double)ai / total) * 100.0, 1) : 0.0;

            return new SilaCulinaryMaterialDashboardDto
            {
                IsRealTelemetry = true,
                TotalIngredientsProcessed = total,
                ExactMatches = exact,
                AliasMatches = alias,
                FuzzyMatches = fuzzy,
                SemanticMatches = semantic,
                AiAssistedMatches = ai,
                ConfirmedMappings = confirmed,
                RejectedCandidates = rejected,
                PendingReview = pending,
                MissingMaterials = missing,
                UomConflicts = uomConf,
                ActiveAliases = activeAliases,
                LocalMatchingUtilizationPct = localPct,
                ExternalAiUtilizationPct = extPct,
                IndexHealth = indexHealth,
                MethodDistribution = new List<SilaCulinaryChartPointDto>
                {
                    new() { Label = "Exact", Count = exact },
                    new() { Label = "Alias", Count = alias },
                    new() { Label = "Fuzzy", Count = fuzzy },
                    new() { Label = "Semantic/Reranked", Count = semantic },
                    new() { Label = "AI-Assisted", Count = ai },
                },
                StatusDistribution = new List<SilaCulinaryChartPointDto>
                {
                    new() { Label = "Confirmed", Count = confirmed },
                    new() { Label = "Pending Review", Count = pending },
                    new() { Label = "UOM Conflict", Count = uomConf },
                    new() { Label = "Missing / Request", Count = missing },
                    new() { Label = "Rejected", Count = rejected },
                },
                UnresolvedIngredients = mappings
                    .Where(x => x.MappingStatus == "UNMATCHED" || x.MappingStatus == "PENDING_MATERIAL_REQUEST")
                    .GroupBy(x => x.OriginalDescription)
                    .Select(g => new SilaCulinaryChartPointDto { Label = g.Key, Count = g.Count() })
                    .OrderByDescending(p => p.Count)
                    .Take(10)
                    .ToList()
            };
        }

        public async Task<SilaCulinaryPhase1CHandoverDto> GetPhase1CHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto ws = await GetWorkspaceAsync(organizationId, userId, roleId, draftId, cancellationToken);
            List<string> issues = new();
            List<SilaCulinaryMaterialRequestDraftDto> reqDrafts = new();

            foreach (SilaCulinaryMaterialMappingLineDto line in ws.Lines)
            {
                if (line.MappingStatus != "CONFIRMED")
                {
                    issues.Add($"{line.OriginalDescription}: status is {line.MappingStatus}");
                }
                if (!line.UomCompatible)
                {
                    issues.Add($"{line.OriginalDescription}: UOM conflict unresolved");
                }
                if (!line.SpecificationCompatible)
                {
                    issues.Add($"{line.OriginalDescription}: specification conflict unresolved");
                }
                if (line.RequestDraft != null)
                {
                    reqDrafts.Add(line.RequestDraft);
                }
            }

            return new SilaCulinaryPhase1CHandoverDto
            {
                DraftId = ws.DraftId,
                DraftReference = ws.DraftReference,
                DraftRevision = ws.DraftRevision,
                RecipeName = ws.RecipeName,
                Servings = ws.Servings,
                ServingSize = ws.ServingSize,
                ReadinessStatus = ws.ReadinessStatus,
                ReadyForPhase1C = ws.Lines.Count > 0 && issues.Count == 0,
                OutstandingValidationIssues = issues,
                IngredientLines = ws.Lines,
                MaterialRequestDrafts = reqDrafts
            };
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportReportAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string format, string? filterStatus, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto ws = await GetWorkspaceAsync(organizationId, userId, roleId, draftId, cancellationToken);
            IEnumerable<SilaCulinaryMaterialMappingLineDto> lines = ws.Lines;
            if (!string.IsNullOrWhiteSpace(filterStatus) && !string.Equals(filterStatus, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                string f = filterStatus.Trim().ToUpperInvariant();
                lines = lines.Where(l => string.Equals(l.MappingStatus, f, StringComparison.OrdinalIgnoreCase));
            }
            List<SilaCulinaryMaterialMappingLineDto> filtered = lines.ToList();

            string fmt = (format ?? "XLSX").Trim().ToUpperInvariant();
            if (fmt == "CSV")
            {
                StringBuilder sb = new();
                sb.AppendLine("Recipe,DraftRef,Ingredient,Normalized,Qty,RecipeUOM,MaterialCode,MaterialDescription,BaseUOM,ConvertedQty,MatchMethod,Status,AvailableStock,Price,PriceSource,MissingReason");
                foreach (SilaCulinaryMaterialMappingLineDto l in filtered)
                {
                    sb.AppendLine(string.Join(",",
                        CsvCell(ws.RecipeName),
                        CsvCell(ws.DraftReference),
                        CsvCell(l.OriginalDescription),
                        CsvCell(l.NormalizedDescription),
                        l.RequiredQuantity.ToString(CultureInfo.InvariantCulture),
                        CsvCell(l.RecipeUom),
                        CsvCell(l.MaterialCode),
                        CsvCell(l.MaterialDescription),
                        CsvCell(l.BaseUom),
                        l.ConvertedQuantity?.ToString(CultureInfo.InvariantCulture) ?? "",
                        CsvCell(l.MatchMethod),
                        CsvCell(l.MappingStatus),
                        l.AvailableStock?.ToString(CultureInfo.InvariantCulture) ?? "Unknown",
                        ws.CanViewCost && l.UnitPrice.HasValue ? $"{l.UnitPrice:0.00} {l.Currency}" : (ws.CanViewCost ? "Missing" : "Restricted"),
                        CsvCell(ws.CanViewCost ? l.PriceSource : "Restricted"),
                        CsvCell(l.MissingReason)));
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"{ws.DraftReference}-material-report.csv");
            }

            if (fmt == "PDF")
            {
                byte[] pdf = BuildSimplePdfReport(ws, filtered);
                return (pdf, "application/pdf", $"{ws.DraftReference}-material-report.pdf");
            }

            using XLWorkbook wb = new();
            IXLWorksheet sheet = wb.Worksheets.Add("Material Mapping Report");
            string[] headers =
            {
                "Recipe", "Draft Reference", "Property", "Ingredient", "Normalized Ingredient",
                "Required Qty", "Recipe UOM", "Material Code", "Material Description", "Base UOM",
                "Converted Qty", "Match Method", "Status", "Confirmed By", "Available Stock",
                "Price", "Price Source", "Missing / Conflict Reason"
            };
            for (int c = 0; c < headers.Length; c++)
            {
                IXLCell cell = sheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
            }
            for (int i = 0; i < filtered.Count; i++)
            {
                SilaCulinaryMaterialMappingLineDto l = filtered[i];
                int r = i + 2;
                sheet.Cell(r, 1).Value = SafeSpreadsheetCell(ws.RecipeName);
                sheet.Cell(r, 2).Value = SafeSpreadsheetCell(ws.DraftReference);
                sheet.Cell(r, 3).Value = SafeSpreadsheetCell(ws.PropertyName ?? ws.KitchenName ?? "");
                sheet.Cell(r, 4).Value = SafeSpreadsheetCell(l.OriginalDescription);
                sheet.Cell(r, 5).Value = SafeSpreadsheetCell(l.NormalizedDescription);
                sheet.Cell(r, 6).Value = (double)l.RequiredQuantity;
                sheet.Cell(r, 7).Value = SafeSpreadsheetCell(l.RecipeUom);
                sheet.Cell(r, 8).Value = SafeSpreadsheetCell(l.MaterialCode ?? "");
                sheet.Cell(r, 9).Value = SafeSpreadsheetCell(l.MaterialDescription ?? "");
                sheet.Cell(r, 10).Value = SafeSpreadsheetCell(l.BaseUom ?? "");
                sheet.Cell(r, 11).Value = l.ConvertedQuantity.HasValue ? (double)l.ConvertedQuantity.Value : "";
                sheet.Cell(r, 12).Value = SafeSpreadsheetCell(l.MatchMethod);
                sheet.Cell(r, 13).Value = SafeSpreadsheetCell(l.MappingStatus);
                sheet.Cell(r, 14).Value = SafeSpreadsheetCell(l.ConfirmedByName ?? "");
                sheet.Cell(r, 15).Value = l.AvailableStock.HasValue ? (double)l.AvailableStock.Value : "Unknown";
                sheet.Cell(r, 16).Value = ws.CanViewCost && l.UnitPrice.HasValue ? $"{l.UnitPrice:0.00} {l.Currency}" : (ws.CanViewCost ? "Missing" : "Restricted");
                sheet.Cell(r, 17).Value = ws.CanViewCost ? SafeSpreadsheetCell(l.PriceSource ?? "") : "Restricted";
                sheet.Cell(r, 18).Value = SafeSpreadsheetCell(l.MissingReason ?? string.Join("; ", l.Warnings));
            }
            sheet.Columns().AdjustToContents();
            using MemoryStream ms = new();
            wb.SaveAs(ms);
            return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{ws.DraftReference}-material-report.xlsx");
        }

        // -------------------------------------------------------------------------
        // Internal Helpers
        // -------------------------------------------------------------------------

        private async Task<SilaCulinaryRecipeDraft> LoadAuthorizedDraftAsync(
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

        private async Task<SilaCulinaryAiConfiguration> LoadAiConfigAsync(Guid buyerId)
        {
            return await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive)
                ?? SilaCulinaryAiSettings.CreateDefault(buyerId);
        }

        private static CulinaryStructuredRecipePayloadDto ParseDraftPayload(SilaCulinaryRecipeDraft draft)
        {
            if (string.IsNullOrWhiteSpace(draft.PayloadJson))
            {
                return new CulinaryStructuredRecipePayloadDto();
            }
            return JsonSerializer.Deserialize<CulinaryStructuredRecipePayloadDto>(draft.PayloadJson, SnakeJson)
                ?? new CulinaryStructuredRecipePayloadDto();
        }

        private async Task<bool> SyncDraftLinesWithMappingsAsync(
            Guid buyerId,
            SilaCulinaryRecipeDraft draft,
            CulinaryStructuredRecipePayloadDto payload,
            List<SilaCulinaryMaterialMapping> existingMappings,
            Guid userId,
            CancellationToken cancellationToken)
        {
            bool modified = false;
            Dictionary<string, SilaCulinaryMaterialMapping> byRef = existingMappings
                .ToDictionary(m => m.IngredientTempRef, StringComparer.OrdinalIgnoreCase);

            foreach (CulinaryIngredientLineDto line in payload.Ingredients)
            {
                if (!byRef.TryGetValue(line.TempRef, out SilaCulinaryMaterialMapping? mapping))
                {
                    mapping = new SilaCulinaryMaterialMapping
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        DraftId = draft.Id,
                        DraftRevision = draft.RevisionNumber,
                        IngredientTempRef = line.TempRef,
                        OriginalDescription = line.Description ?? string.Empty,
                        NormalizedDescription = string.IsNullOrWhiteSpace(line.NormalizedDescription)
                            ? NormalizeBasic(line.Description ?? string.Empty)
                            : line.NormalizedDescription,
                        RecipeQuantity = line.Quantity,
                        RecipeUom = string.IsNullOrWhiteSpace(line.RecipeUom) ? "EA" : line.RecipeUom.Trim().ToUpperInvariant(),
                        MappingStatus = "UNMATCHED",
                        IsActive = true
                    };
                    _repository.SilaCulinaryMaterialMapping.Create(mapping);
                    existingMappings.Add(mapping);
                    byRef[line.TempRef] = mapping;
                    modified = true;
                }
                else
                {
                    // §36 Invalidation: identity description changed materially -> invalidate confirmed mapping
                    string currDesc = (line.Description ?? string.Empty).Trim();
                    if (!string.Equals(mapping.OriginalDescription.Trim(), currDesc, StringComparison.OrdinalIgnoreCase))
                    {
                        string prevStatus = mapping.MappingStatus;
                        mapping.OriginalDescription = currDesc;
                        mapping.NormalizedDescription = NormalizeBasic(currDesc);
                        if (prevStatus == "CONFIRMED")
                        {
                            mapping.MappingStatus = "SUGGESTED_MATCH";
                            mapping.ConfirmedByUserId = null;
                            mapping.ConfirmedByName = null;
                            mapping.ConfirmedOnUtc = null;
                            mapping.MappingRevision += 1;
                            _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
                            {
                                Id = Guid.NewGuid(),
                                BuyerId = buyerId,
                                MappingId = mapping.Id,
                                DraftId = draft.Id,
                                IngredientTempRef = mapping.IngredientTempRef,
                                MappingRevision = mapping.MappingRevision,
                                Action = "INVALIDATED_IDENTITY_CHANGE",
                                PreviousStatus = prevStatus,
                                NewStatus = mapping.MappingStatus,
                                PreviousMaterialCode = mapping.MaterialCode,
                                Reason = "Ingredient description changed in recipe draft; chef revalidation required.",
                                ActorUserId = userId,
                                ActorName = "System",
                                IsActive = true
                            });
                        }
                        modified = true;
                    }
                    else if (mapping.RecipeQuantity != line.Quantity || !string.Equals(mapping.RecipeUom, line.RecipeUom, StringComparison.OrdinalIgnoreCase))
                    {
                        mapping.RecipeQuantity = line.Quantity;
                        mapping.RecipeUom = string.IsNullOrWhiteSpace(line.RecipeUom) ? "EA" : line.RecipeUom.Trim().ToUpperInvariant();
                        if (mapping.ConversionFactor.HasValue && mapping.ConversionFactor.Value > 0)
                        {
                            mapping.ConvertedBaseQuantity = Math.Round(mapping.RecipeQuantity * mapping.ConversionFactor.Value, 4, MidpointRounding.AwayFromZero);
                        }
                        modified = true;
                    }
                }
            }

            await Task.CompletedTask;
            return modified;
        }

        private async Task<SilaCulinaryMaterialWorkspaceDto> BuildWorkspaceDtoAsync(
            BuyerBusinessProfile buyer,
            SilaCulinaryRecipeDraft draft,
            CulinaryStructuredRecipePayloadDto payload,
            List<SilaCulinaryMaterialMapping> mappings,
            SilaCulinaryAiConfiguration cfg,
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            bool canViewCost = CanViewCost(roleId);
            List<Guid> mappedMatIds = mappings
                .Where(m => m.MaterialId.HasValue)
                .Select(m => m.MaterialId!.Value)
                .Distinct()
                .ToList();

            List<SilaCulinaryMaterialCandidateDto> allParsedCands = new();
            foreach (SilaCulinaryMaterialMapping m in mappings)
            {
                allParsedCands.AddRange(DeserializeCandidates(m.CandidatesJson));
            }
            List<Guid> candMatIds = allParsedCands.Select(c => c.MaterialId).Where(id => id != Guid.Empty).Distinct().ToList();
            List<Guid> allMatIds = mappedMatIds.Concat(candMatIds).Distinct().ToList();

            Dictionary<Guid, ItemBuyerMaster> matById = allMatIds.Count > 0
                ? await _repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && allMatIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, ItemBuyerMaster>();

            Dictionary<Guid, SilaCulinaryMaterialStockDto> stockByMat = await LoadStockByMaterialAsync(
                buyer.Id, userId, roleId, draft.PropertyId, draft.LocationId, allMatIds, cancellationToken);

            Dictionary<Guid, SilaCulinaryMaterialPriceDto> priceByMat = await LoadPriceByMaterialAsync(
                buyer.Id, draft.OutletId, draft.LocationId, matById.Values.ToList(), canViewCost, cancellationToken);

            string? plantCode = await ResolvePlantCodeAsync(buyer.Id, draft.PropertyId, draft.LocationId, cancellationToken);

            List<SilaCulinaryMaterialMappingLineDto> lines = new();
            int stockShortages = 0;
            int missingPrices = 0;

            foreach (SilaCulinaryMaterialMapping m in mappings.OrderBy(x => x.IngredientTempRef))
            {
                List<SilaCulinaryMaterialCandidateDto> cands = DeserializeCandidates(m.CandidatesJson);
                foreach (SilaCulinaryMaterialCandidateDto c in cands)
                {
                    if (stockByMat.TryGetValue(c.MaterialId, out var cStock)) c.Stock = cStock;
                    if (priceByMat.TryGetValue(c.MaterialId, out var cPrice)) c.Price = cPrice;
                }

                ItemBuyerMaster? mat = m.MaterialId.HasValue && matById.TryGetValue(m.MaterialId.Value, out var foundMat) ? foundMat : null;
                SilaCulinaryMaterialStockDto? stock = m.MaterialId.HasValue && stockByMat.TryGetValue(m.MaterialId.Value, out var foundStock) ? foundStock : null;
                SilaCulinaryMaterialPriceDto? price = m.MaterialId.HasValue && priceByMat.TryGetValue(m.MaterialId.Value, out var foundPrice) ? foundPrice : null;

                SilaCulinaryMaterialCandidateDto? topCand = cands.FirstOrDefault(c => m.MaterialId.HasValue && c.MaterialId == m.MaterialId.Value)
                    ?? cands.FirstOrDefault();

                decimal? shortage = null;
                if (stock?.AvailableQty != null && m.ConvertedBaseQuantity.HasValue)
                {
                    shortage = Math.Max(0m, m.ConvertedBaseQuantity.Value - stock.AvailableQty.Value);
                    if (shortage > 0) stockShortages += 1;
                }

                if (price == null || price.Status == "MISSING")
                {
                    missingPrices += 1;
                }

                decimal? extCost = null;
                if (canViewCost && price?.Price != null && m.ConvertedBaseQuantity.HasValue)
                {
                    extCost = Math.Round(price.Price.Value * m.ConvertedBaseQuantity.Value, 2, MidpointRounding.AwayFromZero);
                }

                List<string> evidence = string.IsNullOrWhiteSpace(m.EvidenceJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(m.EvidenceJson, CamelJson) ?? new List<string>();
                List<string> warnings = topCand?.Warnings ?? new List<string>();

                lines.Add(new SilaCulinaryMaterialMappingLineDto
                {
                    MappingId = m.Id,
                    MappingRevision = m.MappingRevision,
                    IngredientTempRef = m.IngredientTempRef,
                    OriginalDescription = m.OriginalDescription,
                    NormalizedDescription = m.NormalizedDescription,
                    IngredientImageUrl = null,
                    RequiredQuantity = m.RecipeQuantity,
                    RecipeUom = m.RecipeUom,
                    MaterialId = m.MaterialId,
                    MaterialCode = m.MaterialCode,
                    MaterialDescription = m.MaterialDescription ?? mat?.Description,
                    MaterialImageUrl = null,
                    BaseUom = m.BaseUom ?? mat?.BaseUnitOfMeasure,
                    PurchasingUom = mat?.OrderUnitOfMeasure,
                    ConvertedQuantity = m.ConvertedBaseQuantity,
                    ConversionFactor = m.ConversionFactor,
                    IndicativePacksNeeded = topCand?.IndicativePacksNeeded,
                    IndicativeWholePacks = topCand?.IndicativeWholePacks,
                    ExcessAfterPackRounding = topCand?.ExcessAfterPackRounding,
                    AvailableStock = stock?.AvailableQty,
                    OnHandStock = stock?.OnHandQty,
                    ReservedStock = stock?.ReservedQty,
                    StockUom = stock?.InventoryUom ?? m.BaseUom,
                    StockStatus = stock?.Status ?? "UNKNOWN",
                    IndicativeShortage = shortage,
                    UnitPrice = canViewCost ? price?.Price : null,
                    Currency = canViewCost ? price?.Currency : null,
                    PriceUom = canViewCost ? price?.PriceUom : null,
                    PriceSource = canViewCost ? price?.PriceSource : "RESTRICTED",
                    ExtendedLineCost = extCost,
                    PriceStatus = canViewCost ? (price?.Status ?? "MISSING") : "RESTRICTED",
                    MatchMethod = m.MatchMethod,
                    MatchScore = m.MatchScore,
                    MappingStatus = m.MappingStatus,
                    MissingClassification = m.MissingClassification,
                    RecommendedRequestType = m.RecommendedRequestType,
                    SpecificationCompatible = m.SpecificationCompatible,
                    UomCompatible = m.UomCompatible,
                    RequiresReview = m.MappingStatus != "CONFIRMED",
                    IsSubstitution = m.IsSubstitution,
                    SubstitutedFromMaterialCode = m.SubstitutedFromMaterialCode,
                    ReplacementRatio = m.ReplacementRatio,
                    OverrideReason = m.OverrideReason,
                    MissingReason = m.MissingReason,
                    ConfirmedByName = m.ConfirmedByName,
                    ConfirmedOnUtc = m.ConfirmedOnUtc,
                    LastMatchedOnUtc = m.LastMatchedOnUtc,
                    Evidence = evidence,
                    Warnings = warnings,
                    Candidates = cands,
                    RequestDraft = string.IsNullOrWhiteSpace(m.RequestDraftJson)
                        ? null
                        : JsonSerializer.Deserialize<SilaCulinaryMaterialRequestDraftDto>(m.RequestDraftJson, CamelJson)
                });
            }

            SilaCulinaryMaterialKpisDto kpis = new()
            {
                TotalIngredients = lines.Count,
                ExactMatches = lines.Count(l => l.MatchMethod == "EXACT_CODE" || l.MatchMethod == "EXACT_DESCRIPTION" || l.MatchMethod == "BARCODE"),
                AliasMatches = lines.Count(l => l.MatchMethod == "VERIFIED_ALIAS" || l.MatchMethod == "ALIAS"),
                SuggestedMatches = lines.Count(l => l.MappingStatus == "SUGGESTED_MATCH" || l.MappingStatus == "MULTIPLE_CANDIDATES"),
                ConfirmedMappings = lines.Count(l => l.MappingStatus == "CONFIRMED"),
                PendingReview = lines.Count(l => l.MappingStatus != "CONFIRMED" && l.MappingStatus != "REJECTED" && l.MappingStatus != "UNMATCHED" && l.MappingStatus != "PENDING_MATERIAL_REQUEST"),
                MissingMaterials = lines.Count(l => l.MappingStatus == "UNMATCHED" || l.MappingStatus == "PENDING_MATERIAL_REQUEST"),
                UomConflicts = lines.Count(l => !l.UomCompatible || l.MappingStatus == "UOM_CONFLICT"),
                SpecificationConflicts = lines.Count(l => !l.SpecificationCompatible || l.MappingStatus == "SPECIFICATION_CONFLICT"),
                StockShortages = stockShortages,
                MissingPrices = canViewCost ? missingPrices : 0
            };

            string readiness = ComputeReadinessStatus(lines);

            return new SilaCulinaryMaterialWorkspaceDto
            {
                DraftId = draft.Id,
                DraftReference = draft.DraftReference,
                DraftRevision = draft.RevisionNumber,
                RecipeName = draft.RecipeName,
                Servings = draft.BaseServings,
                ServingSize = $"{payload.Header.ServingSize:0.##} {payload.Header.ServingUom}",
                PropertyId = draft.PropertyId,
                PropertyName = draft.PropertyName,
                KitchenName = draft.KitchenName,
                PlantCode = plantCode,
                OperatingMode = cfg.OperatingMode,
                MatchingEngine = "SILA Local Material Intelligence (bge-small-en-v1.5 + MiniLM-L-6-v2)",
                CanViewCost = canViewCost,
                CanApproveAliases = CanManageAliases(roleId),
                LastMatchedOnUtc = lines.Select(l => l.LastMatchedOnUtc).Where(d => d.HasValue).OrderByDescending(d => d).FirstOrDefault(),
                ReadinessStatus = readiness,
                Kpis = kpis,
                Lines = lines
            };
        }

        private async Task<PythonIndexStatus> EnsureTenantIndexSyncedAsync(
            Guid buyerId, Guid? propertyId, Guid? locationId, SilaCulinaryMaterialConfigDto matCfg, CancellationToken cancellationToken)
        {
            return await SyncOrBuildPythonIndexAsync(buyerId, propertyId, locationId, matCfg, forceRebuild: false, cancellationToken);
        }

        private async Task<PythonIndexStatus> SyncOrBuildPythonIndexAsync(
            Guid buyerId, Guid? propertyId, Guid? locationId, SilaCulinaryMaterialConfigDto matCfg, bool forceRebuild, CancellationToken cancellationToken)
        {
            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId)
                .ToListAsync(cancellationToken);

            List<Guid> matIds = materials.Select(m => m.Id).ToList();
            List<MaterialUomConversion> allConvs = matIds.Count > 0
                ? await _repository.MaterialUomConversion.FindByCondition(x => matIds.Contains(x.MaterialId) && x.IsActive).ToListAsync(cancellationToken)
                : new List<MaterialUomConversion>();
            ILookup<Guid, MaterialUomConversion> convLookup = allConvs.ToLookup(c => c.MaterialId);

            Dictionary<Guid, bool> plantMap = await LoadPlantEligibilityMapAsync(buyerId, propertyId, locationId, matIds, cancellationToken);

            Dictionary<Guid, int> usageCounts = matIds.Count > 0
                ? await _repository.RecipeIngredient
                    .FindByCondition(x => x.IsActive && x.MaterialId.HasValue && matIds.Contains(x.MaterialId.Value))
                    .GroupBy(x => x.MaterialId!.Value)
                    .Select(g => new { MaterialId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.MaterialId, x => x.Count, cancellationToken)
                : new Dictionary<Guid, int>();

            List<SilaCulinaryMaterialAlias> approvedAliases = await _repository.SilaCulinaryMaterialAlias
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.ApprovalStatus == "APPROVED")
                .ToListAsync(cancellationToken);

            List<PythonMaterialRecord> pyMaterials = materials.Select(m =>
            {
                string revStamp = $"{m.DateUpdated:O}|{m.IsActive}|{m.Description}|{m.BaseUnitOfMeasure}";
                return new PythonMaterialRecord
                {
                    MaterialId = m.Id.ToString(),
                    MaterialCode = m.MaterialCode,
                    Description = m.Description,
                    MaterialGroup = m.MaterialGroup,
                    ProductType = m.ProductType,
                    BaseUom = m.BaseUnitOfMeasure,
                    OrderUom = m.OrderUnitOfMeasure,
                    AlternateUom = m.AlternateUnitOfMeasure,
                    UnitOfMeasureMapping = m.UnitOfMeasureMapping,
                    Barcode = m.Barcode,
                    IsActive = m.IsActive,
                    PlantEligible = plantMap.GetValueOrDefault(m.Id, true),
                    PurchasingEligible = m.IsActive,
                    VerifiedRecipeUsageCount = usageCounts.GetValueOrDefault(m.Id, 0),
                    RevisionHash = ComputeSha256Hex(revStamp)[..16],
                    Conversions = convLookup[m.Id].Select(c => new PythonMaterialUomConversion
                    {
                        FromUom = c.FromUom,
                        ToUom = c.ToUom,
                        Factor = c.Factor
                    }).ToList()
                };
            }).ToList();

            List<PythonAliasRecord> pyAliases = approvedAliases.Select(a => new PythonAliasRecord
            {
                AliasId = a.Id.ToString(),
                AliasText = a.AliasText,
                NormalizedAlias = a.NormalizedAlias,
                MaterialId = a.MaterialId.ToString(),
                MaterialCode = a.MaterialCode,
                PropertyId = a.PropertyId?.ToString(),
                Language = a.Language,
                ApprovalStatus = a.ApprovalStatus,
                Source = a.Source
            }).ToList();

            object payload = forceRebuild
                ? new
                {
                    tenant_id = buyerId.ToString(),
                    materials = pyMaterials,
                    aliases = pyAliases,
                    config = ToPythonConfig(matCfg),
                    force_rebuild = true
                }
                : new
                {
                    tenant_id = buyerId.ToString(),
                    upserted_materials = pyMaterials,
                    deactivated_material_ids = materials.Where(m => !m.IsActive).Select(m => m.Id.ToString()).ToList(),
                    aliases = pyAliases,
                    config = ToPythonConfig(matCfg)
                };

            string path = forceRebuild ? "/v1/materials/index/build" : "/v1/materials/index/sync";
            PythonIndexStatus status = await PostCulinaryEngineAsync<PythonIndexStatus>(path, payload, 120, cancellationToken);

            // Persist lightweight index state snapshot
            SilaCulinaryMaterialIndexState? state = await _repository.SilaCulinaryMaterialIndexState
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive);
            if (state == null)
            {
                state = new SilaCulinaryMaterialIndexState
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    IsActive = true
                };
                _repository.SilaCulinaryMaterialIndexState.Create(state);
            }
            state.Status = "READY";
            state.IndexedMaterialCount = status.MaterialCount;
            state.ActiveMaterialCount = materials.Count(m => m.IsActive);
            state.PendingUpdatesCount = 0;
            state.EmbeddingModelVersion = status.EmbeddingModel;
            state.RerankingModelVersion = matCfg.RerankingModel;
            state.SemanticBackend = status.SemanticBackend;
            state.BuildDurationMs = status.BuildDurationMs;
            state.LastBuildCompletedUtc ??= DateTime.UtcNow;
            state.LastIncrementalSyncUtc = DateTime.UtcNow;
            state.LastError = null;

            return status;
        }

        private async Task<Dictionary<Guid, bool>> LoadPlantEligibilityMapAsync(
            Guid buyerId, Guid? propertyId, Guid? locationId, List<Guid> materialIds, CancellationToken cancellationToken)
        {
            Dictionary<Guid, bool> map = materialIds.ToDictionary(id => id, _ => true);
            if (!propertyId.HasValue && !locationId.HasValue) return map;
            if (materialIds.Count == 0) return map;

            List<Guid> targetLocIds = new();
            if (locationId.HasValue)
            {
                targetLocIds.Add(locationId.Value);
            }
            else if (propertyId.HasValue)
            {
                targetLocIds = await _repository.InventoryLocation
                    .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.PropertyId == propertyId.Value)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            }

            if (targetLocIds.Count == 0) return map;

            // Only enforce extension gaps when the tenant actually maintains InventoryLocationMaterial stocking records
            bool anyStockingConfigured = await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId))
                .AnyAsync(cancellationToken);
            if (!anyStockingConfigured) return map;

            HashSet<Guid> materialsWithAnyLocationRow = (await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId))
                .Select(x => x.MaterialId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            HashSet<Guid> materialsAtTargetLocation = (await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && targetLocIds.Contains(x.LocationId) && materialIds.Contains(x.MaterialId))
                .Select(x => x.MaterialId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            foreach (Guid matId in materialIds)
            {
                if (materialsWithAnyLocationRow.Contains(matId) && !materialsAtTargetLocation.Contains(matId))
                {
                    map[matId] = false;
                }
            }
            return map;
        }

        private async Task<bool> IsMaterialPlantEligibleAsync(
            Guid buyerId, Guid materialId, Guid? propertyId, Guid? locationId, CancellationToken cancellationToken)
        {
            Dictionary<Guid, bool> map = await LoadPlantEligibilityMapAsync(
                buyerId, propertyId, locationId, new List<Guid> { materialId }, cancellationToken);
            return map.GetValueOrDefault(materialId, true);
        }

        private async Task<Dictionary<Guid, SilaCulinaryMaterialStockDto>> LoadStockByMaterialAsync(
            Guid buyerId, Guid userId, Guid roleId, Guid? propertyId, Guid? locationId,
            List<Guid> materialIds, CancellationToken cancellationToken)
        {
            Dictionary<Guid, SilaCulinaryMaterialStockDto> result = new();
            if (materialIds.Count == 0) return result;

            List<Guid> allowedLocs = await SilaAccess.GetLocationIdsAsync(_repository, buyerId, userId, roleId, cancellationToken);
            if (allowedLocs.Count == 0) return result;

            List<InventoryLocation> locs = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && allowedLocs.Contains(x.Id))
                .ToListAsync(cancellationToken);

            List<InventoryLocation> scopedLocs = locationId.HasValue
                ? locs.Where(l => l.Id == locationId.Value).ToList()
                : (propertyId.HasValue ? locs.Where(l => l.PropertyId == propertyId.Value).ToList() : locs);

            if (scopedLocs.Count == 0) scopedLocs = locs;
            List<Guid> locIds = scopedLocs.Select(l => l.Id).ToList();
            if (locIds.Count == 0) return result;

            List<InventoryBalance> balances = await _repository.InventoryBalance
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && locIds.Contains(x.LocationId) && materialIds.Contains(x.MaterialId))
                .ToListAsync(cancellationToken);

            if (balances.Count == 0) return result;

            // Open ITO reserved quantities
            Dictionary<Guid, Guid> openTransfers = await _repository.InternalTransferOrder
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && locIds.Contains(x.FromLocationId)
                    && (x.Status == Common.SILA_ITO_PENDING_APPROVAL || x.Status == Common.SILA_ITO_APPROVED))
                .ToDictionaryAsync(x => x.Id, x => x.FromLocationId, cancellationToken);

            Dictionary<Guid, decimal> reservedByMat = new();
            if (openTransfers.Count > 0)
            {
                List<Guid> transferIds = openTransfers.Keys.ToList();
                List<InternalTransferOrderItem> transferItems = await _repository.InternalTransferOrderItem
                    .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId) && transferIds.Contains(x.InternalTransferOrderId))
                    .ToListAsync(cancellationToken);

                foreach (var g in transferItems.GroupBy(i => i.MaterialId))
                {
                    reservedByMat[g.Key] = g.Sum(item => item.ApprovedQty > 0 ? item.ApprovedQty : item.RequestedQty);
                }
            }

            Dictionary<Guid, string> locNames = scopedLocs.ToDictionary(l => l.Id, l => l.LocationName);
            foreach (var g in balances.GroupBy(b => b.MaterialId))
            {
                decimal onHand = g.Sum(b => b.OnHandQty);
                decimal reserved = reservedByMat.GetValueOrDefault(g.Key, 0m);
                decimal available = Math.Max(0m, onHand - reserved);
                InventoryBalance first = g.First();
                DateTime? ts = g.Select(b => b.LastMovementOn ?? b.DateUpdated).OrderByDescending(d => d).FirstOrDefault();

                result[g.Key] = new SilaCulinaryMaterialStockDto
                {
                    Status = available > 0 ? "AVAILABLE" : "OUT_OF_STOCK",
                    PropertyId = propertyId,
                    StorageLocation = locNames.GetValueOrDefault(first.LocationId),
                    OnHandQty = Math.Round(onHand, 4),
                    ReservedQty = Math.Round(reserved, 4),
                    AvailableQty = Math.Round(available, 4),
                    InventoryUom = first.BaseUom,
                    StockTimestampUtc = ts
                };
            }

            return result;
        }

        private async Task<Dictionary<Guid, SilaCulinaryMaterialPriceDto>> LoadPriceByMaterialAsync(
            Guid buyerId, Guid? outletId, Guid? locationId, List<ItemBuyerMaster> materials, bool canViewCost, CancellationToken cancellationToken)
        {
            Dictionary<Guid, SilaCulinaryMaterialPriceDto> result = new();
            if (materials.Count == 0) return result;

            if (!canViewCost)
            {
                foreach (ItemBuyerMaster m in materials)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto { Status = "RESTRICTED", PriceSource = "RESTRICTED" };
                }
                return result;
            }

            List<Guid> matIds = materials.Select(m => m.Id).ToList();
            Guid? outletLocId = outletId ?? locationId;

            Dictionary<Guid, MaterialOutletPrice> outletPrices = outletLocId.HasValue
                ? await _repository.MaterialOutletPrice
                    .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.OutletLocationId == outletLocId.Value && matIds.Contains(x.MaterialId))
                    .ToDictionaryAsync(x => x.MaterialId, cancellationToken)
                : new Dictionary<Guid, MaterialOutletPrice>();

            DateTime now = DateTime.UtcNow;
            List<MaterialPriceChange> approvedChanges = await _repository.MaterialPriceChange
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && matIds.Contains(x.MaterialId)
                    && (x.Status == Common.APPROVED || x.Status == "APPROVED")
                    && x.EffectiveFrom <= now)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, MaterialPriceChange> latestChangeByMat = approvedChanges
                .GroupBy(x => x.MaterialId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (ItemBuyerMaster m in materials)
            {
                if (outletPrices.TryGetValue(m.Id, out MaterialOutletPrice? mop) && mop.UnitCost > 0)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "AVAILABLE",
                        Price = mop.UnitCost,
                        Currency = mop.Currency,
                        PriceUom = m.BaseUnitOfMeasure,
                        PriceSource = "OUTLET_PRICE",
                        DataTimestampUtc = mop.DateUpdated
                    };
                }
                else if (latestChangeByMat.TryGetValue(m.Id, out MaterialPriceChange? mpc) && mpc.ProposedUnitCost > 0)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "AVAILABLE",
                        Price = mpc.ProposedUnitCost,
                        Currency = mpc.Currency,
                        PriceUom = string.IsNullOrWhiteSpace(mpc.PriceUom) ? m.BaseUnitOfMeasure : mpc.PriceUom,
                        PriceSource = "APPROVED_PRICE_CHANGE",
                        EffectiveDateUtc = mpc.EffectiveFrom,
                        DataTimestampUtc = mpc.DecidedOn ?? mpc.DateUpdated
                    };
                }
                else if (m.UnitCost.HasValue && m.UnitCost.Value > 0)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "AVAILABLE",
                        Price = m.UnitCost.Value,
                        Currency = string.IsNullOrWhiteSpace(m.Currency) ? "AED" : m.Currency,
                        PriceUom = m.BaseUnitOfMeasure,
                        PriceSource = "MATERIAL_MASTER_UNIT_COST",
                        DataTimestampUtc = m.DateUpdated
                    };
                }
                else if (m.StandardPrice.HasValue && m.StandardPrice.Value > 0)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "AVAILABLE",
                        Price = m.StandardPrice.Value,
                        Currency = string.IsNullOrWhiteSpace(m.Currency) ? "AED" : m.Currency,
                        PriceUom = m.BaseUnitOfMeasure,
                        PriceSource = "STANDARD_COST",
                        DataTimestampUtc = m.DateUpdated
                    };
                }
                else if (m.MovingAveragePrice.HasValue && m.MovingAveragePrice.Value > 0)
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "AVAILABLE",
                        Price = m.MovingAveragePrice.Value,
                        Currency = string.IsNullOrWhiteSpace(m.Currency) ? "AED" : m.Currency,
                        PriceUom = m.BaseUnitOfMeasure,
                        PriceSource = "MOVING_AVERAGE_COST",
                        DataTimestampUtc = m.DateUpdated
                    };
                }
                else
                {
                    result[m.Id] = new SilaCulinaryMaterialPriceDto
                    {
                        Status = "MISSING",
                        Price = null,
                        Currency = string.IsNullOrWhiteSpace(m.Currency) ? "AED" : m.Currency,
                        PriceUom = m.BaseUnitOfMeasure,
                        PriceSource = "MISSING"
                    };
                }
            }

            return result;
        }

        private void ApplyPythonMatchResult(
            SilaCulinaryMaterialMapping m,
            PythonIngredientMatchResult res,
            Dictionary<Guid, ItemBuyerMaster> allMaterials,
            SilaCulinaryRecipeDraft draft,
            string actorName)
        {
            m.NormalizedDescription = string.IsNullOrWhiteSpace(res.NormalizedDescription) ? m.NormalizedDescription : res.NormalizedDescription;
            m.MatchMethod = res.MatchMethod;
            m.MatchScore = (decimal)Math.Round(res.ConfidenceScore, 4);
            m.MappingStatus = res.MappingStatus;
            m.MissingClassification = res.MissingClassification;
            m.RecommendedRequestType = res.ProposedRequestDraft?.RequestType ?? res.RecommendedRequestType;
            m.LastMatchedOnUtc = DateTime.UtcNow;

            List<SilaCulinaryMaterialCandidateDto> candDtos = res.Candidates.Select(c =>
            {
                double finalScore = c.Scores?.CompositeScore ?? c.FinalScore;
                double exactScore = c.Scores?.Exact ?? c.ExactScore;
                double aliasScore = c.Scores?.Alias ?? c.AliasScore;
                double fuzzyScore = c.Scores?.FuzzyWratio ?? c.FuzzyScore;
                double semanticScore = c.Scores?.Semantic ?? c.SemanticScore;
                double rerankScore = c.Scores?.RerankNorm ?? c.RerankScore;
                bool uomCompat = c.UomValidation?.Compatible ?? c.UomCompatible;
                int? wholePacks = c.UomValidation?.IndicativeWholePacks
                    ?? (c.UomValidation?.IndicativePacksNeeded != null ? (int)Math.Ceiling(c.UomValidation.IndicativePacksNeeded.Value) : null);

                return new SilaCulinaryMaterialCandidateDto
                {
                    MaterialId = Guid.TryParse(c.MaterialId, out Guid mid) ? mid : Guid.Empty,
                    MaterialCode = c.MaterialCode,
                    Description = c.Description,
                    MaterialGroup = c.MaterialGroup,
                    ProductType = c.ProductType,
                    BaseUom = c.BaseUom,
                    OrderUom = c.OrderUom,
                    AlternateUom = c.AlternateUom,
                    Barcode = c.Barcode,
                    ImageUrl = null,
                    MatchMethod = c.MatchMethod,
                    FinalScore = finalScore,
                    ExactScore = exactScore,
                    AliasScore = aliasScore,
                    FuzzyScore = fuzzyScore,
                    SemanticScore = semanticScore,
                    RerankScore = rerankScore,
                    SpecificationCompatible = c.SpecificationCompatible,
                    UomCompatible = uomCompat,
                    MaterialActive = c.MaterialActive,
                    PlantEligible = c.PlantEligible,
                    ConvertedBaseQuantity = c.UomValidation?.ConvertedQuantity,
                    ConversionFactor = c.UomValidation?.ConversionFactor,
                    IndicativePacksNeeded = c.UomValidation?.IndicativePacksNeeded,
                    IndicativeWholePacks = wholePacks,
                    ExcessAfterPackRounding = c.UomValidation?.ExcessAfterPackRounding,
                    UomReason = !string.IsNullOrWhiteSpace(c.UomValidation?.Warning) ? c.UomValidation.Warning : c.UomValidation?.Reason,
                    Evidence = c.Evidence,
                    Warnings = c.Warnings
                };
            }).ToList();

            m.CandidatesJson = JsonSerializer.Serialize(candDtos, CamelJson);

            PythonMatchCandidate? selected = res.SelectedCandidate ?? res.Candidates.FirstOrDefault();
            if (selected != null && Guid.TryParse(selected.MaterialId, out Guid selId) && allMaterials.TryGetValue(selId, out ItemBuyerMaster? mat))
            {
                m.MaterialId = mat.Id;
                m.MaterialCode = mat.MaterialCode;
                m.MaterialDescription = mat.Description;
                m.BaseUom = mat.BaseUnitOfMeasure;
                m.ConvertedBaseQuantity = selected.UomValidation?.ConvertedQuantity;
                m.ConversionFactor = selected.UomValidation?.ConversionFactor;
                m.SpecificationCompatible = selected.SpecificationCompatible;
                m.UomCompatible = selected.UomValidation?.Compatible ?? selected.UomCompatible;
                m.EvidenceJson = JsonSerializer.Serialize(selected.Evidence, CamelJson);
            }
            else
            {
                m.MaterialId = null;
                m.MaterialCode = null;
                m.MaterialDescription = null;
                m.BaseUom = null;
                m.ConvertedBaseQuantity = null;
                m.ConversionFactor = null;
                m.SpecificationCompatible = true;
                m.UomCompatible = true;
                m.EvidenceJson = JsonSerializer.Serialize(new List<string>(), CamelJson);
            }

            if (res.ProposedRequestDraft != null)
            {
                SilaCulinaryMaterialRequestDraftDto reqDto = new()
                {
                    RequestType = res.ProposedRequestDraft.RequestType,
                    ProposedDescription = res.ProposedRequestDraft.ProposedDescription,
                    ProposedLongDescription = res.ProposedRequestDraft.ProposedLongDescription,
                    IngredientCategory = res.ProposedRequestDraft.IngredientCategory,
                    ProposedMaterialGroup = res.ProposedRequestDraft.ProposedMaterialGroup,
                    ProposedBaseUom = res.ProposedRequestDraft.ProposedBaseUom,
                    ProposedPurchasingUom = res.ProposedRequestDraft.ProposedPurchasingUom,
                    ProposedPackSize = res.ProposedRequestDraft.ProposedPackSize,
                    BrandPreference = res.ProposedRequestDraft.BrandPreference,
                    ProductSpecification = res.ProposedRequestDraft.ProductSpecification,
                    StorageRequirement = res.ProposedRequestDraft.StorageRequirement,
                    PotentialAllergens = res.ProposedRequestDraft.PotentialAllergens,
                    DraftId = draft.Id,
                    DraftReference = draft.DraftReference,
                    IngredientTempRef = m.IngredientTempRef,
                    RequiredQuantity = m.RecipeQuantity,
                    RecipeUom = m.RecipeUom,
                    PropertyId = draft.PropertyId,
                    PlantCode = draft.KitchenName,
                    RequestedBy = actorName,
                    Reason = res.ProposedRequestDraft.Reason,
                    DuplicateCheckEvidence = res.ProposedRequestDraft.DuplicateCheckEvidence,
                    CandidateMaterialCodesChecked = res.ProposedRequestDraft.CandidateMaterialCodesChecked,
                    IsAiSuggested = true
                };
                m.MissingReason = reqDto.Reason;
                m.RequestDraftJson = JsonSerializer.Serialize(reqDto, CamelJson);
            }
            else
            {
                m.MissingReason = null;
                m.RequestDraftJson = null;
            }
        }

        private PythonLlmSettings? BuildAuthorizedLlmSettings(SilaCulinaryAiConfiguration cfg, bool requested)
        {
            if (!requested) return null;
            string mode = (cfg.OperatingMode ?? SilaCulinaryAiSettings.MODE_LOCAL).ToUpperInvariant();
            string fallback = (cfg.FallbackPolicy ?? "NONE").ToUpperInvariant() switch
            {
                "LOCAL_ONLY" or "LOCAL_IF_ALLOWED" => "LOCAL_ONLY",
                "CONFIGURED_ORDER" or "CONFIGURED_SECONDARY" => "CONFIGURED_ORDER",
                _ => "NONE"
            };

            if (mode == SilaCulinaryAiSettings.MODE_LOCAL && cfg.LocalEnabled)
            {
                return new PythonLlmSettings
                {
                    TenantId = cfg.BuyerId.ToString(),
                    OperatingMode = "LOCAL",
                    TenantExternalAiAllowed = false,
                    LocalEnabled = true,
                    LocalEndpoint = cfg.LocalEndpoint,
                    LocalModel = string.IsNullOrWhiteSpace(cfg.LocalModel) ? "qwen2.5:7b-instruct" : cfg.LocalModel,
                    TimeoutSeconds = Math.Clamp(cfg.RequestTimeoutSeconds, 1, 120),
                    MaxOutputTokens = Math.Clamp(cfg.MaxOutputTokens, 256, 4096),
                    Temperature = (double)cfg.Temperature,
                    MaxRetries = Math.Clamp(cfg.RetryCount, 0, 3),
                    FallbackPolicy = fallback
                };
            }

            if (!cfg.AllowExternalAi) return null;

            bool geminiReady = (mode == SilaCulinaryAiSettings.MODE_GEMINI || mode == SilaCulinaryAiSettings.MODE_HYBRID)
                && cfg.GeminiEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedGeminiApiKey);
            bool claudeReady = (mode == SilaCulinaryAiSettings.MODE_CLAUDE || mode == SilaCulinaryAiSettings.MODE_HYBRID)
                && cfg.ClaudeEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedClaudeApiKey);

            if (!geminiReady && !claudeReady) return null;

            return new PythonLlmSettings
            {
                TenantId = cfg.BuyerId.ToString(),
                OperatingMode = mode,
                TenantExternalAiAllowed = true,
                LocalEnabled = cfg.LocalEnabled,
                LocalEndpoint = cfg.LocalEndpoint,
                LocalModel = string.IsNullOrWhiteSpace(cfg.LocalModel) ? "qwen2.5:7b-instruct" : cfg.LocalModel,
                GeminiEnabled = geminiReady,
                GeminiModel = string.IsNullOrWhiteSpace(cfg.GeminiModel) ? "gemini-2.5-flash" : cfg.GeminiModel,
                GeminiApiKey = geminiReady ? _protector.Unprotect(cfg.ProtectedGeminiApiKey) : null,
                ClaudeEnabled = claudeReady,
                ClaudeModel = string.IsNullOrWhiteSpace(cfg.ClaudeModel) ? "claude-sonnet-4-20250514" : cfg.ClaudeModel,
                ClaudeApiKey = claudeReady ? _protector.Unprotect(cfg.ProtectedClaudeApiKey) : null,
                TimeoutSeconds = Math.Clamp(cfg.RequestTimeoutSeconds, 1, 120),
                MaxOutputTokens = Math.Clamp(cfg.MaxOutputTokens, 256, 4096),
                Temperature = (double)cfg.Temperature,
                MaxRetries = Math.Clamp(cfg.RetryCount, 0, 3),
                FallbackPolicy = fallback
            };
        }

        private async Task<string?> ResolvePlantCodeAsync(
            Guid buyerId, Guid? propertyId, Guid? locationId, CancellationToken cancellationToken)
        {
            if (locationId.HasValue)
            {
                InventoryLocation? loc = await _repository.InventoryLocation
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.Id == locationId.Value && x.IsActive);
                if (!string.IsNullOrWhiteSpace(loc?.StorageLocationCode)) return loc.StorageLocationCode;
                if (!string.IsNullOrWhiteSpace(loc?.LocationCode)) return loc.LocationCode;
            }
            if (propertyId.HasValue)
            {
                BuyerProperty? prop = await _repository.BuyerProperty
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.Id == propertyId.Value && x.IsActive);
                if (!string.IsNullOrWhiteSpace(prop?.PlantCode)) return prop.PlantCode;
                InventoryLocation? loc = await _repository.InventoryLocation
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.PropertyId == propertyId.Value && x.IsActive);
                if (!string.IsNullOrWhiteSpace(loc?.LocationCode)) return loc.LocationCode;
            }
            return null;
        }

        private async Task<SilaCulinaryMaterialMapping> LoadMappingLineAsync(Guid buyerId, Guid draftId, string ingredientTempRef)
        {
            SilaCulinaryMaterialMapping? mapping = await _repository.SilaCulinaryMaterialMapping
                .FindFirstByConditionAsync(x =>
                    x.BuyerId == buyerId
                    && x.DraftId == draftId
                    && x.IngredientTempRef == ingredientTempRef
                    && x.IsActive);

            return mapping ?? throw new NotFoundCustomException(
                "Ingredient mapping line not found.",
                $"Ingredient '{ingredientTempRef}' was not found for this draft.");
        }

        private static void EnsureRevisionMatches(SilaCulinaryMaterialMapping mapping, int? expectedRevision)
        {
            if (expectedRevision.HasValue && expectedRevision.Value != mapping.MappingRevision)
            {
                throw new BadRequestCustomException(
                    "Concurrent mapping modification detected.",
                    $"Expected revision {expectedRevision.Value}, but current revision is {mapping.MappingRevision}. Refresh and retry.");
            }
        }

        private async Task CreatePendingAliasIfNotDuplicateAsync(
            Guid buyerId, string aliasText, ItemBuyerMaster material, Guid? propertyId, Guid userId, string actorName, CancellationToken cancellationToken)
        {
            string norm = NormalizeBasic(aliasText);
            bool exists = await _repository.SilaCulinaryMaterialAlias
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.NormalizedAlias == norm && x.PropertyId == propertyId)
                .AnyAsync(cancellationToken);
            if (exists) return;

            _repository.SilaCulinaryMaterialAlias.Create(new SilaCulinaryMaterialAlias
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                AliasText = aliasText.Trim(),
                NormalizedAlias = norm,
                MaterialId = material.Id,
                MaterialCode = material.MaterialCode,
                PropertyId = propertyId,
                Language = "en",
                Source = "CHEF_MAPPING_SUGGESTION",
                ApprovalStatus = "PENDING",
                Revision = 1,
                CreatedByUserId = userId,
                CreatedByName = actorName,
                EffectiveFromUtc = DateTime.UtcNow,
                IsActive = true
            });
        }

        private static void UpdateDraftIngredientMappingRef(CulinaryStructuredRecipePayloadDto payload, SilaCulinaryMaterialMapping mapping)
        {
            CulinaryIngredientLineDto? line = payload.Ingredients.FirstOrDefault(
                x => string.Equals(x.TempRef, mapping.IngredientTempRef, StringComparison.OrdinalIgnoreCase));
            if (line != null)
            {
                line.MaterialMappingStatus = mapping.MappingStatus;
                line.MaterialReference = mapping.MaterialCode;
            }
        }

        private static (bool Compatible, decimal? ConvertedQty, decimal? Factor, string Reason) ComputeDeterministicUomConversion(
            decimal qty, string recipeUom, string baseUom, List<MaterialUomConversion> conversions)
        {
            string r = NormalizeUomToken(recipeUom);
            string b = NormalizeUomToken(baseUom);
            if (r == b)
            {
                return (true, Math.Round(qty, 4, MidpointRounding.AwayFromZero), 1m, "Direct UOM match.");
            }

            if ((r == "G" && b == "KG") || (r == "ML" && b == "L"))
            {
                return (true, Math.Round(qty * 0.001m, 4, MidpointRounding.AwayFromZero), 0.001m, $"Metric conversion {r} -> {b}.");
            }
            if ((r == "KG" && b == "G") || (r == "L" && b == "ML"))
            {
                return (true, Math.Round(qty * 1000m, 4, MidpointRounding.AwayFromZero), 1000m, $"Metric conversion {r} -> {b}.");
            }

            MaterialUomConversion? direct = conversions.FirstOrDefault(c =>
                NormalizeUomToken(c.FromUom) == r && NormalizeUomToken(c.ToUom) == b && c.Factor > 0);
            if (direct != null)
            {
                return (true, Math.Round(qty * direct.Factor, 4, MidpointRounding.AwayFromZero), direct.Factor,
                    $"Material conversion 1 {r} = {direct.Factor} {b}.");
            }

            MaterialUomConversion? reverse = conversions.FirstOrDefault(c =>
                NormalizeUomToken(c.FromUom) == b && NormalizeUomToken(c.ToUom) == r && c.Factor > 0);
            if (reverse != null)
            {
                decimal f = 1m / reverse.Factor;
                return (true, Math.Round(qty * f, 4, MidpointRounding.AwayFromZero), Math.Round(f, 6),
                    $"Material conversion 1 {b} = {reverse.Factor} {r}.");
            }

            return (false, null, null, $"No verified conversion between {recipeUom} and material base UOM {baseUom}.");
        }

        private static string NormalizeUomToken(string? uom)
        {
            string u = (uom ?? "EA").Trim().ToUpperInvariant();
            return u switch
            {
                "GR" or "GM" or "GMS" or "GRAM" or "GRAMS" => "G",
                "KGS" or "KILOGRAM" or "KILOGRAMS" => "KG",
                "MLS" or "MILLILITER" or "MILLILITRE" => "ML",
                "LTR" or "LTRS" or "LITER" or "LITRE" => "L",
                "PCS" or "PC" or "PIECE" or "EACH" => "EA",
                _ => u
            };
        }

        private static string NormalizeBasic(string raw)
        {
            return string.Join(" ", (raw ?? string.Empty).Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static string ComputeReadinessStatus(List<SilaCulinaryMaterialMappingLineDto> lines)
        {
            if (lines.Count == 0) return "MATERIAL_MAPPING_PENDING";
            if (lines.Any(l => l.MappingStatus == "PENDING_MATERIAL_REQUEST")) return "MATERIAL_REQUESTS_REQUIRED";
            if (lines.Any(l => l.MappingStatus == "UNMATCHED")) return "MISSING_MATERIALS_IDENTIFIED";
            if (lines.Any(l => l.MappingStatus != "CONFIRMED")) return "MATERIAL_REVIEW_REQUIRED";
            return "READY_FOR_LIFECYCLE_PROCESSING";
        }

        private static List<SilaCulinaryMaterialCandidateDto> DeserializeCandidates(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<SilaCulinaryMaterialCandidateDto>();
            try
            {
                return JsonSerializer.Deserialize<List<SilaCulinaryMaterialCandidateDto>>(json, CamelJson)
                    ?? new List<SilaCulinaryMaterialCandidateDto>();
            }
            catch
            {
                return new List<SilaCulinaryMaterialCandidateDto>();
            }
        }

        private void LogMatchMethodWorkflowEvent(Guid buyerId, Guid draftId, SilaCulinaryMaterialMapping m, Guid userId)
        {
            string action = m.MatchMethod switch
            {
                "EXACT_CODE" or "EXACT_DESCRIPTION" or "BARCODE" => Common.SILA_AUDIT_CULINARY_MAT_EXACT,
                "VERIFIED_ALIAS" or "ALIAS" => Common.SILA_AUDIT_CULINARY_MAT_ALIAS,
                "FUZZY" => Common.SILA_AUDIT_CULINARY_MAT_FUZZY,
                "SEMANTIC" or "RERANKED" => Common.SILA_AUDIT_CULINARY_MAT_SEMANTIC,
                "AI_ASSISTED" => Common.SILA_AUDIT_CULINARY_MAT_AI,
                _ => Common.SILA_AUDIT_CULINARY_MAT_MISSING
            };
            if (!m.UomCompatible) action = Common.SILA_AUDIT_CULINARY_MAT_UOM_CONFLICT;
            else if (!m.SpecificationCompatible) action = Common.SILA_AUDIT_CULINARY_MAT_SPEC_CONFLICT;

            AddWorkflowEvent(
                buyerId,
                Common.SILA_REF_CULINARY_MATERIAL_MAP,
                draftId,
                action,
                $"{m.IngredientTempRef} ({m.OriginalDescription}) -> {m.MappingStatus} [{m.MatchMethod}] {m.MaterialCode ?? "UNMATCHED"}",
                userId);
        }

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
                Comment = comment.Length > 490 ? comment[..490] : comment,
                ActorUserId = actorUserId,
                IsActive = true
            });
        }

        private async Task<T> PostCulinaryEngineAsync<T>(
            string path, object body, int timeoutSeconds, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 180)));

            string baseUrl = (_configuration[Common.CULINARY_ENGINE_URL] ?? "http://127.0.0.1:8013").TrimEnd('/');
            string url = $"{baseUrl}{path}";
            string json = JsonSerializer.Serialize(body, SnakeJson);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync(url, content, cts.Token);
            string raw = await response.Content.ReadAsStringAsync(cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Material Intelligence engine error ({(int)response.StatusCode}): {raw}");
            }

            T? result = JsonSerializer.Deserialize<T>(raw, SnakeJson);
            return result ?? throw new InvalidOperationException("Material Intelligence engine returned an empty response.");
        }

        private static PythonMatchConfig ToPythonConfig(SilaCulinaryMaterialConfigDto c) => new()
        {
            ExactMatchingEnabled = c.ExactMatchingEnabled,
            AliasMatchingEnabled = c.AliasMatchingEnabled,
            FuzzyMatchingEnabled = c.FuzzyMatchingEnabled,
            SemanticMatchingEnabled = c.SemanticMatchingEnabled,
            AdvancedRerankingEnabled = c.AdvancedRerankingEnabled,
            LlmAssistedMatchingEnabled = c.LlmAssistedMatchingEnabled,
            CandidateRetrievalLimit = Math.Clamp(c.CandidateRetrievalLimit, 5, 100),
            CandidateDisplayLimit = Math.Clamp(c.CandidateDisplayLimit, 1, 20),
            FuzzyThreshold = c.FuzzyThreshold,
            SemanticThreshold = c.SemanticThreshold,
            AutoSuggestThreshold = Math.Clamp(c.AutoSuggestThreshold, 0.5, 1.0),
            AmbiguityGapThreshold = Math.Clamp(c.AmbiguityGapThreshold, 0.0, 0.5),
            MandatoryChefReview = c.MandatoryChefReview,
            EmbeddingModel = c.EmbeddingModel,
            RerankingModel = c.RerankingModel,
            Device = string.Equals(c.ExecutionDevice, "GPU", StringComparison.OrdinalIgnoreCase) ? "GPU" : "CPU"
        };

        private static SilaCulinaryMaterialAliasDto MapAliasDto(SilaCulinaryMaterialAlias a, string? materialDesc) => new()
        {
            Id = a.Id,
            AliasText = a.AliasText,
            NormalizedAlias = a.NormalizedAlias,
            MaterialId = a.MaterialId,
            MaterialCode = a.MaterialCode,
            MaterialDescription = materialDesc,
            PropertyId = a.PropertyId,
            Language = a.Language,
            Source = a.Source,
            ApprovalStatus = a.ApprovalStatus,
            Revision = a.Revision,
            CreatedByName = a.CreatedByName,
            ApprovedByName = a.ApprovedByName,
            ApprovedOnUtc = a.ApprovedOnUtc,
            CreatedOn = a.DateCreated
        };

        private static string SafeSpreadsheetCell(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string trimmed = value.TrimStart();
            if (trimmed.StartsWith('=') || trimmed.StartsWith('+') || trimmed.StartsWith('-') || trimmed.StartsWith('@') || trimmed.StartsWith('\t'))
            {
                return "'" + value;
            }
            return value;
        }

        private static string CsvCell(string? value)
        {
            string safe = SafeSpreadsheetCell(value).Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }

        private static string ComputeSha256Hex(string input)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static byte[] BuildSimplePdfReport(SilaCulinaryMaterialWorkspaceDto ws, List<SilaCulinaryMaterialMappingLineDto> lines)
        {
            List<string> textLines = new()
            {
                $"SILA Culinary Intelligence - Material Matching Report",
                $"Recipe: {ws.RecipeName} ({ws.DraftReference}) | Servings: {ws.Servings} | Status: {ws.ReadinessStatus}",
                $"Generated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                "--------------------------------------------------------------------------------"
            };
            foreach (SilaCulinaryMaterialMappingLineDto l in lines)
            {
                string priceText = ws.CanViewCost && l.UnitPrice.HasValue ? $"{l.UnitPrice:0.00} {l.Currency}" : (ws.CanViewCost ? "Missing" : "Restricted");
                string stockText = l.AvailableStock.HasValue ? $"{l.AvailableStock:0.##} {l.StockUom}" : "Unknown";
                textLines.Add($"{l.IngredientTempRef} | {l.OriginalDescription} ({l.RequiredQuantity:0.##} {l.RecipeUom}) -> {l.MaterialCode ?? "UNMATCHED"} {l.MaterialDescription ?? ""} | {l.MappingStatus} | Stock: {stockText} | Price: {priceText}");
            }

            StringBuilder stream = new();
            stream.AppendLine("BT");
            stream.AppendLine("/F1 9 Tf");
            int y = 760;
            foreach (string line in textLines.Take(52))
            {
                string esc = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                stream.AppendLine($"1 0 0 1 36 {y} Tm ({esc}) Tj");
                y -= 13;
            }
            stream.AppendLine("ET");
            string streamStr = stream.ToString();

            List<string> objects = new()
            {
                "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
                "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
                "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n",
                $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(streamStr)} >>\nstream\n{streamStr}endstream\nendobj\n",
                "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
            };

            StringBuilder pdf = new();
            pdf.Append("%PDF-1.4\n");
            List<int> offsets = new();
            foreach (string obj in objects)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.Append(obj);
            }
            int xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (int off in offsets)
            {
                pdf.Append($"{off:D10} 00000 n \n");
            }
            pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        // Python wire models
        private sealed class PythonMaterialRecord
        {
            public string MaterialId { get; set; } = string.Empty;
            public string MaterialCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? MaterialGroup { get; set; }
            public string? ProductType { get; set; }
            public string BaseUom { get; set; } = "EA";
            public string? OrderUom { get; set; }
            public string? AlternateUom { get; set; }
            public string? UnitOfMeasureMapping { get; set; }
            public string? Barcode { get; set; }
            public bool IsActive { get; set; }
            public bool PlantEligible { get; set; }
            public bool PurchasingEligible { get; set; }
            public int VerifiedRecipeUsageCount { get; set; }
            [JsonPropertyName("revision_stamp")]
            public string RevisionHash { get; set; } = string.Empty;
            [JsonPropertyName("uom_conversions")]
            public List<PythonMaterialUomConversion> Conversions { get; set; } = new();
        }

        private sealed class PythonMaterialUomConversion
        {
            public string FromUom { get; set; } = string.Empty;
            public string ToUom { get; set; } = string.Empty;
            public decimal Factor { get; set; }
        }

        private sealed class PythonAliasRecord
        {
            public string AliasId { get; set; } = string.Empty;
            public string AliasText { get; set; } = string.Empty;
            public string NormalizedAlias { get; set; } = string.Empty;
            public string MaterialId { get; set; } = string.Empty;
            public string MaterialCode { get; set; } = string.Empty;
            public string? PropertyId { get; set; }
            public string Language { get; set; } = "en";
            public string ApprovalStatus { get; set; } = "APPROVED";
            public string Source { get; set; } = "TENANT";
        }

        private sealed class PythonIndexStatus
        {
            public string TenantId { get; set; } = string.Empty;
            public string Status { get; set; } = "READY";
            [JsonPropertyName("indexed_material_count")]
            public int MaterialCount { get; set; }
            public int AliasCount { get; set; }
            public string EmbeddingModel { get; set; } = string.Empty;
            public string SemanticBackend { get; set; } = string.Empty;
            public double BuildDurationMs { get; set; }
        }

        private sealed class PythonIngredientQuery
        {
            [JsonPropertyName("temp_ref")]
            public string IngredientTempRef { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? NormalizedDescription { get; set; }
            public decimal Quantity { get; set; }
            public string RecipeUom { get; set; } = "EA";
            public string? Category { get; set; }
            public string? IngredientForm { get; set; }
            public string? BrandPreference { get; set; }
            public string? PropertyId { get; set; }
            public string? PlantCode { get; set; }
            public string? ExplicitMaterialCode { get; set; }
        }

        private sealed class PythonMatchConfig
        {
            public bool ExactMatchingEnabled { get; set; }
            public bool AliasMatchingEnabled { get; set; }
            public bool FuzzyMatchingEnabled { get; set; }
            public bool SemanticMatchingEnabled { get; set; }
            [JsonPropertyName("reranking_enabled")]
            public bool AdvancedRerankingEnabled { get; set; }
            [JsonPropertyName("llm_assisted_enabled")]
            public bool LlmAssistedMatchingEnabled { get; set; }
            public int CandidateRetrievalLimit { get; set; }
            public int CandidateDisplayLimit { get; set; }
            public double FuzzyThreshold { get; set; }
            public double SemanticThreshold { get; set; }
            [JsonPropertyName("auto_exact_threshold")]
            public double AutoSuggestThreshold { get; set; }
            [JsonPropertyName("ambiguity_margin")]
            public double AmbiguityGapThreshold { get; set; }
            public bool MandatoryChefReview { get; set; }
            public string EmbeddingModel { get; set; } = string.Empty;
            public string RerankingModel { get; set; } = string.Empty;
            [JsonPropertyName("execution_mode")]
            public string Device { get; set; } = "CPU";
        }

        private sealed class PythonLlmSettings
        {
            public string TenantId { get; set; } = string.Empty;
            public string OperatingMode { get; set; } = "LOCAL";
            public bool TenantExternalAiAllowed { get; set; }
            public bool LocalEnabled { get; set; } = true;
            public string? LocalEndpoint { get; set; }
            public string LocalModel { get; set; } = "qwen2.5:7b-instruct";
            public bool GeminiEnabled { get; set; }
            public string GeminiModel { get; set; } = "gemini-2.5-flash";
            public string? GeminiApiKey { get; set; }
            public bool ClaudeEnabled { get; set; }
            public string ClaudeModel { get; set; } = "claude-sonnet-4-20250514";
            public string? ClaudeApiKey { get; set; }
            public double TimeoutSeconds { get; set; } = 30.0;
            public int MaxOutputTokens { get; set; } = 1024;
            public double Temperature { get; set; } = 0.2;
            public int MaxRetries { get; set; } = 1;
            public string FallbackPolicy { get; set; } = "NONE";
        }

        private sealed class PythonMatchBulkRequest
        {
            public string TenantId { get; set; } = string.Empty;
            public string? DraftId { get; set; }
            public string? DraftReference { get; set; }
            public string? PropertyId { get; set; }
            public string? PlantCode { get; set; }
            public PythonMatchConfig Config { get; set; } = new();
            [JsonPropertyName("provider_config")]
            public PythonLlmSettings? Llm { get; set; }
            public bool UseAiDisambiguation { get; set; }
            public List<PythonIngredientQuery> Ingredients { get; set; } = new();
        }

        private sealed class PythonMatchBulkResponse
        {
            public List<PythonIngredientMatchResult> Results { get; set; } = new();
        }

        private sealed class PythonCandidateScores
        {
            public double Exact { get; set; }
            public double Alias { get; set; }
            public double FuzzyWratio { get; set; }
            public double Semantic { get; set; }
            public double RerankNorm { get; set; }
            public double CompositeScore { get; set; }
        }

        private sealed class PythonUomValidation
        {
            public bool Compatible { get; set; }
            [JsonPropertyName("converted_base_qty")]
            public decimal? ConvertedQuantity { get; set; }
            public decimal? ConversionFactor { get; set; }
            public decimal? IndicativePacksNeeded { get; set; }
            public int? IndicativeWholePacks { get; set; }
            public decimal? ExcessAfterPackRounding { get; set; }
            public string? Warning { get; set; }
            public string Reason { get; set; } = string.Empty;
        }

        private sealed class PythonMatchCandidate
        {
            public string MaterialId { get; set; } = string.Empty;
            public string MaterialCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? MaterialGroup { get; set; }
            public string? ProductType { get; set; }
            public string BaseUom { get; set; } = "EA";
            public string? OrderUom { get; set; }
            public string? AlternateUom { get; set; }
            public string? Barcode { get; set; }
            public string MatchMethod { get; set; } = string.Empty;
            public PythonCandidateScores? Scores { get; set; }
            public double FinalScore { get; set; }
            public double ExactScore { get; set; }
            public double AliasScore { get; set; }
            public double FuzzyScore { get; set; }
            public double SemanticScore { get; set; }
            public double RerankScore { get; set; }
            public bool SpecificationCompatible { get; set; }
            public bool UomCompatible { get; set; }
            public bool MaterialActive { get; set; }
            public bool PlantEligible { get; set; }
            public PythonUomValidation? UomValidation { get; set; }
            public List<string> Evidence { get; set; } = new();
            public List<string> Warnings { get; set; } = new();
        }

        private sealed class PythonProposedRequestDraft
        {
            public string RequestType { get; set; } = "NEW_MATERIAL";
            public string ProposedDescription { get; set; } = string.Empty;
            [JsonPropertyName("long_description")]
            public string? ProposedLongDescription { get; set; }
            public string? IngredientCategory { get; set; }
            public string? ProposedMaterialGroup { get; set; }
            public string? ProposedBaseUom { get; set; }
            public string? ProposedPurchasingUom { get; set; }
            public string? ProposedPackSize { get; set; }
            public string? BrandPreference { get; set; }
            public string? ProductSpecification { get; set; }
            public string? StorageRequirement { get; set; }
            public List<string> PotentialAllergens { get; set; } = new();
            [JsonPropertyName("reason_for_request")]
            public string Reason { get; set; } = string.Empty;
            public List<string> DuplicateCheckEvidence { get; set; } = new();
            public List<string> CandidateMaterialCodesChecked { get; set; } = new();
        }

        private sealed class PythonIngredientMatchResult
        {
            [JsonPropertyName("temp_ref")]
            public string IngredientTempRef { get; set; } = string.Empty;
            public string NormalizedDescription { get; set; } = string.Empty;
            public string MatchMethod { get; set; } = "NONE";
            [JsonPropertyName("status")]
            public string MappingStatus { get; set; } = "UNMATCHED";
            [JsonPropertyName("missing_material_classification")]
            public string MissingClassification { get; set; } = "NONE";
            public string? RecommendedRequestType { get; set; }
            public double ConfidenceScore { get; set; }
            [JsonPropertyName("recommended_candidate")]
            public PythonMatchCandidate? SelectedCandidate { get; set; }
            public List<PythonMatchCandidate> Candidates { get; set; } = new();
            [JsonPropertyName("proposed_material_request_draft")]
            public PythonProposedRequestDraft? ProposedRequestDraft { get; set; }
        }
    }
}
