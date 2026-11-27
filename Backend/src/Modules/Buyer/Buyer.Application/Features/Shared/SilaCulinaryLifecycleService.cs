using System.Globalization;
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
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryLifecycleService
    {
        // Material Requests
        Task<List<SilaCulinaryMaterialRequestDto>> ListMaterialRequestsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? status, string? requestType,
            Guid? propertyId, Guid? draftId, string? search, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> GetMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid requestId, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestAssistResultDto> AssistMaterialRequestAsync(
            Guid organizationId, SilaCulinaryMaterialRequestAssistInputDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> CreateMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken);

        Task<List<SilaCulinaryMaterialRequestDto>> BulkPrepareMaterialRequestsAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryBulkMaterialRequestDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> UpdateMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> SubmitMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> DecideMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestDecisionDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> ResubmitMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestUpsertDto? updates, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> ExecuteMaterialCreationAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            string? idempotencyKey, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDto> VerifyManualErpMaterialAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryManualErpVerifyDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryMaterialRequestDashboardDto> GetMaterialRequestDashboardAsync(
            Guid organizationId, Guid? propertyId, CancellationToken cancellationToken);

        Task<(byte[] Content, string ContentType, string FileName)> ExportMaterialRequestsReportAsync(
            Guid organizationId, string format, string? status, CancellationToken cancellationToken);

        // Costing, Scenarios & Optimization
        Task<SilaCulinaryCostingResultDto> CalculateDraftCostingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryCostingCalculateRequestDto input, CancellationToken cancellationToken);

        Task<List<SilaCulinaryCostingResultDto>> ListCostingSnapshotsAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<SilaCulinaryCostOptimizationResultDto> OptimizeDraftCostingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryCostOptimizationRequestDto input, CancellationToken cancellationToken);

        Task<SilaCulinaryBulkRecostResultDto> BulkRecostAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryBulkRecostRequestDto input, CancellationToken cancellationToken);

        // Readiness, Approvals, Production Recipe ID, Images, Publishing, Versioning & Phase 2A Handover
        Task<SilaCulinaryRecipeReadinessDto> GetRecipeReadinessAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<SilaCulinaryApprovalPackageDto> GetApprovalPackageAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> SubmitRecipeForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> DecideRecipeApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            bool approve, SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> CreateProductionRecipeAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> AssignRecipeImageAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            byte[] content, string? imageOrigin, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> PublishProductionRecipeAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken);

        Task<SilaCulinaryRecipeLifecycleSummaryDto> CreateNewProductionVersionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken);

        Task<List<SilaCulinaryRecipeVersionHistoryItemDto>> GetVersionHistoryAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken);

        Task<SilaCulinaryLifecycleDashboardDto> GetLifecycleDashboardAsync(
            Guid organizationId, Guid? propertyId, CancellationToken cancellationToken);

        Task<(byte[] Content, string ContentType, string FileName)> ExportLifecycleReportAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string reportType, string format, CancellationToken cancellationToken);

        Task<SilaCulinaryPhase2AHandoverDto> GetPhase2AHandoverAsync(
            Guid organizationId, Guid productionRecipeId, int? version, CancellationToken cancellationToken);
    }

    public class SilaCulinaryLifecycleService : ISilaCulinaryLifecycleService
    {
        private static readonly JsonSerializerOptions CamelJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions SnakeJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private static readonly HashSet<string> ValidRequestTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NEW_MATERIAL",
            "MATERIAL_EXTENSION",
            "MATERIAL_REACTIVATION",
            "UOM_MAINTENANCE",
            "SPECIFICATION_CORRECTION",
            "CLASSIFICATION_MAINTENANCE",
            "MANUAL_REVIEW"
        };

        private static readonly HashSet<string> ActiveRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "DRAFT", "VALIDATION_REQUIRED", "READY_FOR_SUBMISSION", "SUBMITTED", "PENDING_REVIEW",
            "PENDING_APPROVAL", "RETURNED_FOR_CORRECTION", "APPROVED", "CREATION_PENDING",
            "CREATION_IN_PROGRESS", "ERP_OUTCOME_UNKNOWN", "MATERIAL_CREATED", "SYNCHRONIZATION_PENDING"
        };

        private readonly IRepositoryWrapper _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SilaCulinaryLifecycleService(
            IRepositoryWrapper repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // =========================================================================
        // 1. MATERIAL REQUEST WORKSPACE & EXECUTION (§5 - §17, §39)
        // =========================================================================

        public async Task<List<SilaCulinaryMaterialRequestDto>> ListMaterialRequestsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? status, string? requestType,
            Guid? propertyId, Guid? draftId, string? search, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            IQueryable<SilaCulinaryMaterialRequest> query = _repository.SilaCulinaryMaterialRequest
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                string st = status.Trim().ToUpperInvariant();
                query = query.Where(x => x.Status == st);
            }
            if (!string.IsNullOrWhiteSpace(requestType) && !string.Equals(requestType, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                string rt = requestType.Trim().ToUpperInvariant();
                query = query.Where(x => x.RequestType == rt);
            }
            if (propertyId.HasValue)
            {
                query = query.Where(x => x.PropertyId == propertyId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    x.RequestReference.ToLower().Contains(term)
                    || x.ProposedDescription.ToLower().Contains(term)
                    || (x.ResultingMaterialCode != null && x.ResultingMaterialCode.ToLower().Contains(term))
                    || (x.TargetMaterialCode != null && x.TargetMaterialCode.ToLower().Contains(term)));
            }

            List<SilaCulinaryMaterialRequest> requests = await query
                .OrderByDescending(x => x.DateUpdated)
                .ToListAsync(cancellationToken);

            if (draftId.HasValue)
            {
                HashSet<Guid> reqIdsForDraft = (await _repository.SilaCulinaryMaterialRequestLink
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draftId.Value && x.IsActive)
                    .Select(x => x.MaterialRequestId)
                    .Distinct()
                    .ToListAsync(cancellationToken)).ToHashSet();
                requests = requests.Where(r => reqIdsForDraft.Contains(r.Id)).ToList();
            }

            return await HydrateMaterialRequestsAsync(buyer, requests, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialRequestDto> GetMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid requestId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);
            List<SilaCulinaryMaterialRequestDto> list = await HydrateMaterialRequestsAsync(
                buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken);
            return list.First();
        }

        public async Task<SilaCulinaryMaterialRequestAssistResultDto> AssistMaterialRequestAsync(
            Guid organizationId, SilaCulinaryMaterialRequestAssistInputDto input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.IngredientDescription))
            {
                throw new BadRequestCustomException("Ingredient description required.", "Provide an ingredient description for AI preparation assistance.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id)
                .Take(500)
                .ToListAsync(cancellationToken);

            string? recipeName = null;
            if (input.DraftId.HasValue)
            {
                recipeName = (await _repository.SilaCulinaryRecipeDraft
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == input.DraftId.Value && x.IsActive))?.RecipeName;
            }

            Dictionary<Guid, bool> plantMap = await LoadPlantEligibilityAsync(
                buyer.Id, input.PropertyId, materials.Select(m => m.Id).ToList(), cancellationToken);

            try
            {
                object pyPayload = new
                {
                    tenant_id = buyer.Id.ToString(),
                    ingredient_description = input.IngredientDescription.Trim(),
                    required_qty = input.RequiredQty,
                    recipe_uom = input.RecipeUom,
                    recipe_name = recipeName,
                    existing_materials = materials.Select(m => new
                    {
                        material_id = m.Id.ToString(),
                        material_code = m.MaterialCode,
                        description = m.Description,
                        base_uom = m.BaseUnitOfMeasure,
                        is_active = m.IsActive,
                        plant_eligible = plantMap.GetValueOrDefault(m.Id, true)
                    }).ToList()
                };

                PythonAssistResponse pyRes = await PostCulinaryEngineAsync<PythonAssistResponse>(
                    "/v1/material-requests/assist", pyPayload, 20, cancellationToken);

                return new SilaCulinaryMaterialRequestAssistResultDto
                {
                    RecommendedRequestType = pyRes.RecommendedRequestType,
                    ProposedDescription = pyRes.ProposedDescription,
                    ProposedLongDescription = pyRes.ProposedLongDescription,
                    IngredientCategory = pyRes.IngredientCategory,
                    ProposedMaterialGroup = pyRes.ProposedMaterialGroup,
                    ProposedBaseUom = pyRes.ProposedBaseUom,
                    ProductSpecification = pyRes.ProductSpecification,
                    StorageRequirement = pyRes.StorageRequirement,
                    PotentialAllergens = pyRes.PotentialAllergens ?? new(),
                    BusinessJustification = pyRes.BusinessJustification,
                    PotentialDuplicates = (pyRes.PotentialDuplicates ?? new()).Select(d => new SilaCulinaryDuplicateCandidateDto
                    {
                        MaterialId = Guid.TryParse(d.MaterialId, out Guid mid) ? mid : null,
                        MaterialCode = d.MaterialCode,
                        Description = d.Description,
                        Similarity = d.Similarity,
                        IsActive = d.IsActive,
                        PlantEligible = d.PlantEligible,
                        Reason = d.Reason
                    }).ToList(),
                    ProviderUsed = pyRes.ProviderUsed,
                    AiSuggested = pyRes.AiSuggested
                };
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Python material assist unavailable; falling back to local C# duplicate check. Reason: {ex.Message}");
                string raw = input.IngredientDescription.Trim();
                List<SilaCulinaryDuplicateCandidateDto> localDups = FindDuplicateCandidatesLocal(raw, null, materials, plantMap);
                string recType = localDups.Any(d => !d.IsActive)
                    ? "MATERIAL_REACTIVATION"
                    : localDups.Any(d => !d.PlantEligible)
                        ? "MATERIAL_EXTENSION"
                        : "NEW_MATERIAL";
                string uom = NormalizeUom(input.RecipeUom);
                if (uom == "G") uom = "KG";
                if (uom == "ML") uom = "L";

                return new SilaCulinaryMaterialRequestAssistResultDto
                {
                    RecommendedRequestType = recType,
                    ProposedDescription = raw.ToUpperInvariant(),
                    ProposedLongDescription = $"{raw.ToUpperInvariant()} (Standard culinary ingredient)",
                    IngredientCategory = "CULINARY",
                    ProposedMaterialGroup = "CULINARY_INGREDIENTS",
                    ProposedBaseUom = uom,
                    ProductSpecification = "Chef / Master Data verification required",
                    StorageRequirement = "Verify ambient or chilled storage requirement",
                    BusinessJustification = recipeName != null ? $"Required for recipe '{recipeName}'." : "Required for AI Recipe Planner.",
                    PotentialDuplicates = localDups,
                    ProviderUsed = "LOCAL_FALLBACK",
                    AiSuggested = true
                };
            }
        }

        public async Task<SilaCulinaryMaterialRequestDto> CreateMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.ProposedDescription))
            {
                throw new BadRequestCustomException("Description required.", "Provide a proposed material description.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);

            // Idempotency check if caller supplied IdempotencyKey
            if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
            {
                string idem = input.IdempotencyKey.Trim();
                SilaCulinaryMaterialRequest? existingByIdem = await _repository.SilaCulinaryMaterialRequest
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == idem && x.IsActive);
                if (existingByIdem != null)
                {
                    return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { existingByIdem }, cancellationToken)).First();
                }
            }

            string reqType = NormalizeRequestType(input.RequestType);
            string normalizedDesc = NormalizeKey(input.ProposedDescription);

            // Duplicate prevention (§10): check existing pending requests & Material Master
            List<SilaCulinaryDuplicateCandidateDto> dupCandidates = await RunDuplicateScreeningAsync(
                buyer.Id, input.ProposedDescription, input.Barcode, input.PropertyId, reqType, input.TargetMaterialId, cancellationToken);

            SilaCulinaryMaterialRequest? pendingSame = await FindMatchingPendingRequestAsync(buyer.Id, normalizedDesc, reqType, input.PropertyId, cancellationToken);
            if (pendingSame != null)
            {
                // Consolidate links onto the existing pending request instead of creating a duplicate (§10 & §11)
                if (input.Links != null && input.Links.Count > 0)
                {
                    await AttachLinksToRequestAsync(buyer.Id, pendingSame, input.Links, cancellationToken);
                    _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        MaterialRequestId = pendingSame.Id,
                        Action = "CONSOLIDATED_LINK_ADDED",
                        PreviousStatus = pendingSame.Status,
                        NewStatus = pendingSame.Status,
                        ActorUserId = userId,
                        ActorName = actorName,
                        Comment = $"Linked {input.Links.Count} additional recipe ingredient requirement(s) to existing pending request {pendingSame.RequestReference}.",
                        IsActive = true
                    });
                    await _repository.SaveAsync();
                    return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { pendingSame }, cancellationToken)).First();
                }

                throw new ConflictCustomException(
                    "Duplicate pending material request.",
                    $"An active material request ({pendingSame.RequestReference}) already exists for '{pendingSame.ProposedDescription}'.");
            }

            if (reqType == "NEW_MATERIAL" && !input.AllowDuplicateOverride)
            {
                SilaCulinaryDuplicateCandidateDto? exactActive = dupCandidates.FirstOrDefault(d => d.Similarity >= 0.96 && d.IsActive && d.PlantEligible);
                if (exactActive != null)
                {
                    throw new ConflictCustomException(
                        "Duplicate material exists in Material Master.",
                        $"Existing material {exactActive.MaterialCode} ({exactActive.Description}) already matches '{input.ProposedDescription}'. Confirm mapping or switch request type to UOM/Spec/Extension.");
                }
            }

            string reqRef = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.MATERIAL_REQUEST, 8, cancellationToken);
            (string? propertyName, string? companyCode, string? plantCode) = await ResolvePropertyOrgAsync(buyer.Id, input.PropertyId, cancellationToken);

            SilaCulinaryMaterialRequest entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                RequestReference = reqRef,
                RequestType = reqType,
                Status = "DRAFT",
                RevisionNumber = 1,
                ProposedDescription = input.ProposedDescription.Trim(),
                LongDescription = TrimOrNull(input.LongDescription, 1000),
                IngredientCategory = TrimOrNull(input.IngredientCategory, 80),
                MaterialType = TrimOrNull(input.MaterialType, 60),
                MaterialGroup = TrimOrNull(input.MaterialGroup, 80),
                ProductHierarchy = TrimOrNull(input.ProductHierarchy, 120),
                Brand = TrimOrNull(input.Brand, 120),
                Manufacturer = TrimOrNull(input.Manufacturer, 160),
                ProductSpecification = TrimOrNull(input.ProductSpecification, 1000),
                Barcode = TrimOrNull(input.Barcode, 80),
                CountryOfOrigin = TrimOrNull(input.CountryOfOrigin, 80),
                PropertyId = input.PropertyId,
                PropertyName = propertyName,
                CompanyCode = TrimOrNull(input.CompanyCode, 40) ?? companyCode,
                PlantCode = TrimOrNull(input.PlantCode, 40) ?? plantCode,
                StorageLocation = TrimOrNull(input.StorageLocation, 40),
                PurchasingOrganization = TrimOrNull(input.PurchasingOrganization, 40),
                PurchasingGroup = TrimOrNull(input.PurchasingGroup, 40),
                BaseUom = NormalizeUom(input.BaseUom),
                PurchasingUom = string.IsNullOrWhiteSpace(input.PurchasingUom) ? null : NormalizeUom(input.PurchasingUom),
                AlternateUom = string.IsNullOrWhiteSpace(input.AlternateUom) ? null : NormalizeUom(input.AlternateUom),
                PackSize = input.PackSize,
                ConversionNumerator = input.ConversionNumerator,
                ConversionDenominator = input.ConversionDenominator,
                NetWeight = input.NetWeight,
                GrossWeight = input.GrossWeight,
                Volume = input.Volume,
                IngredientForm = TrimOrNull(input.IngredientForm, 80),
                StorageRequirements = TrimOrNull(input.StorageRequirements, 200),
                ShelfLifeDays = input.ShelfLifeDays,
                BatchManaged = input.BatchManaged,
                PotentialAllergens = TrimOrNull(input.PotentialAllergens, 500),
                DietaryClassification = TrimOrNull(input.DietaryClassification, 200),
                QualitySpecifications = TrimOrNull(input.QualitySpecifications, 500),
                FoodSafetyRequirements = TrimOrNull(input.FoodSafetyRequirements, 500),
                RequiredQuantity = input.RequiredQuantity,
                RequiredDateUtc = input.RequiredDateUtc,
                RequestedByUserId = userId,
                RequestedByName = actorName,
                BusinessJustification = TrimOrNull(input.BusinessJustification, 1000),
                SuggestedSupplier = TrimOrNull(input.SuggestedSupplier, 200),
                TargetMaterialId = input.TargetMaterialId,
                TargetMaterialCode = TrimOrNull(input.TargetMaterialCode, 80),
                DataOrigin = NormalizeOrigin(input.DataOrigin),
                DuplicateCheckJson = JsonSerializer.Serialize(dupCandidates, CamelJson),
                AttachmentsJson = input.Attachments != null ? JsonSerializer.Serialize(input.Attachments, CamelJson) : null,
                IdempotencyKey = TrimOrNull(input.IdempotencyKey, 120),
                CorrelationId = Guid.NewGuid().ToString("N")[..16],
                ExecutionStatus = "NOT_STARTED",
                IsActive = true
            };

            _repository.SilaCulinaryMaterialRequest.Create(entity);
            if (input.Links != null && input.Links.Count > 0)
            {
                await AttachLinksToRequestAsync(buyer.Id, entity, input.Links, cancellationToken);
            }

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = "CREATED",
                PreviousStatus = null,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = $"Prepared {entity.RequestType} request for {entity.ProposedDescription}.",
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_PREPARED, $"Prepared {entity.RequestReference} ({entity.RequestType}): {entity.ProposedDescription}.", userId);

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<List<SilaCulinaryMaterialRequestDto>> BulkPrepareMaterialRequestsAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryBulkMaterialRequestDto input, CancellationToken cancellationToken)
        {
            if (input.Items == null || input.Items.Count == 0)
            {
                throw new BadRequestCustomException("No ingredients selected.", "Select at least one unresolved ingredient to prepare material requests.");
            }

            List<IGrouping<string, SilaCulinaryMaterialRequestLinkInputDto>> groups = input.ConsolidateEquivalent
                ? input.Items.GroupBy(i => NormalizeKey(i.IngredientDescription ?? i.IngredientTempRef)).ToList()
                : input.Items.Select((item, idx) => new { Key = $"{idx}_{item.IngredientTempRef}", item })
                    .GroupBy(x => x.Key, x => x.item).ToList();

            List<SilaCulinaryMaterialRequestDto> results = new();
            foreach (IGrouping<string, SilaCulinaryMaterialRequestLinkInputDto> g in groups)
            {
                SilaCulinaryMaterialRequestLinkInputDto first = g.First();
                string desc = string.IsNullOrWhiteSpace(first.IngredientDescription) ? first.IngredientTempRef : first.IngredientDescription.Trim();
                SilaCulinaryMaterialRequestAssistResultDto assist = await AssistMaterialRequestAsync(
                    organizationId,
                    new SilaCulinaryMaterialRequestAssistInputDto
                    {
                        IngredientDescription = desc,
                        RequiredQty = g.Sum(x => x.RequiredQuantity),
                        RecipeUom = first.RecipeUom,
                        DraftId = first.DraftId,
                        PropertyId = input.PropertyId
                    },
                    cancellationToken);

                SilaCulinaryDuplicateCandidateDto? topCandidate = assist.PotentialDuplicates.FirstOrDefault();
                string recType = assist.RecommendedRequestType;
                bool overrideDup = recType == "NEW_MATERIAL"; // allow draft preparation with flagged candidates for master data review

                SilaCulinaryMaterialRequestDto created = await CreateMaterialRequestAsync(
                    organizationId, userId, roleId, actorName,
                    new SilaCulinaryMaterialRequestUpsertDto
                    {
                        RequestType = recType,
                        ProposedDescription = string.IsNullOrWhiteSpace(assist.ProposedDescription) ? desc : assist.ProposedDescription,
                        LongDescription = assist.ProposedLongDescription,
                        IngredientCategory = assist.IngredientCategory,
                        MaterialGroup = assist.ProposedMaterialGroup,
                        ProductSpecification = assist.ProductSpecification,
                        StorageRequirements = assist.StorageRequirement,
                        PotentialAllergens = assist.PotentialAllergens.Count > 0 ? string.Join(", ", assist.PotentialAllergens) : null,
                        PropertyId = input.PropertyId,
                        BaseUom = assist.ProposedBaseUom,
                        RequiredQuantity = g.Sum(x => x.RequiredQuantity),
                        BusinessJustification = input.BusinessJustification ?? assist.BusinessJustification,
                        TargetMaterialId = recType is "MATERIAL_EXTENSION" or "MATERIAL_REACTIVATION" ? topCandidate?.MaterialId : null,
                        TargetMaterialCode = recType is "MATERIAL_EXTENSION" or "MATERIAL_REACTIVATION" ? topCandidate?.MaterialCode : null,
                        DataOrigin = "AI_SUGGESTED",
                        AllowDuplicateOverride = overrideDup,
                        Links = g.ToList()
                    },
                    cancellationToken);

                results.Add(created);
            }

            return results;
        }

        public async Task<SilaCulinaryMaterialRequestDto> UpdateMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);

            if (entity.Status is "CREATION_IN_PROGRESS" or "MATERIAL_AVAILABLE" or "RESOLVED" or "CANCELLED")
            {
                throw new BadRequestCustomException("Request locked.", $"Material request {entity.RequestReference} in status {entity.Status} cannot be edited.");
            }

            string prevStatus = entity.Status;
            if (!string.IsNullOrWhiteSpace(input.RequestType)) entity.RequestType = NormalizeRequestType(input.RequestType);
            if (!string.IsNullOrWhiteSpace(input.ProposedDescription)) entity.ProposedDescription = input.ProposedDescription.Trim();
            entity.LongDescription = TrimOrNull(input.LongDescription, 1000) ?? entity.LongDescription;
            entity.IngredientCategory = TrimOrNull(input.IngredientCategory, 80) ?? entity.IngredientCategory;
            entity.MaterialType = TrimOrNull(input.MaterialType, 60) ?? entity.MaterialType;
            entity.MaterialGroup = TrimOrNull(input.MaterialGroup, 80) ?? entity.MaterialGroup;
            entity.ProductHierarchy = TrimOrNull(input.ProductHierarchy, 120) ?? entity.ProductHierarchy;
            entity.Brand = TrimOrNull(input.Brand, 120) ?? entity.Brand;
            entity.Manufacturer = TrimOrNull(input.Manufacturer, 160) ?? entity.Manufacturer;
            entity.ProductSpecification = TrimOrNull(input.ProductSpecification, 1000) ?? entity.ProductSpecification;
            entity.Barcode = TrimOrNull(input.Barcode, 80) ?? entity.Barcode;
            entity.CountryOfOrigin = TrimOrNull(input.CountryOfOrigin, 80) ?? entity.CountryOfOrigin;
            if (input.PropertyId.HasValue)
            {
                entity.PropertyId = input.PropertyId;
                (string? pn, string? cc, string? pc) = await ResolvePropertyOrgAsync(buyer.Id, input.PropertyId, cancellationToken);
                entity.PropertyName = pn;
                entity.CompanyCode = TrimOrNull(input.CompanyCode, 40) ?? cc;
                entity.PlantCode = TrimOrNull(input.PlantCode, 40) ?? pc;
            }
            if (!string.IsNullOrWhiteSpace(input.BaseUom)) entity.BaseUom = NormalizeUom(input.BaseUom);
            if (input.PurchasingUom != null) entity.PurchasingUom = string.IsNullOrWhiteSpace(input.PurchasingUom) ? null : NormalizeUom(input.PurchasingUom);
            if (input.AlternateUom != null) entity.AlternateUom = string.IsNullOrWhiteSpace(input.AlternateUom) ? null : NormalizeUom(input.AlternateUom);
            entity.PackSize = input.PackSize ?? entity.PackSize;
            entity.ConversionNumerator = input.ConversionNumerator ?? entity.ConversionNumerator;
            entity.ConversionDenominator = input.ConversionDenominator ?? entity.ConversionDenominator;
            entity.StorageRequirements = TrimOrNull(input.StorageRequirements, 200) ?? entity.StorageRequirements;
            entity.PotentialAllergens = TrimOrNull(input.PotentialAllergens, 500) ?? entity.PotentialAllergens;
            entity.BusinessJustification = TrimOrNull(input.BusinessJustification, 1000) ?? entity.BusinessJustification;
            entity.SuggestedSupplier = TrimOrNull(input.SuggestedSupplier, 200) ?? entity.SuggestedSupplier;
            entity.TargetMaterialId = input.TargetMaterialId ?? entity.TargetMaterialId;
            entity.TargetMaterialCode = TrimOrNull(input.TargetMaterialCode, 80) ?? entity.TargetMaterialCode;
            if (input.Attachments != null) entity.AttachmentsJson = JsonSerializer.Serialize(input.Attachments, CamelJson);
            if (!string.IsNullOrWhiteSpace(input.DataOrigin)) entity.DataOrigin = NormalizeOrigin(input.DataOrigin);

            // Editing invalidates any prior approval so approvers never approve stale data
            entity.RevisionNumber += 1;
            if (entity.Status == "APPROVED")
            {
                entity.Status = "DRAFT";
                entity.ApprovedRevisionNumber = null;
            }

            List<SilaCulinaryDuplicateCandidateDto> dupCandidates = await RunDuplicateScreeningAsync(
                buyer.Id, entity.ProposedDescription, entity.Barcode, entity.PropertyId, entity.RequestType, entity.TargetMaterialId, cancellationToken);
            entity.DuplicateCheckJson = JsonSerializer.Serialize(dupCandidates, CamelJson);

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = "UPDATED",
                PreviousStatus = prevStatus,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = $"Updated revision {entity.RevisionNumber}.",
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<SilaCulinaryMaterialRequestDto> SubmitMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);

            if (entity.Status is not ("DRAFT" or "VALIDATION_REQUIRED" or "READY_FOR_SUBMISSION" or "RETURNED_FOR_CORRECTION"))
            {
                throw new BadRequestCustomException("Cannot submit request.", $"Request {entity.RequestReference} is in status {entity.Status}.");
            }

            // Re-run duplicate screening right before submission (§10)
            List<SilaCulinaryDuplicateCandidateDto> dups = await RunDuplicateScreeningAsync(
                buyer.Id, entity.ProposedDescription, entity.Barcode, entity.PropertyId, entity.RequestType, entity.TargetMaterialId, cancellationToken);
            entity.DuplicateCheckJson = JsonSerializer.Serialize(dups, CamelJson);

            string prevStatus = entity.Status;
            entity.Status = "PENDING_APPROVAL";
            entity.SubmittedOnUtc = DateTime.UtcNow;

            // Reuse existing SilaApprovals flow if MATERIAL_REQUEST flow is configured; otherwise fall back to authorized approver queue
            bool hasFlow = await _repository.MasterApprovalFlow
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Type != null && x.Type.ToUpper() == Common.SILA_APPROVAL_TYPE_MATERIAL_REQUEST)
                .AnyAsync(cancellationToken);

            if (hasFlow)
            {
                ApprovalScope scope = new() { PropertyId = entity.PropertyId, CompanyCode = entity.CompanyCode };
                await SilaApprovals.StartAsync(
                    _repository, _logger, buyer.Id, Common.SILA_APPROVAL_TYPE_MATERIAL_REQUEST,
                    Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id, entity.RevisionNumber, scope, cancellationToken);
            }
            else
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                {
                    TaskType = "MATERIAL_REQUEST_APPROVAL",
                    DeduplicationKey = $"MRQ_APPR_{entity.Id}_{entity.RevisionNumber}",
                    Title = $"Approve Material Request {entity.RequestReference}: {entity.ProposedDescription}",
                    Description = $"{entity.RequestType} requested by {actorName}.",
                    Priority = SilaOperations.PRIORITY_ACTION,
                    SourceModule = "CULINARY_INTELLIGENCE",
                    SourceDocumentType = Common.SILA_REF_CULINARY_MATERIAL_REQUEST,
                    SourceDocumentId = entity.Id,
                    SourceDocumentNumber = entity.RequestReference,
                    AssignedPermission = "MANAGE_SILA_MASTER_DATA"
                }, cancellationToken);
            }

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = "SUBMITTED",
                PreviousStatus = prevStatus,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = $"Submitted revision {entity.RevisionNumber} for Master Data approval.",
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_SUBMITTED, $"Submitted {entity.RequestReference} ({entity.RequestType}).", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_DUP_CHECK, $"Duplicate check completed ({dups.Count} candidate(s)).", userId);

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<SilaCulinaryMaterialRequestDto> DecideMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestDecisionDto input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);

            string decision = (input.Decision ?? "APPROVE").Trim().ToUpperInvariant();
            if (decision == "CANCEL")
            {
                if (entity.Status is "MATERIAL_AVAILABLE" or "RESOLVED")
                {
                    throw new BadRequestCustomException("Cannot cancel.", "Completed material requests cannot be cancelled.");
                }
                string prev = entity.Status;
                entity.Status = "CANCELLED";
                entity.DecisionComment = TrimOrNull(input.Comment, 500);
                _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MaterialRequestId = entity.Id,
                    Action = "CANCELLED",
                    PreviousStatus = prev,
                    NewStatus = entity.Status,
                    ActorUserId = userId,
                    ActorName = actorName,
                    Comment = entity.DecisionComment,
                    IsActive = true
                });
                await _repository.SaveAsync();
                return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
            }

            if (entity.Status is not ("PENDING_APPROVAL" or "PENDING_REVIEW" or "SUBMITTED"))
            {
                throw new BadRequestCustomException("Not awaiting approval.", $"Request {entity.RequestReference} is in status {entity.Status}.");
            }

            // Separation of duties (§12): requestor cannot self-approve unless administrator
            if (decision == "APPROVE" && entity.RequestedByUserId == userId && !SilaAccess.IsAdministrator(roleId))
            {
                throw new ForBiddenCustomException("Separation of duties.", "The requester cannot approve their own material request.");
            }

            // Check if SilaApprovals steps exist for this revision
            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.ReferenceType == Common.SILA_REF_CULINARY_MATERIAL_REQUEST
                    && x.ReferenceId == entity.Id && x.Version == entity.RevisionNumber && x.IsActive && x.Status == Common.SILA_APPROVAL_PENDING)
                .AnyAsync(cancellationToken);

            string prevStatus = entity.Status;
            string actionName;
            string auditAction;

            if (decision == "APPROVE")
            {
                if (hasSteps)
                {
                    ApprovalDecision stepRes = await SilaApprovals.DecideAsync(
                        _repository, _logger, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id, entity.RevisionNumber,
                        userId, true, input.Comment, cancellationToken, SilaAccess.IsAdministrator(roleId));
                    if (stepRes.Outcome == SilaApprovals.OUTCOME_APPROVED)
                    {
                        entity.Status = "APPROVED";
                        entity.ApprovedRevisionNumber = entity.RevisionNumber;
                        entity.ApprovedOnUtc = DateTime.UtcNow;
                    }
                    actionName = stepRes.Outcome == SilaApprovals.OUTCOME_APPROVED ? "APPROVED" : $"APPROVED_LEVEL_{stepRes.Level}";
                }
                else
                {
                    entity.Status = "APPROVED";
                    entity.ApprovedRevisionNumber = entity.RevisionNumber;
                    entity.ApprovedOnUtc = DateTime.UtcNow;
                    actionName = "APPROVED";
                }
                auditAction = Common.SILA_AUDIT_CULINARY_MRQ_APPROVED;
            }
            else if (decision is "RETURN" or "CLARIFY")
            {
                entity.Status = "RETURNED_FOR_CORRECTION";
                actionName = decision == "RETURN" ? "RETURNED_FOR_CORRECTION" : "CLARIFICATION_REQUESTED";
                auditAction = Common.SILA_AUDIT_CULINARY_MRQ_RETURNED;
            }
            else
            {
                if (hasSteps)
                {
                    await SilaApprovals.DecideAsync(
                        _repository, _logger, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id, entity.RevisionNumber,
                        userId, false, input.Comment, cancellationToken, SilaAccess.IsAdministrator(roleId));
                }
                entity.Status = "REJECTED";
                actionName = "REJECTED";
                auditAction = Common.SILA_AUDIT_CULINARY_MRQ_REJECTED;
            }

            entity.DecisionComment = TrimOrNull(input.Comment, 500);
            if (!string.IsNullOrWhiteSpace(input.TargetMaterialCode))
            {
                entity.TargetMaterialCode = input.TargetMaterialCode.Trim();
            }

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = actionName,
                PreviousStatus = prevStatus,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = entity.DecisionComment,
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                auditAction, $"{entity.RequestReference} {actionName}: {entity.DecisionComment ?? "OK"}", userId);

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<SilaCulinaryMaterialRequestDto> ResubmitMaterialRequestAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryMaterialRequestUpsertDto? updates, CancellationToken cancellationToken)
        {
            if (updates != null)
            {
                await UpdateMaterialRequestAsync(organizationId, userId, roleId, actorName, requestId, updates, cancellationToken);
            }
            return await SubmitMaterialRequestAsync(organizationId, userId, roleId, actorName, requestId, cancellationToken);
        }

        public async Task<SilaCulinaryMaterialRequestDto> ExecuteMaterialCreationAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            string? idempotencyKey, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);

            // Idempotent return if already completed or pending ERP manual fulfillment (§16)
            if (entity.Status is "MATERIAL_AVAILABLE" or "RESOLVED" or "CREATION_PENDING")
            {
                return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
            }

            if (entity.Status == "ERP_OUTCOME_UNKNOWN")
            {
                throw new ConflictCustomException(
                    "ERP outcome unknown.",
                    $"Reconcile {entity.RequestReference} via manual ERP material code verification before retrying creation.");
            }

            if (entity.Status != "APPROVED" || entity.ApprovedRevisionNumber != entity.RevisionNumber)
            {
                throw new BadRequestCustomException(
                    "Approval required.",
                    $"Material request {entity.RequestReference} must have an active approval on revision {entity.RevisionNumber} before creation.");
            }

            string prevStatus = entity.Status;
            entity.ExecutionReference ??= $"EXEC-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
            if (!string.IsNullOrWhiteSpace(idempotencyKey)) entity.IdempotencyKey = idempotencyKey.Trim();

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_EXEC_STARTED, $"Material execution requested for {entity.RequestReference}.", userId);

            (bool isErpTenant, bool hasErpCreationApi) = await CheckErpMaterialCapabilityAsync(buyer.OrganizationId, cancellationToken);

            if (isErpTenant && !hasErpCreationApi)
            {
                // §14: ERP-integrated tenant with no POST_MATERIAL API -> controlled manual ERP fulfillment
                entity.Status = "CREATION_PENDING";
                entity.ExecutionStatus = "PENDING_ERP_MANUAL";

                await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                {
                    TaskType = "MANUAL_ERP_MATERIAL_CREATION",
                    DeduplicationKey = $"MRQ_ERP_MANUAL_{entity.Id}",
                    Title = $"Create/Maintain Material in SAP ERP: {entity.RequestReference}",
                    Description = $"{entity.RequestType} — {entity.ProposedDescription} ({entity.BaseUom}). Enter resulting SAP Material Code in SILA once created.",
                    Priority = SilaOperations.PRIORITY_CRITICAL,
                    SourceModule = "CULINARY_INTELLIGENCE",
                    SourceDocumentType = Common.SILA_REF_CULINARY_MATERIAL_REQUEST,
                    SourceDocumentId = entity.Id,
                    SourceDocumentNumber = entity.RequestReference,
                    AssignedPermission = "MANAGE_SILA_MASTER_DATA"
                }, cancellationToken);

                _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MaterialRequestId = entity.Id,
                    Action = "ERP_MANUAL_FULFILLMENT_REQUIRED",
                    PreviousStatus = prevStatus,
                    NewStatus = entity.Status,
                    ActorUserId = userId,
                    ActorName = actorName,
                    Comment = "Tenant uses ERP Material Master (GET_MATERIAL active; no SAP POST_MATERIAL API configured). Moved to CREATION_PENDING for authorized Master Data entry.",
                    CorrelationId = entity.CorrelationId,
                    IsActive = true
                });

                await _repository.SaveAsync();
                return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
            }

            // §15: Standalone SILA tenant — execute authorized local Material Master creation / maintenance
            ItemBuyerMaster resulting = await ExecuteStandaloneMaterialActionAsync(buyer, entity, userId, cancellationToken);
            entity.ResultingMaterialId = resulting.Id;
            entity.ResultingMaterialCode = resulting.MaterialCode;
            entity.Status = "MATERIAL_AVAILABLE";
            entity.ExecutionStatus = "SUCCEEDED";
            entity.MaterialAvailableOnUtc = DateTime.UtcNow;

            int reconnected = await ReconnectWaitingDraftsAsync(buyer.Id, entity, resulting, userId, cancellationToken);

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = "STANDALONE_MATERIAL_COMPLETED",
                PreviousStatus = prevStatus,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = $"Material {resulting.MaterialCode} ({resulting.Description}) available. Reconnected {reconnected} waiting draft ingredient(s).",
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_CREATED, $"Standalone material {resulting.MaterialCode} created/updated.", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_SYNCED, $"Material {resulting.MaterialCode} available and {reconnected} recipe link(s) refreshed.", userId);

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<SilaCulinaryMaterialRequestDto> VerifyManualErpMaterialAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid requestId,
            SilaCulinaryManualErpVerifyDto input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.MaterialCode))
            {
                throw new BadRequestCustomException("Material code required.", "Enter the authoritative SAP / ERP Material Code created for this request.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryMaterialRequest entity = await LoadTrackedRequestAsync(buyer.Id, requestId);

            string rawCode = input.MaterialCode.Trim();
            string trimmedZeros = rawCode.TrimStart('0');
            List<ItemBuyerMaster> candidates = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            ItemBuyerMaster? verified = candidates.FirstOrDefault(m =>
                string.Equals(m.MaterialCode, rawCode, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(trimmedZeros) && string.Equals(m.MaterialCode.TrimStart('0'), trimmedZeros, StringComparison.OrdinalIgnoreCase)));

            if (verified == null)
            {
                throw new NotFoundCustomException(
                    "Authoritative material not found.",
                    $"Material code '{rawCode}' is not present or not active in Material Master. Run GET_MATERIAL sync or verify the code first.");
            }

            string prevStatus = entity.Status;
            entity.ResultingMaterialId = verified.Id;
            entity.ResultingMaterialCode = verified.MaterialCode;
            entity.Status = "MATERIAL_AVAILABLE";
            entity.ExecutionStatus = "SUCCEEDED";
            entity.MaterialAvailableOnUtc = DateTime.UtcNow;

            int reconnected = input.AutoReconnectDrafts
                ? await ReconnectWaitingDraftsAsync(buyer.Id, entity, verified, userId, cancellationToken)
                : 0;

            _repository.SilaCulinaryMaterialRequestHistory.Create(new SilaCulinaryMaterialRequestHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialRequestId = entity.Id,
                Action = "ERP_MATERIAL_VERIFIED",
                PreviousStatus = prevStatus,
                NewStatus = entity.Status,
                ActorUserId = userId,
                ActorName = actorName,
                Comment = $"Verified ERP material {verified.MaterialCode} ({verified.Description}). {input.Comment} Reconnected {reconnected} link(s).",
                CorrelationId = entity.CorrelationId,
                IsActive = true
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, entity.Id,
                Common.SILA_AUDIT_CULINARY_MRQ_SYNCED, $"Verified ERP material {verified.MaterialCode} and reconnected {reconnected} waiting recipe line(s).", userId);

            await _repository.SaveAsync();
            return (await HydrateMaterialRequestsAsync(buyer, new List<SilaCulinaryMaterialRequest> { entity }, cancellationToken)).First();
        }

        public async Task<SilaCulinaryMaterialRequestDashboardDto> GetMaterialRequestDashboardAsync(
            Guid organizationId, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            IQueryable<SilaCulinaryMaterialRequest> q = _repository.SilaCulinaryMaterialRequest
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId.Value);

            List<SilaCulinaryMaterialRequest> all = await q.ToListAsync(cancellationToken);
            List<Guid> openReqIds = all.Where(r => ActiveRequestStatuses.Contains(r.Status)).Select(r => r.Id).ToList();
            int blockedDrafts = openReqIds.Count > 0
                ? await _repository.SilaCulinaryMaterialRequestLink
                    .FindByCondition(l => l.BuyerId == buyer.Id && l.IsActive && openReqIds.Contains(l.MaterialRequestId))
                    .Select(l => l.DraftId)
                    .Distinct()
                    .CountAsync(cancellationToken)
                : 0;

            List<double> resolvedHours = all
                .Where(r => r.MaterialAvailableOnUtc.HasValue)
                .Select(r => (r.MaterialAvailableOnUtc!.Value - r.DateCreated).TotalHours)
                .Where(h => h >= 0)
                .ToList();

            (bool isErp, bool hasCreateApi) = await CheckErpMaterialCapabilityAsync(buyer.OrganizationId, cancellationToken);

            DateTime now = DateTime.UtcNow;
            Dictionary<string, int> aging = new()
            {
                ["0-24h"] = all.Count(r => ActiveRequestStatuses.Contains(r.Status) && (now - r.DateCreated).TotalHours <= 24),
                ["1-3d"] = all.Count(r => ActiveRequestStatuses.Contains(r.Status) && (now - r.DateCreated).TotalHours is > 24 and <= 72),
                ["3-7d"] = all.Count(r => ActiveRequestStatuses.Contains(r.Status) && (now - r.DateCreated).TotalHours is > 72 and <= 168),
                ["7d+"] = all.Count(r => ActiveRequestStatuses.Contains(r.Status) && (now - r.DateCreated).TotalHours > 168),
            };

            return new SilaCulinaryMaterialRequestDashboardDto
            {
                TotalRequests = all.Count,
                DraftRequests = all.Count(r => r.Status is "DRAFT" or "VALIDATION_REQUIRED" or "READY_FOR_SUBMISSION"),
                PendingApprovalRequests = all.Count(r => r.Status is "SUBMITTED" or "PENDING_REVIEW" or "PENDING_APPROVAL"),
                ReturnedRequests = all.Count(r => r.Status == "RETURNED_FOR_CORRECTION"),
                ApprovedRequests = all.Count(r => r.Status == "APPROVED"),
                CreationPendingRequests = all.Count(r => r.Status is "CREATION_PENDING" or "CREATION_IN_PROGRESS"),
                CreationFailedRequests = all.Count(r => r.Status is "CREATION_FAILED" or "ERP_OUTCOME_UNKNOWN"),
                SynchronizationPendingRequests = all.Count(r => r.Status is "MATERIAL_CREATED" or "SYNCHRONIZATION_PENDING"),
                ResolvedRequests = all.Count(r => r.Status is "MATERIAL_AVAILABLE" or "RESOLVED"),
                RecipesBlockedByMissingMaterials = blockedDrafts,
                AverageResolutionHours = resolvedHours.Count > 0 ? Math.Round(resolvedHours.Average(), 1) : null,
                IsErpIntegratedTenant = isErp,
                HasErpCreationApi = hasCreateApi,
                ByType = all.GroupBy(r => r.RequestType).ToDictionary(g => g.Key, g => g.Count()),
                AgingByStage = aging
            };
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportMaterialRequestsReportAsync(
            Guid organizationId, string format, string? status, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequestDto> rows = await ListMaterialRequestsAsync(
                organizationId, Guid.Empty, Common.BUYER_ADMIN_ROLE_ID, status, null, null, null, null, cancellationToken);

            string fmt = (format ?? "XLSX").Trim().ToUpperInvariant();
            if (fmt == "CSV")
            {
                StringBuilder sb = new();
                sb.AppendLine("Reference,Type,Status,Description,BaseUOM,Plant,LinkedDrafts,ResultingMaterial,RequestedBy,CreatedUtc");
                foreach (SilaCulinaryMaterialRequestDto r in rows)
                {
                    sb.AppendLine(string.Join(",",
                        CsvCell(r.RequestReference),
                        CsvCell(r.RequestType),
                        CsvCell(r.Status),
                        CsvCell(r.ProposedDescription),
                        CsvCell(r.BaseUom),
                        CsvCell(r.PlantCode ?? r.PropertyName ?? ""),
                        r.Links.Count.ToString(CultureInfo.InvariantCulture),
                        CsvCell(r.ResultingMaterialCode ?? r.TargetMaterialCode ?? ""),
                        CsvCell(r.RequestedByName ?? ""),
                        CsvCell(r.DateCreated.ToString("O", CultureInfo.InvariantCulture))));
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "material-requests-report.csv");
            }

            if (fmt == "PDF")
            {
                List<string> lines = rows.Select(r =>
                    $"{r.RequestReference} | {r.RequestType} | {r.Status} | {r.ProposedDescription} | {r.BaseUom} | Links:{r.Links.Count} | Mat:{r.ResultingMaterialCode ?? "-"}").ToList();
                return (BuildSimpleTextPdf("SILA Material Creation Request Report", lines), "application/pdf", "material-requests-report.pdf");
            }

            using XLWorkbook wb = new();
            IXLWorksheet sheet = wb.Worksheets.Add("Material Requests");
            string[] headers = { "Reference", "Type", "Status", "Proposed Description", "Base UOM", "Material Group", "Property", "Plant", "Linked Recipes", "Resulting Material", "Requested By", "Created UTC" };
            for (int c = 0; c < headers.Length; c++)
            {
                IXLCell cell = sheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                SilaCulinaryMaterialRequestDto r = rows[i];
                int row = i + 2;
                sheet.Cell(row, 1).Value = SafeSpreadsheetCell(r.RequestReference);
                sheet.Cell(row, 2).Value = SafeSpreadsheetCell(r.RequestType);
                sheet.Cell(row, 3).Value = SafeSpreadsheetCell(r.Status);
                sheet.Cell(row, 4).Value = SafeSpreadsheetCell(r.ProposedDescription);
                sheet.Cell(row, 5).Value = SafeSpreadsheetCell(r.BaseUom);
                sheet.Cell(row, 6).Value = SafeSpreadsheetCell(r.MaterialGroup);
                sheet.Cell(row, 7).Value = SafeSpreadsheetCell(r.PropertyName);
                sheet.Cell(row, 8).Value = SafeSpreadsheetCell(r.PlantCode);
                sheet.Cell(row, 9).Value = r.Links.Count;
                sheet.Cell(row, 10).Value = SafeSpreadsheetCell(r.ResultingMaterialCode ?? r.TargetMaterialCode);
                sheet.Cell(row, 11).Value = SafeSpreadsheetCell(r.RequestedByName);
                sheet.Cell(row, 12).Value = r.DateCreated.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
            sheet.Columns().AdjustToContents();
            using MemoryStream ms = new();
            wb.SaveAs(ms);
            return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "material-requests-report.xlsx");
        }

        // =========================================================================
        // 2. RECIPE COSTING, SCENARIOS, SNAPSHOTS & OPTIMIZATION (§19 - §38)
        // =========================================================================

        public async Task<SilaCulinaryCostingResultDto> CalculateDraftCostingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryCostingCalculateRequestDto input, CancellationToken cancellationToken)
        {
            bool canViewCost = SilaAccess.HasFullAccess(roleId);
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);

            SilaCulinaryCostingResultDto result = await ComputeDeterministicCostingAsync(
                buyer, draft, payload, costCfg, canViewCost, input, cancellationToken);

            if (input.SaveSnapshot && canViewCost)
            {
                string costRef = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.COSTING_SNAPSHOT, 8, cancellationToken);
                SilaCulinaryCostingSnapshot snap = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    CostingReference = costRef,
                    DraftId = draft.Id,
                    DraftRevision = draft.RevisionNumber,
                    PropertyId = result.PropertyId,
                    ScenarioName = string.IsNullOrWhiteSpace(input.ScenarioName) ? "Baseline" : input.ScenarioName.Trim(),
                    IsBaseline = input.IsBaseline,
                    IsApprovedSnapshot = false,
                    ApprovalStatus = "DRAFT",
                    CostingBasis = result.CostingBasis,
                    TaxBasis = result.TaxBasis,
                    Currency = result.Currency,
                    Servings = result.Servings,
                    TotalIngredientCost = result.TotalIngredientCost,
                    SubRecipeCost = result.SubRecipeCost,
                    AdditionalDirectCost = result.AdditionalDirectCost,
                    AllocatedOverheadCost = result.AllocatedOverheadCost,
                    TotalRecipeCost = result.TotalRecipeCost,
                    IngredientCostPerPortion = result.IngredientCostPerPortion,
                    TotalCostPerPortion = result.TotalCostPerPortion,
                    SellingPricePerPortion = result.SellingPricePerPortion,
                    FoodCostPercent = result.FoodCostPercent,
                    TargetFoodCostPercent = result.TargetFoodCostPercent,
                    RecommendedSellingPrice = result.RecommendedSellingPriceNet,
                    EstimatedRevenue = result.EstimatedRevenue,
                    ContributionAmount = result.ContributionAmount,
                    CostCompleteness = result.CostCompleteness,
                    MissingPriceCount = result.MissingPriceCount,
                    LinesJson = JsonSerializer.Serialize(result.Lines, CamelJson),
                    AdditionalCostsJson = JsonSerializer.Serialize(result.AdditionalCosts, CamelJson),
                    AssumptionsJson = JsonSerializer.Serialize(new { input.GlobalBufferPercent, input.GlobalPriceIncreasePercent, input.PortionSize }, CamelJson),
                    CreatedByName = actorName,
                    IsActive = true
                };
                _repository.SilaCulinaryCostingSnapshot.Create(snap);

                // Link baseline snapshot onto lifecycle if not already approved
                SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive);
                if (lc != null && input.IsBaseline && lc.LifecycleStatus is "DRAFT" or "READINESS_PENDING")
                {
                    lc.CostingSnapshotId = snap.Id;
                }

                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_COSTING, snap.Id,
                    input.IsBaseline ? Common.SILA_AUDIT_CULINARY_COST_SNAPSHOT : Common.SILA_AUDIT_CULINARY_COST_SCENARIO,
                    $"Saved {snap.CostingReference} ({snap.ScenarioName}): {snap.Currency} {snap.TotalIngredientCost:0.00} ({snap.CostCompleteness}).", userId);

                await _repository.SaveAsync();
                result.SnapshotId = snap.Id;
                result.CostingReference = snap.CostingReference;
            }
            else
            {
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, draft.Id,
                    Common.SILA_AUDIT_CULINARY_COST_CALC,
                    $"Calculated recipe costing for {draft.DraftReference}: {result.Currency} {result.TotalIngredientCost:0.00}.", userId);
                await _repository.SaveAsync();
            }

            return result;
        }

        public async Task<List<SilaCulinaryCostingResultDto>> ListCostingSnapshotsAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            if (!SilaAccess.HasFullAccess(roleId))
            {
                throw new ForBiddenCustomException("Cost visibility restricted.", "You do not have permission to view detailed costing snapshots.");
            }
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            _ = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);

            List<SilaCulinaryCostingSnapshot> snaps = await _repository.SilaCulinaryCostingSnapshot
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draftId && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            return snaps.Select(s => new SilaCulinaryCostingResultDto
            {
                SnapshotId = s.Id,
                CostingReference = s.CostingReference,
                DraftId = draftId,
                DraftRevision = s.DraftRevision ?? 1,
                ScenarioName = s.ScenarioName,
                IsBaseline = s.IsBaseline,
                IsApprovedSnapshot = s.IsApprovedSnapshot,
                ApprovalStatus = s.ApprovalStatus,
                CostingBasis = s.CostingBasis,
                TaxBasis = s.TaxBasis,
                Currency = s.Currency,
                Servings = s.Servings,
                TotalIngredientCost = s.TotalIngredientCost,
                SubRecipeCost = s.SubRecipeCost,
                AdditionalDirectCost = s.AdditionalDirectCost,
                AllocatedOverheadCost = s.AllocatedOverheadCost,
                TotalRecipeCost = s.TotalRecipeCost,
                IngredientCostPerPortion = s.IngredientCostPerPortion,
                TotalCostPerPortion = s.TotalCostPerPortion,
                SellingPricePerPortion = s.SellingPricePerPortion,
                FoodCostPercent = s.FoodCostPercent,
                TargetFoodCostPercent = s.TargetFoodCostPercent,
                RecommendedSellingPriceNet = s.RecommendedSellingPrice,
                EstimatedRevenue = s.EstimatedRevenue,
                ContributionAmount = s.ContributionAmount,
                CostCompleteness = s.CostCompleteness,
                MissingPriceCount = s.MissingPriceCount,
                CanViewCostDetail = true,
                Lines = DeserializeSafe<List<SilaCulinaryCostedLineDto>>(s.LinesJson) ?? new(),
                AdditionalCosts = DeserializeSafe<List<SilaCulinaryAdditionalCostInputDto>>(s.AdditionalCostsJson) ?? new(),
                CalculatedAtUtc = s.DateCreated
            }).ToList();
        }

        public async Task<SilaCulinaryCostOptimizationResultDto> OptimizeDraftCostingAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryCostOptimizationRequestDto input, CancellationToken cancellationToken)
        {
            if (!SilaAccess.HasFullAccess(roleId))
            {
                throw new ForBiddenCustomException("Cost visibility restricted.", "Cost optimization requires cost viewing permissions.");
            }

            SilaCulinaryCostingCalculateRequestDto baseReq = input.CostingRequest ?? new SilaCulinaryCostingCalculateRequestDto();
            SilaCulinaryCostingResultDto baseline = await CalculateDraftCostingAsync(
                organizationId, userId, roleId, actorName, draftId, baseReq, cancellationToken);

            List<SilaCulinaryCostOptimizationSuggestionDto> suggestions = new();
            List<SilaCulinaryCostedLineDto> ranked = baseline.Lines
                .Where(l => (l.IngredientCost ?? 0m) > 0m)
                .OrderByDescending(l => l.IngredientCost ?? 0m)
                .ToList();

            if (ranked.Count > 0)
            {
                SilaCulinaryCostedLineDto top = ranked[0];
                decimal savings = Math.Round((top.IngredientCost ?? 0m) * 0.10m, 4, MidpointRounding.AwayFromZero);
                decimal projectedIngCost = Math.Max(0m, baseline.TotalIngredientCost - savings);
                decimal? projFc = baseline.SellingPricePerPortion > 0 && baseline.Servings > 0
                    ? Math.Round((projectedIngCost / baseline.Servings) / baseline.SellingPricePerPortion.Value * 100m, 2, MidpointRounding.AwayFromZero)
                    : null;

                suggestions.Add(new SilaCulinaryCostOptimizationSuggestionDto
                {
                    SuggestionId = "OPT-TOP-DRIVER-10PCT",
                    Category = "TOP_COST_DRIVER",
                    Title = $"Optimize {top.Description} portion/yield by 10%",
                    Rationale = $"{top.Description} is the highest cost driver ({top.PercentageOfTotal ?? 0m}% / {baseline.Currency} {top.IngredientCost:0.00}). Deterministic simulation shows 10% quantity/yield optimization saves {baseline.Currency} {savings:0.00}.",
                    TargetTempRef = top.TempRef,
                    ProjectedIngredientCost = projectedIngCost,
                    ProjectedCostSavings = savings,
                    ProjectedFoodCostPercent = projFc,
                    RequiresChefConfirmation = true,
                    AllergenOrSafetyWarning = "Verify flavor parity, allergen profile, and portion spec before applying."
                });
            }

            if (baseline.TargetFoodCostPercent.HasValue && baseline.FoodCostPercent.HasValue && baseline.FoodCostPercent > baseline.TargetFoodCostPercent && baseline.RecommendedSellingPriceNet.HasValue)
            {
                suggestions.Add(new SilaCulinaryCostOptimizationSuggestionDto
                {
                    SuggestionId = "OPT-SELLING-PRICE",
                    Category = "PRICING_ALIGNMENT",
                    Title = $"Align net selling price to {baseline.Currency} {baseline.RecommendedSellingPriceNet:0.00} for {baseline.TargetFoodCostPercent:0.#}% target",
                    Rationale = $"Current food cost {baseline.FoodCostPercent:0.00}% exceeds target {baseline.TargetFoodCostPercent:0.00}%. Advisory only — POS prices are not changed automatically.",
                    ProjectedIngredientCost = baseline.TotalIngredientCost,
                    ProjectedCostSavings = 0m,
                    ProjectedFoodCostPercent = baseline.TargetFoodCostPercent,
                    RequiresChefConfirmation = true
                });
            }

            return new SilaCulinaryCostOptimizationResultDto
            {
                DraftId = draftId,
                ProviderUsed = "LOCAL_DETERMINISTIC",
                Baseline = baseline,
                Suggestions = suggestions,
                Notes = new List<string>
                {
                    "All optimization figures are independently calculated using deterministic Decimal arithmetic.",
                    "Chef confirmation is mandatory before modifying recipe quantities or selling prices."
                }
            };
        }

        public async Task<SilaCulinaryBulkRecostResultDto> BulkRecostAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName,
            SilaCulinaryBulkRecostRequestDto input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            List<Guid> ids = input.DraftIds != null && input.DraftIds.Count > 0
                ? input.DraftIds
                : await _repository.SilaCulinaryRecipeDraft
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "CANCELLED")
                    .Select(x => x.Id)
                    .Take(50)
                    .ToListAsync(cancellationToken);

            SilaCulinaryBulkRecostResultDto bulk = new();
            foreach (Guid id in ids)
            {
                SilaCulinaryCostingResultDto res = await CalculateDraftCostingAsync(
                    organizationId, userId, roleId, actorName, id,
                    new SilaCulinaryCostingCalculateRequestDto
                    {
                        PropertyId = input.PropertyId,
                        ScenarioName = "Recosting Run",
                        SaveSnapshot = input.SaveNewSnapshot,
                        IsBaseline = true
                    },
                    cancellationToken);

                bulk.Results.Add(res);
                bulk.ProcessedCount += 1;
                if (res.MissingPriceCount > 0) bulk.MissingCostCount += 1;
                if (res.FoodCostPercent.HasValue && res.TargetFoodCostPercent.HasValue && res.FoodCostPercent > res.TargetFoodCostPercent)
                {
                    bulk.ExceedingTargetCount += 1;
                    await SilaOperations.UpsertAlertAsync(_repository, buyer.Id, userId, new SilaOperations.AlertDraft
                    {
                        AlertType = "RECIPE_ABOVE_FOOD_COST_TARGET",
                        DeduplicationKey = $"FC_TARGET_{id}",
                        Title = $"Food cost exceeds target: {res.RecipeName}",
                        Message = $"Recosting calculated {res.FoodCostPercent:0.00}% vs target {res.TargetFoodCostPercent:0.00}%.",
                        Severity = SilaOperations.SEVERITY_WARNING,
                        SourceModule = "CULINARY_INTELLIGENCE",
                        SourceDocumentType = Common.SILA_REF_CULINARY_RECIPE_DRAFT,
                        SourceDocumentId = id
                    }, cancellationToken);
                }
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_DRAFT, ids.FirstOrDefault(),
                Common.SILA_AUDIT_CULINARY_RECOSTING,
                $"Bulk recosting processed {bulk.ProcessedCount} recipe(s); {bulk.ExceedingTargetCount} above target, {bulk.MissingCostCount} missing prices.", userId);
            await _repository.SaveAsync();
            return bulk;
        }

        // =========================================================================
        // 3. READINESS, APPROVALS, RECIPE ID, IMAGE, PUBLISHING, VERSIONING & 2A
        // =========================================================================

        public async Task<SilaCulinaryRecipeReadinessDto> GetRecipeReadinessAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);

            return await EvaluateReadinessInternalAsync(buyer, draft, payload, costCfg, cancellationToken);
        }

        public async Task<SilaCulinaryApprovalPackageDto> GetApprovalPackageAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);

            SilaCulinaryRecipeReadinessDto readiness = await EvaluateReadinessInternalAsync(buyer, draft, payload, costCfg, cancellationToken);
            SilaCulinaryCostingResultDto? costing = SilaAccess.HasFullAccess(roleId)
                ? await ComputeDeterministicCostingAsync(buyer, draft, payload, costCfg, true, new SilaCulinaryCostingCalculateRequestDto(), cancellationToken)
                : null;

            List<SilaCulinaryMaterialRequestDto> matRequests = await ListMaterialRequestsAsync(
                organizationId, userId, roleId, null, null, null, draft.Id, null, cancellationToken);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);
            Recipe? prod = lc.ProductionRecipeId.HasValue
                ? await _repository.Recipe.FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == lc.ProductionRecipeId.Value && x.IsActive)
                : null;

            return new SilaCulinaryApprovalPackageDto
            {
                DraftId = draft.Id,
                DraftReference = draft.DraftReference,
                DraftRevision = draft.RevisionNumber,
                RecipeName = draft.RecipeName,
                Description = payload.Header.Description,
                Category = payload.Header.Category,
                Cuisine = payload.Header.Cuisine,
                BaseServings = draft.BaseServings,
                ServingUom = payload.Header.ServingUom,
                Allergens = payload.Header.PotentialAllergens ?? new(),
                DietaryFlags = payload.Header.DietaryTags ?? new(),
                PreparationSteps = payload.Instructions.OrderBy(s => s.StepNumber).Select(s => $"{s.StepNumber}. {s.StepTitle}: {s.Instruction}").ToList(),
                CostingSummary = costing,
                Readiness = readiness,
                MaterialRequests = matRequests,
                Lifecycle = MapLifecycleDto(lc, prod)
            };
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> SubmitRecipeForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);

            SilaCulinaryRecipeReadinessDto readiness = await EvaluateReadinessInternalAsync(buyer, draft, payload, costCfg, cancellationToken);
            if (!readiness.ReadyForApproval)
            {
                string blockers = string.Join("; ", readiness.Checks.Where(c => c.Status == "BLOCKING").Select(c => $"{c.Label}: {c.Detail}"));
                throw new BadRequestCustomException("Recipe not ready for approval.", blockers);
            }

            // Capture baseline costing snapshot bound to this revision
            SilaCulinaryCostingResultDto costing = await ComputeDeterministicCostingAsync(
                buyer, draft, payload, costCfg, true, new SilaCulinaryCostingCalculateRequestDto(), cancellationToken);
            string costRef = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.COSTING_SNAPSHOT, 8, cancellationToken);
            SilaCulinaryCostingSnapshot snap = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                CostingReference = costRef,
                DraftId = draft.Id,
                DraftRevision = draft.RevisionNumber,
                PropertyId = draft.PropertyId,
                ScenarioName = $"Approval Rev {draft.RevisionNumber}",
                IsBaseline = true,
                ApprovalStatus = "PENDING_REVIEW",
                CostingBasis = costing.CostingBasis,
                TaxBasis = costing.TaxBasis,
                Currency = costing.Currency,
                Servings = costing.Servings,
                TotalIngredientCost = costing.TotalIngredientCost,
                SubRecipeCost = costing.SubRecipeCost,
                AdditionalDirectCost = costing.AdditionalDirectCost,
                AllocatedOverheadCost = costing.AllocatedOverheadCost,
                TotalRecipeCost = costing.TotalRecipeCost,
                IngredientCostPerPortion = costing.IngredientCostPerPortion,
                TotalCostPerPortion = costing.TotalCostPerPortion,
                SellingPricePerPortion = costing.SellingPricePerPortion,
                FoodCostPercent = costing.FoodCostPercent,
                TargetFoodCostPercent = costing.TargetFoodCostPercent,
                RecommendedSellingPrice = costing.RecommendedSellingPriceNet,
                EstimatedRevenue = costing.EstimatedRevenue,
                ContributionAmount = costing.ContributionAmount,
                CostCompleteness = costing.CostCompleteness,
                MissingPriceCount = costing.MissingPriceCount,
                LinesJson = JsonSerializer.Serialize(costing.Lines, CamelJson),
                CreatedByName = actorName,
                IsActive = true
            };
            _repository.SilaCulinaryCostingSnapshot.Create(snap);

            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);
            lc.ApprovedDraftRevision = draft.RevisionNumber;
            lc.CostingSnapshotId = snap.Id;
            lc.LifecycleStatus = "PENDING_APPROVAL";
            lc.ApprovalComment = TrimOrNull(input?.Comment, 500);
            lc.ReadinessSummaryJson = JsonSerializer.Serialize(readiness, CamelJson);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                Common.SILA_AUDIT_CULINARY_RCP_SUBMITTED, $"Submitted {draft.DraftReference} (Rev {draft.RevisionNumber}) for approval.", userId);

            await _repository.SaveAsync();
            return MapLifecycleDto(lc, null);
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> DecideRecipeApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            bool approve, SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);

            if (lc.LifecycleStatus != "PENDING_APPROVAL")
            {
                throw new BadRequestCustomException("Recipe not pending approval.", $"Lifecycle status is {lc.LifecycleStatus}.");
            }
            if (lc.ApprovedDraftRevision != draft.RevisionNumber)
            {
                throw new ConflictCustomException(
                    "Approval revision mismatch.",
                    $"Draft was modified to revision {draft.RevisionNumber} after revision {lc.ApprovedDraftRevision} was submitted. Resubmit for approval.");
            }

            if (approve)
            {
                lc.LifecycleStatus = "APPROVED";
                lc.ApprovedByUserId = userId;
                lc.ApprovedByName = actorName;
                lc.ApprovedOnUtc = DateTime.UtcNow;
                lc.ApprovalComment = TrimOrNull(input?.Comment, 500);
                if (lc.CostingSnapshotId.HasValue)
                {
                    SilaCulinaryCostingSnapshot? snap = await _repository.SilaCulinaryCostingSnapshot
                        .FindFirstByConditionAsync(x => x.Id == lc.CostingSnapshotId.Value && x.BuyerId == buyer.Id);
                    if (snap != null)
                    {
                        snap.IsApprovedSnapshot = true;
                        snap.ApprovalStatus = "APPROVED";
                    }
                }
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                    Common.SILA_AUDIT_CULINARY_RCP_APPROVED, $"Approved {draft.DraftReference} Rev {draft.RevisionNumber}.", userId);
            }
            else
            {
                lc.LifecycleStatus = "REJECTED";
                lc.RejectionReason = TrimOrNull(input?.Comment, 500) ?? "Returned/Rejected during recipe review.";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                    Common.SILA_AUDIT_CULINARY_RCP_REJECTED, $"Rejected {draft.DraftReference} Rev {draft.RevisionNumber}: {lc.RejectionReason}", userId);
            }

            await _repository.SaveAsync();
            return MapLifecycleDto(lc, null);
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> CreateProductionRecipeAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);

            // Idempotency (§44): if already created for this draft/key, return existing Recipe ID cleanly
            if (lc.ProductionRecipeId.HasValue)
            {
                Recipe? existingProd = await _repository.Recipe
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == lc.ProductionRecipeId.Value && x.IsActive);
                if (existingProd != null)
                {
                    return MapLifecycleDto(lc, existingProd);
                }
            }

            if (lc.LifecycleStatus != "APPROVED")
            {
                throw new BadRequestCustomException(
                    "Recipe approval required.",
                    "Only an approved recipe draft revision may generate a Production Recipe ID.");
            }
            if (lc.ApprovedDraftRevision != draft.RevisionNumber)
            {
                throw new ConflictCustomException("Revision mismatch.", "Draft changed after approval. Re-approve before creating Production Recipe ID.");
            }

            // Re-validate readiness & confirmed material mappings (§43 & §45)
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);
            SilaCulinaryRecipeReadinessDto readiness = await EvaluateReadinessInternalAsync(buyer, draft, payload, costCfg, cancellationToken);
            if (!readiness.ReadyForProductionId)
            {
                string blockers = string.Join("; ", readiness.Checks.Where(c => c.Status == "BLOCKING").Select(c => $"{c.Label}: {c.Detail}"));
                throw new BadRequestCustomException("Cannot create Production Recipe ID.", blockers);
            }

            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive && x.MappingStatus == "CONFIRMED" && x.MaterialId.HasValue)
                .ToListAsync(cancellationToken);

            // Validate sub-recipe dependencies (§45)
            List<SilaRecipeIngredientWriteDto> recipeIngredients = mappings.Select(m => new SilaRecipeIngredientWriteDto
            {
                MaterialId = m.MaterialId,
                Quantity = m.RecipeQuantity > 0 ? m.RecipeQuantity : 1m,
                Uom = string.IsNullOrWhiteSpace(m.RecipeUom) ? "KG" : m.RecipeUom
            }).ToList();

            List<Guid> linkedSubRecipeIds = new();
            foreach (CulinarySubRecipeDto sub in payload.SubRecipes ?? new())
            {
                if (!string.IsNullOrWhiteSpace(sub.ExistingRecipeReference) && Guid.TryParse(sub.ExistingRecipeReference, out Guid subId))
                {
                    Recipe? subRec = await _repository.Recipe
                        .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == subId && x.IsActive);
                    if (subRec == null)
                    {
                        throw new BadRequestCustomException("Sub-recipe missing.", $"Required sub-recipe {sub.SubRecipeName} was not found in Recipe Master.");
                    }
                    linkedSubRecipeIds.Add(subRec.Id);
                    recipeIngredients.Add(new SilaRecipeIngredientWriteDto
                    {
                        SubRecipeId = subRec.Id,
                        Quantity = sub.RequiredQtyInParent > 0 ? sub.RequiredQtyInParent : 1m,
                        Uom = string.IsNullOrWhiteSpace(sub.RequiredUomInParent) ? subRec.ServingUom : sub.RequiredUomInParent
                    });
                }
            }

            // Reuse existing DocumentNumber.RECIPE generator (RI########) and SilaRecipeRules.ApplyAsync (§43)
            string recipeCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.RECIPE, 8, cancellationToken);
            Recipe prodRecipe = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                RecipeCode = recipeCode,
                Status = Common.SILA_RECIPE_DRAFT,
                Version = 1,
                ActiveVersion = 0,
                IsActive = true
            };

            string stepsText = string.Join("\n", (payload.Instructions ?? new()).OrderBy(s => s.StepNumber)
                .Select(s => $"{s.StepNumber}. {s.StepTitle}: {s.Instruction}"));

            string rawFormat = string.IsNullOrWhiteSpace(input?.RecipeFormat) ? payload.Header.RecipeType : input.RecipeFormat.Trim().ToUpperInvariant();
            string mappedFormat = rawFormat.Trim().ToUpperInvariant() switch
            {
                "SUB_RECIPE" or "SUB" => SilaHospitalityRules.FORMAT_SUB,
                "BATCH" or "BATCH_RECIPE" => SilaHospitalityRules.FORMAT_BATCH,
                "DIRECT" or "DIRECT_ITEM" => SilaHospitalityRules.FORMAT_DIRECT,
                "COMBO" => SilaHospitalityRules.FORMAT_COMBO,
                "VARIANT" or "VARIANT_RECIPE" => SilaHospitalityRules.FORMAT_VARIANT,
                _ => SilaHospitalityRules.FORMAT_STANDARD
            };

            SilaRecipeWriteDto writeDto = new()
            {
                Name = draft.RecipeName,
                Description = payload.Header.Description,
                Category = payload.Header.Category,
                CategoryId = input?.CategoryId,
                ItemMode = string.IsNullOrWhiteSpace(input?.ItemMode) ? "RECIPE" : input.ItemMode.Trim().ToUpperInvariant(),
                ProductClass = string.IsNullOrWhiteSpace(input?.ProductClass) ? "FOOD" : input.ProductClass.Trim().ToUpperInvariant(),
                RecipeFormat = mappedFormat,
                ServingQty = draft.BaseServings > 0 ? draft.BaseServings : 1m,
                ServingUom = string.IsNullOrWhiteSpace(payload.Header.ServingUom) ? "PORTION" : payload.Header.ServingUom,
                SellingUom = "EA",
                Currency = buyer.Currency ?? costCfg.ReportingCurrency,
                PrepTimeMinutes = payload.Header.PrepDurationMinutes,
                CookTimeMinutes = payload.Header.CookDurationMinutes,
                PreparationSteps = stepsText,
                Ingredients = recipeIngredients
            };

            await SilaRecipeRules.ApplyAsync(_repository, _logger, buyer.Id, userId, roleId, prodRecipe, writeDto, cancellationToken);
            _repository.Recipe.Create(prodRecipe);

            lc.ProductionRecipeId = prodRecipe.Id;
            lc.ProductionRecipeCode = prodRecipe.RecipeCode;
            lc.ProductionRecipeVersion = prodRecipe.Version;
            lc.LifecycleStatus = "RECIPE_ID_CREATED";
            lc.IdempotencyKey = TrimOrNull(input?.IdempotencyKey, 120);
            lc.SubRecipeDependenciesJson = JsonSerializer.Serialize(linkedSubRecipeIds, CamelJson);

            if (lc.CostingSnapshotId.HasValue)
            {
                SilaCulinaryCostingSnapshot? snap = await _repository.SilaCulinaryCostingSnapshot
                    .FindFirstByConditionAsync(x => x.Id == lc.CostingSnapshotId.Value && x.BuyerId == buyer.Id);
                if (snap != null)
                {
                    snap.ProductionRecipeId = prodRecipe.Id;
                    snap.RecipeVersion = prodRecipe.Version;
                }
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_RECIPE, prodRecipe.Id,
                SilaRecipeRules.EVENT_CREATED, $"Created from AI Draft {draft.DraftReference}.", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                Common.SILA_AUDIT_CULINARY_RCP_ID_CREATED, $"Allocated Production Recipe ID {prodRecipe.RecipeCode} for {draft.DraftReference}.", userId);

            await _repository.SaveAsync();
            return MapLifecycleDto(lc, prodRecipe);
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> AssignRecipeImageAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            byte[] content, string? imageOrigin, CancellationToken cancellationToken)
        {
            if (content == null || content.Length == 0)
            {
                throw new BadRequestCustomException("Image required.", "Upload a valid JPEG, PNG, or WEBP dish photograph.");
            }
            if (content.Length > SilaRecipePictureRules.MAX_PICTURE_BYTES)
            {
                throw new BadRequestCustomException("Image too large.", $"Maximum recipe image size is {SilaRecipePictureRules.MAX_PICTURE_BYTES / (1024 * 1024)} MB.");
            }

            string? ext = SilaRecipePictureRules.DetectExtension(content);
            if (ext == null)
            {
                throw new BadRequestCustomException("Unsupported image format.", "Upload a JPEG, PNG, or WEBP image.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);

            if (!lc.ProductionRecipeId.HasValue)
            {
                throw new BadRequestCustomException("Production Recipe ID required.", "Create the Production Recipe ID before assigning the production recipe image.");
            }

            Recipe prod = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, lc.ProductionRecipeId.Value);
            string basePath = SilaRecipePictureRules.RequireBasePath(_configuration, _logger);
            string slot = SilaRecipePictureRules.PreferDraftSlot(prod) ? SilaRecipePictureRules.SLOT_DRAFT : SilaRecipePictureRules.SLOT_ACTIVE;
            SilaRecipePictureRules.WriteSlot(basePath, buyer.Id, prod.Id, slot, content, ext);

            lc.ImageAssigned = true;
            lc.ImageOrigin = string.IsNullOrWhiteSpace(imageOrigin) ? "CHEF_PHOTOGRAPH" : imageOrigin.Trim().ToUpperInvariant();

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                Common.SILA_AUDIT_CULINARY_RCP_IMG_ASSIGNED, $"Assigned {lc.ImageOrigin} image ({slot} slot) to {prod.RecipeCode}.", userId);
            await _repository.SaveAsync();
            return MapLifecycleDto(lc, prod);
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> PublishProductionRecipeAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);

            if (!lc.ProductionRecipeId.HasValue)
            {
                throw new BadRequestCustomException("Production Recipe ID required.", "Create the Production Recipe ID before publishing.");
            }

            Recipe prod = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, lc.ProductionRecipeId.Value);
            if (lc.LifecycleStatus is not ("APPROVED" or "RECIPE_ID_CREATED" or "PUBLISHED"))
            {
                throw new BadRequestCustomException("Approval required before publishing.", $"Lifecycle status is {lc.LifecycleStatus}.");
            }

            // Validate readiness again so no incomplete draft can become operational (§4 & §47)
            CulinaryStructuredRecipePayloadDto payload = ParseDraftPayload(draft);
            SilaCulinaryAiConfiguration aiCfg = await LoadAiConfigAsync(buyer.Id);
            SilaCulinaryCostingConfigDto costCfg = SilaCulinaryAiSettings.ParseCostingConfig(aiCfg.CostingConfigJson);
            SilaCulinaryRecipeReadinessDto readiness = await EvaluateReadinessInternalAsync(buyer, draft, payload, costCfg, cancellationToken);
            if (!readiness.ReadyForPublishing)
            {
                string blockers = string.Join("; ", readiness.Checks.Where(c => c.Status == "BLOCKING").Select(c => $"{c.Label}: {c.Detail}"));
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                    Common.SILA_AUDIT_CULINARY_RCP_PUB_FAILED, $"Publishing blocked for {prod.RecipeCode}: {blockers}", userId);
                await _repository.SaveAsync();
                throw new BadRequestCustomException("Recipe publishing blocked.", blockers);
            }

            prod.Status = Common.SILA_RECIPE_APPROVED;
            prod.ActiveVersion = prod.Version;
            prod.ApprovedOn = DateTime.UtcNow;
            SilaRecipeRules.PromoteDraftHeader(prod);

            string? basePath = _configuration[Common.BASE_FOLDER_PATH];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                SilaRecipePictureRules.PromoteDraftToActive(basePath, buyer.Id, prod.Id, _logger);
            }

            lc.LifecycleStatus = "PUBLISHED";
            lc.PublishedOnUtc = DateTime.UtcNow;
            lc.EffectiveDateUtc = input?.EffectiveDateUtc ?? DateTime.UtcNow;
            lc.ProductionRecipeVersion = prod.Version;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_RECIPE, prod.Id,
                SilaRecipeRules.EVENT_APPROVED, $"Published version {prod.Version} via Phase 1C Culinary Lifecycle.", userId);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                Common.SILA_AUDIT_CULINARY_RCP_PUBLISHED, $"Published {prod.RecipeCode} v{prod.Version} effective {lc.EffectiveDateUtc:yyyy-MM-dd}.", userId);

            await _repository.SaveAsync();
            return MapLifecycleDto(lc, prod);
        }

        public async Task<SilaCulinaryRecipeLifecycleSummaryDto> CreateNewProductionVersionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid draftId,
            SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeLifecycle lc = await GetOrCreateLifecycleAsync(buyer.Id, draft, cancellationToken);

            if (!lc.ProductionRecipeId.HasValue)
            {
                throw new BadRequestCustomException("No production recipe.", "Create the Production Recipe ID first before creating a new production version.");
            }

            Recipe prod = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, lc.ProductionRecipeId.Value);
            InventoryLedger ledger = new(_repository, buyer.Id, userId);
            SilaRecipeRules.StartEditing(_logger, prod, ledger);

            draft.RevisionNumber += 1;
            lc.ProductionRecipeVersion = prod.Version;
            lc.LifecycleStatus = "DRAFT";
            lc.ChangeReason = TrimOrNull(input?.ChangeReason, 500) ?? "Controlled revision started from published version.";

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RECIPE_LIFECYCLE, lc.Id,
                Common.SILA_AUDIT_CULINARY_RCP_REV_CREATED, $"Started revision {draft.RevisionNumber} / production v{prod.Version} for {prod.RecipeCode} ({lc.ChangeReason}).", userId);

            await _repository.SaveAsync();
            return MapLifecycleDto(lc, prod);
        }

        public async Task<List<SilaCulinaryRecipeVersionHistoryItemDto>> GetVersionHistoryAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecipeDraft draft = await LoadAuthorizedDraftAsync(buyer.Id, userId, roleId, draftId, cancellationToken);
            SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive);

            List<SilaCulinaryRecipeVersionHistoryItemDto> items = new();
            if (lc?.ProductionRecipeId != null)
            {
                Recipe? prod = await _repository.Recipe
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == lc.ProductionRecipeId.Value && x.IsActive);
                if (prod != null)
                {
                    for (int v = 1; v <= prod.Version; v++)
                    {
                        items.Add(new SilaCulinaryRecipeVersionHistoryItemDto
                        {
                            Version = v,
                            Status = v == prod.ActiveVersion ? "ACTIVE" : (v == prod.Version ? prod.Status : "SUPERSEDED"),
                            IsActiveSelling = v == prod.ActiveVersion,
                            TotalCost = prod.TotalCost,
                            Currency = prod.Currency,
                            ChangeReason = v == prod.Version ? lc.ChangeReason : null,
                            DateCreated = prod.DateCreated,
                            ApprovedOn = v == prod.ActiveVersion ? prod.ApprovedOn : null
                        });
                    }
                }
            }
            return items.OrderByDescending(x => x.Version).ToList();
        }

        public async Task<SilaCulinaryLifecycleDashboardDto> GetLifecycleDashboardAsync(
            Guid organizationId, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            IQueryable<SilaCulinaryRecipeDraft> dq = _repository.SilaCulinaryRecipeDraft
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "CANCELLED");
            if (propertyId.HasValue) dq = dq.Where(x => x.PropertyId == propertyId.Value);

            List<SilaCulinaryRecipeDraft> drafts = await dq.ToListAsync(cancellationToken);
            List<SilaCulinaryRecipeLifecycle> lifecycles = await _repository.SilaCulinaryRecipeLifecycle
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMaterialRequest> requests = await _repository.SilaCulinaryMaterialRequest
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryCostingSnapshot> snapshots = await _repository.SilaCulinaryCostingSnapshot
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, SilaCulinaryRecipeLifecycle> lcByDraft = lifecycles.ToDictionary(l => l.DraftId);
            Dictionary<Guid, List<SilaCulinaryMaterialMapping>> mapByDraft = mappings.GroupBy(m => m.DraftId).ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<Guid, SilaCulinaryCostingSnapshot> latestSnapByDraft = snapshots
                .Where(s => s.DraftId.HasValue)
                .GroupBy(s => s.DraftId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.DateCreated).First());

            int mappingPending = 0, readyForCosting = 0;
            Dictionary<string, int> byCategory = new(StringComparer.OrdinalIgnoreCase);

            foreach (SilaCulinaryRecipeDraft d in drafts)
            {
                CulinaryStructuredRecipePayloadDto p = ParseDraftPayload(d);
                string cat = string.IsNullOrWhiteSpace(p.Header.Category) ? "Uncategorized" : p.Header.Category;
                byCategory[cat] = byCategory.GetValueOrDefault(cat, 0) + 1;

                List<SilaCulinaryMaterialMapping> mList = mapByDraft.GetValueOrDefault(d.Id) ?? new();
                if (mList.Count == 0 || mList.Any(m => m.MappingStatus != "CONFIRMED"))
                {
                    mappingPending += 1;
                }
                else
                {
                    readyForCosting += 1;
                }
            }

            SilaCulinaryLifecycleDashboardDto dto = new()
            {
                AiRecipeDrafts = drafts.Count,
                MaterialMappingPending = mappingPending,
                MissingMaterialRequests = requests.Count(r => ActiveRequestStatuses.Contains(r.Status)),
                AwaitingMaterialSync = requests.Count(r => r.Status is "CREATION_PENDING" or "MATERIAL_CREATED" or "SYNCHRONIZATION_PENDING"),
                ReadyForCosting = readyForCosting,
                CostingIncomplete = latestSnapByDraft.Values.Count(s => s.CostCompleteness != "COMPLETE"),
                AboveFoodCostTarget = latestSnapByDraft.Values.Count(s => s.FoodCostPercent.HasValue && s.TargetFoodCostPercent.HasValue && s.FoodCostPercent > s.TargetFoodCostPercent),
                PendingApproval = lifecycles.Count(l => l.LifecycleStatus == "PENDING_APPROVAL"),
                ApprovedRecipes = lifecycles.Count(l => l.LifecycleStatus is "APPROVED" or "RECIPE_ID_CREATED"),
                ReadyForPublishing = lifecycles.Count(l => l.LifecycleStatus == "RECIPE_ID_CREATED"),
                PublishedRecipes = lifecycles.Count(l => l.LifecycleStatus == "PUBLISHED"),
                RequiringRecosting = latestSnapByDraft.Values.Count(s => s.CostCompleteness != "COMPLETE"),
                RecipesWithNewVersions = lifecycles.Count(l => (l.ProductionRecipeVersion ?? 1) > 1),
                RecipesByCategory = byCategory
            };

            dto.LifecycleFunnel = new Dictionary<string, int>
            {
                ["AI Drafts"] = dto.AiRecipeDrafts,
                ["Mapping Pending"] = dto.MaterialMappingPending,
                ["Ready for Costing"] = dto.ReadyForCosting,
                ["Pending Approval"] = dto.PendingApproval,
                ["Production ID Created"] = dto.ReadyForPublishing,
                ["Published"] = dto.PublishedRecipes
            };

            return dto;
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> ExportLifecycleReportAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid draftId, string reportType, string format, CancellationToken cancellationToken)
        {
            SilaCulinaryApprovalPackageDto pkg = await GetApprovalPackageAsync(organizationId, userId, roleId, draftId, cancellationToken);
            string rt = (reportType ?? "COSTING").Trim().ToUpperInvariant();
            string fmt = (format ?? "XLSX").Trim().ToUpperInvariant();

            if (fmt == "CSV")
            {
                StringBuilder sb = new();
                sb.AppendLine("Recipe,DraftRef,TempRef,Ingredient,MaterialCode,RecipeQty,RecipeUOM,Yield%,CostingQty,CostingUOM,UnitPrice,PriceSource,IngredientCost,Share%");
                if (pkg.CostingSummary != null)
                {
                    foreach (SilaCulinaryCostedLineDto l in pkg.CostingSummary.Lines)
                    {
                        sb.AppendLine(string.Join(",",
                            CsvCell(pkg.RecipeName),
                            CsvCell(pkg.DraftReference),
                            CsvCell(l.TempRef),
                            CsvCell(l.Description),
                            CsvCell(l.MaterialCode ?? ""),
                            l.RecipeQty.ToString(CultureInfo.InvariantCulture),
                            CsvCell(l.RecipeUom),
                            l.YieldPercent.ToString(CultureInfo.InvariantCulture),
                            l.CostingQty.ToString(CultureInfo.InvariantCulture),
                            CsvCell(l.CostingUom),
                            l.UnitPrice?.ToString(CultureInfo.InvariantCulture) ?? "Missing",
                            CsvCell(l.PriceSource),
                            l.IngredientCost?.ToString(CultureInfo.InvariantCulture) ?? "Missing",
                            l.PercentageOfTotal?.ToString(CultureInfo.InvariantCulture) ?? "0"));
                    }
                }
                return (Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"{pkg.DraftReference}-{rt.ToLowerInvariant()}-report.csv");
            }

            if (fmt == "PDF")
            {
                List<string> lines = new()
                {
                    $"Recipe: {pkg.RecipeName} ({pkg.DraftReference} Rev {pkg.DraftRevision})",
                    $"Servings: {pkg.BaseServings} {pkg.ServingUom} | Lifecycle: {pkg.Lifecycle.LifecycleStatus} | Prod ID: {pkg.Lifecycle.ProductionRecipeCode ?? "Not Created"}"
                };
                if (pkg.CostingSummary != null)
                {
                    lines.Add($"Total Ingredient Cost: {pkg.CostingSummary.Currency} {pkg.CostingSummary.TotalIngredientCost:0.00} | Per Portion: {pkg.CostingSummary.IngredientCostPerPortion:0.0000} | Food Cost %: {pkg.CostingSummary.FoodCostPercent?.ToString("0.00") ?? "N/A"}");
                    foreach (SilaCulinaryCostedLineDto l in pkg.CostingSummary.Lines)
                    {
                        lines.Add($"- {l.Description} ({l.MaterialCode ?? "Unmapped"}): {l.CostingQty} {l.CostingUom} x {l.UnitPrice?.ToString("0.00") ?? "MISSING"} = {l.IngredientCost?.ToString("0.00") ?? "MISSING"} ({l.PriceSource})");
                    }
                }
                return (BuildSimpleTextPdf($"SILA Recipe {rt} Report", lines), "application/pdf", $"{pkg.DraftReference}-{rt.ToLowerInvariant()}-report.pdf");
            }

            using XLWorkbook wb = new();
            IXLWorksheet ws = wb.Worksheets.Add("Recipe Report");
            ws.Cell(1, 1).Value = "Recipe";
            ws.Cell(1, 2).Value = SafeSpreadsheetCell(pkg.RecipeName);
            ws.Cell(2, 1).Value = "Draft Reference";
            ws.Cell(2, 2).Value = SafeSpreadsheetCell(pkg.DraftReference);
            ws.Cell(3, 1).Value = "Production Recipe ID";
            ws.Cell(3, 2).Value = SafeSpreadsheetCell(pkg.Lifecycle.ProductionRecipeCode ?? "Pending");
            ws.Cell(4, 1).Value = "Lifecycle Status";
            ws.Cell(4, 2).Value = SafeSpreadsheetCell(pkg.Lifecycle.LifecycleStatus);

            if (pkg.CostingSummary != null)
            {
                string[] headers = { "Ref", "Ingredient", "Material Code", "Recipe Qty", "Recipe UOM", "Yield %", "Costing Qty", "Costing UOM", "Unit Price", "Price Source", "Ingredient Cost", "Share %" };
                for (int c = 0; c < headers.Length; c++)
                {
                    IXLCell cell = ws.Cell(6, c + 1);
                    cell.Value = headers[c];
                    cell.Style.Font.Bold = true;
                }
                for (int i = 0; i < pkg.CostingSummary.Lines.Count; i++)
                {
                    SilaCulinaryCostedLineDto l = pkg.CostingSummary.Lines[i];
                    int r = i + 7;
                    ws.Cell(r, 1).Value = SafeSpreadsheetCell(l.TempRef);
                    ws.Cell(r, 2).Value = SafeSpreadsheetCell(l.Description);
                    ws.Cell(r, 3).Value = SafeSpreadsheetCell(l.MaterialCode ?? "");
                    ws.Cell(r, 4).Value = (double)l.RecipeQty;
                    ws.Cell(r, 5).Value = SafeSpreadsheetCell(l.RecipeUom);
                    ws.Cell(r, 6).Value = (double)l.YieldPercent;
                    ws.Cell(r, 7).Value = (double)l.CostingQty;
                    ws.Cell(r, 8).Value = SafeSpreadsheetCell(l.CostingUom);
                    ws.Cell(r, 9).Value = l.UnitPrice.HasValue ? (double)l.UnitPrice.Value : "MISSING";
                    ws.Cell(r, 10).Value = SafeSpreadsheetCell(l.PriceSource);
                    ws.Cell(r, 11).Value = l.IngredientCost.HasValue ? (double)l.IngredientCost.Value : "MISSING";
                    ws.Cell(r, 12).Value = l.PercentageOfTotal.HasValue ? (double)l.PercentageOfTotal.Value : 0;
                }
            }
            ws.Columns().AdjustToContents();
            using MemoryStream ms = new();
            wb.SaveAs(ms);
            return (ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{pkg.DraftReference}-{rt.ToLowerInvariant()}-report.xlsx");
        }

        public async Task<SilaCulinaryPhase2AHandoverDto> GetPhase2AHandoverAsync(
            Guid organizationId, Guid productionRecipeId, int? version, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            Recipe prod = await _repository.Recipe
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == productionRecipeId && x.IsActive)
                ?? throw new NotFoundCustomException("Production Recipe not found.", "Select a valid Production Recipe ID.");

            int targetVersion = version ?? (prod.ActiveVersion > 0 ? prod.ActiveVersion : prod.Version);
            if (prod.ActiveVersion <= 0 && prod.Status != Common.SILA_RECIPE_APPROVED)
            {
                throw new BadRequestCustomException(
                    "Recipe not published.",
                    $"Production Recipe {prod.RecipeCode} must be approved/published before Phase 2A Event Planner handover.");
            }

            SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.ProductionRecipeId == prod.Id && x.IsActive);
            SilaCulinaryRecipeDraft? draft = lc != null
                ? await _repository.SilaCulinaryRecipeDraft.FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == lc.DraftId && x.IsActive)
                : null;
            CulinaryStructuredRecipePayloadDto? payload = draft != null ? ParseDraftPayload(draft) : null;

            SilaCulinaryCostingSnapshot? snap = lc?.CostingSnapshotId != null
                ? await _repository.SilaCulinaryCostingSnapshot.FindFirstByConditionAsync(x => x.Id == lc.CostingSnapshotId.Value && x.BuyerId == buyer.Id)
                : null;

            List<RecipeIngredient> ingRows = await _repository.RecipeIngredient
                .FindByCondition(x => x.RecipeId == prod.Id && x.Version == targetVersion && x.IsActive)
                .ToListAsync(cancellationToken);

            List<Guid> matIds = ingRows.Where(i => i.MaterialId.HasValue).Select(i => i.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> matById = matIds.Count > 0
                ? await _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, ItemBuyerMaster>();

            return new SilaCulinaryPhase2AHandoverDto
            {
                ProductionRecipeId = prod.Id,
                ProductionRecipeCode = prod.RecipeCode,
                RecipeVersion = targetVersion,
                RecipeName = prod.Name,
                RecipeCategory = prod.Category,
                Cuisine = payload?.Header.Cuisine,
                BaseServings = prod.ServingQty,
                ServingUom = prod.ServingUom,
                SellingUom = prod.SellingUom,
                PrepDurationMinutes = prod.PrepTimeMinutes,
                CookDurationMinutes = prod.CookTimeMinutes,
                ImageAssigned = lc?.ImageAssigned ?? false,
                PictureEndpoint = (lc?.ImageAssigned ?? false) ? $"/api/v1/buyer/sila/recipes/{prod.Id}/picture?version={targetVersion}" : null,
                Allergens = payload?.Header.PotentialAllergens ?? new(),
                DietaryClassifications = payload?.Header.DietaryTags ?? new(),
                CostingSnapshotId = snap?.Id,
                CostingReference = snap?.CostingReference,
                CostPerPortion = snap?.IngredientCostPerPortion ?? (prod.ServingQty > 0 ? Math.Round(prod.TotalCost / prod.ServingQty, 4) : prod.TotalCost),
                AuthorizedSellingPrice = snap?.SellingPricePerPortion,
                Currency = prod.Currency ?? buyer.Currency ?? "AED",
                PublishingStatus = prod.Status,
                EffectiveDateUtc = lc?.EffectiveDateUtc ?? prod.ApprovedOn,
                Ingredients = ingRows.Select(i =>
                {
                    matById.TryGetValue(i.MaterialId ?? Guid.Empty, out ItemBuyerMaster? mat);
                    return new SilaCulinaryPhase2AHandoverIngredientDto
                    {
                        MaterialCode = mat?.MaterialCode ?? string.Empty,
                        Description = mat?.Description ?? string.Empty,
                        Quantity = i.Quantity,
                        Uom = i.Uom ?? mat?.BaseUnitOfMeasure ?? "EA",
                        ConversionFactor = 1m,
                        YieldPercent = 100m,
                        Scalable = true
                    };
                }).ToList()
            };
        }

        // =========================================================================
        // PRIVATE DETERMINISTIC HELPERS
        // =========================================================================

        private async Task<SilaCulinaryCostingResultDto> ComputeDeterministicCostingAsync(
            BuyerBusinessProfile buyer,
            SilaCulinaryRecipeDraft draft,
            CulinaryStructuredRecipePayloadDto payload,
            SilaCulinaryCostingConfigDto costCfg,
            bool canViewCost,
            SilaCulinaryCostingCalculateRequestDto input,
            CancellationToken cancellationToken)
        {
            string currency = buyer.Currency ?? costCfg.ReportingCurrency;
            decimal servings = input.Servings ?? (draft.BaseServings > 0 ? draft.BaseServings : 1m);
            if (servings <= 0) servings = 1m;
            decimal targetFc = input.TargetFoodCostPercent ?? costCfg.DefaultTargetFoodCostPercent;

            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            Dictionary<string, SilaCulinaryMaterialMapping> mapByRef = mappings.ToDictionary(m => m.IngredientTempRef, StringComparer.OrdinalIgnoreCase);

            List<Guid> matIds = mappings.Where(m => m.MaterialId.HasValue).Select(m => m.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> matById = matIds.Count > 0
                ? await _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, ItemBuyerMaster>();

            List<MaterialUomConversion> allConvs = matIds.Count > 0
                ? await _repository.MaterialUomConversion.FindByCondition(x => matIds.Contains(x.MaterialId) && x.IsActive).ToListAsync(cancellationToken)
                : new List<MaterialUomConversion>();
            ILookup<Guid, MaterialUomConversion> convLookup = allConvs.ToLookup(c => c.MaterialId);

            Guid? effectiveOutletId = input.OutletId ?? draft.OutletId ?? draft.LocationId;
            Dictionary<Guid, MaterialOutletPrice> outletPrices = effectiveOutletId.HasValue && matIds.Count > 0
                ? await _repository.MaterialOutletPrice
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.OutletLocationId == effectiveOutletId.Value && matIds.Contains(x.MaterialId))
                    .ToDictionaryAsync(x => x.MaterialId, cancellationToken)
                : new Dictionary<Guid, MaterialOutletPrice>();

            Dictionary<string, SilaCulinaryCostingOverrideDto> overrideByRef = (input.Overrides ?? new())
                .ToDictionary(o => o.TempRef, StringComparer.OrdinalIgnoreCase);

            List<SilaCulinaryCostedLineDto> lines = new();
            decimal totalIngredientCost = 0m;
            decimal subRecipeCost = 0m;
            int missingPrices = 0, currencyMismatches = 0;
            bool circularDetected = false;
            List<string> warnings = new();

            foreach (CulinaryIngredientLineDto ing in payload.Ingredients ?? new())
            {
                overrideByRef.TryGetValue(ing.TempRef, out SilaCulinaryCostingOverrideDto? ov);
                mapByRef.TryGetValue(ing.TempRef, out SilaCulinaryMaterialMapping? map);

                decimal recipeQty = ov?.Quantity ?? ing.Quantity;
                string recipeUom = NormalizeUom(ov?.RecipeUom ?? ing.RecipeUom);
                string qtyBasis = (ov?.QuantityBasis ?? ing.QuantityBasis ?? "RAW").ToUpperInvariant();
                decimal yieldPct = ov?.YieldPercent ?? (ing.YieldPercent > 0 ? ing.YieldPercent : 100m);
                decimal prepLoss = ov?.PrepLossPercent ?? ing.PrepLossPercent;
                decimal cookLoss = ov?.CookLossPercent ?? ing.CookLossPercent;
                decimal wastage = ov?.WastagePercent ?? 0m;
                decimal buffer = ov?.BufferPercent ?? input.GlobalBufferPercent;

                decimal effectiveGrossQty = recipeQty;
                if (qtyBasis == "EDIBLE" && yieldPct is > 0m and < 100m)
                {
                    effectiveGrossQty = recipeQty / (yieldPct / 100m);
                }
                else if (qtyBasis == "RAW")
                {
                    decimal combinedLoss = prepLoss + cookLoss + wastage;
                    if (combinedLoss > 0m && combinedLoss < 95m)
                    {
                        effectiveGrossQty = recipeQty / (1m - combinedLoss / 100m);
                    }
                }
                if (buffer > 0m)
                {
                    effectiveGrossQty *= (1m + buffer / 100m);
                }
                effectiveGrossQty = Math.Round(effectiveGrossQty, 4, MidpointRounding.AwayFromZero);

                ItemBuyerMaster? mat = map?.MaterialId != null ? matById.GetValueOrDefault(map.MaterialId.Value) : null;
                string costingUom = mat != null ? NormalizeUom(mat.BaseUnitOfMeasure) : recipeUom;
                decimal costingQty = effectiveGrossQty;
                bool uomOk = true;
                if (mat != null)
                {
                    (bool ok, decimal? convQty, string _) = ConvertUomDeterministic(effectiveGrossQty, recipeUom, costingUom, convLookup[mat.Id].ToList());
                    uomOk = ok;
                    if (ok && convQty.HasValue) costingQty = convQty.Value;
                }

                decimal? unitPrice = null;
                decimal priceUnit = 1m;
                string priceCurrency = currency;
                string priceSource = "MISSING";
                bool isEstimated = false;

                if (canViewCost)
                {
                    if (ov?.ManualUnitPrice != null && costCfg.AllowManualCostEstimate)
                    {
                        unitPrice = ov.ManualUnitPrice.Value;
                        priceUnit = ov.ManualPriceUnit > 0 ? ov.ManualPriceUnit.Value : 1m;
                        priceSource = ov.ManualPriceSource ?? "MANUAL_ESTIMATE";
                        isEstimated = true;
                    }
                    else if (mat != null)
                    {
                        (unitPrice, priceCurrency, priceSource) = ResolveMaterialPrice(mat, outletPrices.GetValueOrDefault(mat.Id), costCfg.CostSourceHierarchy, currency);
                    }

                    if (unitPrice.HasValue && input.GlobalPriceIncreasePercent != 0m)
                    {
                        unitPrice = Math.Round(unitPrice.Value * (1m + input.GlobalPriceIncreasePercent / 100m), 4, MidpointRounding.AwayFromZero);
                    }
                }
                else
                {
                    priceSource = "RESTRICTED";
                }

                string validationStatus = "OK";
                string? issueReason = null;
                decimal? lineCost = null;

                if (mat == null)
                {
                    validationStatus = "UNMAPPED";
                    issueReason = "Material mapping missing or not confirmed.";
                    missingPrices += 1;
                }
                else if (!uomOk)
                {
                    validationStatus = "UOM_CONFLICT";
                    issueReason = $"No conversion from {recipeUom} to {costingUom}.";
                    missingPrices += 1;
                }
                else if (!canViewCost)
                {
                    validationStatus = "RESTRICTED";
                }
                else if (!unitPrice.HasValue)
                {
                    validationStatus = "MISSING_PRICE";
                    issueReason = "No authorized material price configured (never substituted with 0).";
                    missingPrices += 1;
                }
                else if (!string.Equals(priceCurrency, currency, StringComparison.OrdinalIgnoreCase))
                {
                    validationStatus = "CURRENCY_MISMATCH";
                    issueReason = $"Price currency {priceCurrency} differs from reporting currency {currency}.";
                    currencyMismatches += 1;
                }
                else
                {
                    decimal effectiveUnitCost = unitPrice.Value / (priceUnit > 0m ? priceUnit : 1m);
                    lineCost = Math.Round(costingQty * effectiveUnitCost, 4, MidpointRounding.AwayFromZero);
                    totalIngredientCost += lineCost.Value;
                }

                lines.Add(new SilaCulinaryCostedLineDto
                {
                    TempRef = ing.TempRef,
                    Description = ing.Description,
                    MaterialId = mat?.Id,
                    MaterialCode = mat?.MaterialCode ?? map?.MaterialCode,
                    RecipeQty = recipeQty,
                    RecipeUom = recipeUom,
                    YieldPercent = yieldPct,
                    EffectiveGrossQty = effectiveGrossQty,
                    CostingQty = costingQty,
                    CostingUom = costingUom,
                    UnitPrice = unitPrice,
                    PriceUnit = priceUnit,
                    PriceCurrency = priceCurrency,
                    PriceSource = priceSource,
                    IsEstimatedCost = isEstimated,
                    PriceTimestampUtc = mat?.DateUpdated,
                    IngredientCost = lineCost,
                    ValidationStatus = validationStatus,
                    IssueReason = issueReason
                });
            }

            // Sub-recipes (§27)
            foreach (CulinarySubRecipeDto sub in payload.SubRecipes ?? new())
            {
                if (!string.IsNullOrWhiteSpace(sub.ExistingRecipeReference) && Guid.TryParse(sub.ExistingRecipeReference, out Guid subId))
                {
                    if (subId == draft.SourceRecipeId)
                    {
                        circularDetected = true;
                        warnings.Add($"Circular sub-recipe reference detected on {sub.SubRecipeName}.");
                        continue;
                    }
                    Recipe? subRec = await _repository.Recipe.FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == subId && x.IsActive);
                    if (subRec != null && subRec.ServingQty > 0m && canViewCost)
                    {
                        decimal alloc = Math.Round((subRec.TotalCost / subRec.ServingQty) * sub.RequiredQtyInParent, 4, MidpointRounding.AwayFromZero);
                        subRecipeCost += alloc;
                        totalIngredientCost += alloc;
                        lines.Add(new SilaCulinaryCostedLineDto
                        {
                            TempRef = sub.SubRecipeRef,
                            Description = $"[Sub-Recipe] {subRec.Name}",
                            SubRecipeId = subRec.Id,
                            MaterialCode = subRec.RecipeCode,
                            IsSubRecipe = true,
                            RecipeQty = sub.RequiredQtyInParent,
                            RecipeUom = sub.RequiredUomInParent,
                            CostingQty = sub.RequiredQtyInParent,
                            CostingUom = subRec.ServingUom,
                            UnitPrice = Math.Round(subRec.TotalCost / subRec.ServingQty, 4, MidpointRounding.AwayFromZero),
                            PriceCurrency = subRec.Currency ?? currency,
                            PriceSource = "SUB_RECIPE_MASTER",
                            IngredientCost = alloc,
                            ValidationStatus = "OK"
                        });
                    }
                }
            }

            totalIngredientCost = Math.Round(totalIngredientCost, 4, MidpointRounding.AwayFromZero);
            foreach (SilaCulinaryCostedLineDto l in lines)
            {
                if (l.IngredientCost.HasValue && totalIngredientCost > 0m)
                {
                    l.PercentageOfTotal = Math.Round(l.IngredientCost.Value / totalIngredientCost * 100m, 2, MidpointRounding.AwayFromZero);
                }
            }

            decimal addDirect = 0m, addOverhead = 0m;
            if (canViewCost)
            {
                foreach (SilaCulinaryAdditionalCostInputDto ac in input.AdditionalCosts ?? new())
                {
                    decimal computed = ac.Basis.ToUpperInvariant() switch
                    {
                        "FIXED_BATCH" => ac.Amount,
                        "PER_PORTION" => ac.Amount * servings,
                        "PERCENT_OF_INGREDIENT" => totalIngredientCost * (ac.Amount / 100m),
                        "HOURLY" or "QUANTITY" => ac.Amount * ac.Units,
                        _ => ac.Amount
                    };
                    if (string.Equals(ac.Category, "ALLOCATED_OVERHEAD", StringComparison.OrdinalIgnoreCase))
                        addOverhead += computed;
                    else
                        addDirect += computed;
                }
            }

            addDirect = Math.Round(addDirect, 4, MidpointRounding.AwayFromZero);
            addOverhead = Math.Round(addOverhead, 4, MidpointRounding.AwayFromZero);
            decimal totalRecipeCost = Math.Round(totalIngredientCost + addDirect + addOverhead, 4, MidpointRounding.AwayFromZero);
            decimal ingCostPerPortion = Math.Round(totalIngredientCost / servings, 4, MidpointRounding.AwayFromZero);
            decimal totalCostPerPortion = Math.Round(totalRecipeCost / servings, 4, MidpointRounding.AwayFromZero);

            decimal? sellingPrice = canViewCost ? input.SellingPricePerPortion : null;
            decimal? fcPct = sellingPrice > 0m
                ? Math.Round(ingCostPerPortion / sellingPrice.Value * 100m, 2, MidpointRounding.AwayFromZero)
                : null;
            decimal? targetVar = fcPct.HasValue ? Math.Round(fcPct.Value - targetFc, 2, MidpointRounding.AwayFromZero) : null;
            decimal? recPriceNet = canViewCost && targetFc > 0m
                ? Math.Round(ingCostPerPortion / (targetFc / 100m), 4, MidpointRounding.AwayFromZero)
                : null;
            decimal? recMarkup = canViewCost && targetFc > 0m && targetFc < 100m
                ? Math.Round(ingCostPerPortion * (1m + (100m - targetFc) / targetFc), 4, MidpointRounding.AwayFromZero)
                : null;

            decimal? estRev = sellingPrice > 0m ? Math.Round(sellingPrice.Value * servings, 4, MidpointRounding.AwayFromZero) : null;
            decimal? grossProfit = estRev.HasValue ? Math.Round(estRev.Value - totalIngredientCost, 4, MidpointRounding.AwayFromZero) : null;
            decimal? contribution = estRev.HasValue ? Math.Round(estRev.Value - totalIngredientCost - addDirect, 4, MidpointRounding.AwayFromZero) : null;
            decimal? contribMargin = estRev > 0m && contribution.HasValue ? Math.Round(contribution.Value / estRev.Value * 100m, 2, MidpointRounding.AwayFromZero) : null;
            decimal? netAfterOverhead = estRev.HasValue ? Math.Round(estRev.Value - totalRecipeCost, 4, MidpointRounding.AwayFromZero) : null;

            string completeness = (missingPrices > 0 || currencyMismatches > 0 || circularDetected || !canViewCost)
                ? "PARTIAL"
                : "COMPLETE";

            return new SilaCulinaryCostingResultDto
            {
                DraftId = draft.Id,
                DraftRevision = draft.RevisionNumber,
                RecipeName = draft.RecipeName,
                PropertyId = input.PropertyId ?? draft.PropertyId,
                PropertyName = draft.PropertyName,
                ScenarioName = string.IsNullOrWhiteSpace(input.ScenarioName) ? "Baseline" : input.ScenarioName.Trim(),
                IsBaseline = input.IsBaseline,
                CostingBasis = string.IsNullOrWhiteSpace(input.CostingBasis) ? "STANDARD_RECIPE_COST" : input.CostingBasis.Trim().ToUpperInvariant(),
                TaxBasis = string.IsNullOrWhiteSpace(input.TaxBasis) ? costCfg.TaxBasis : input.TaxBasis.Trim().ToUpperInvariant(),
                Currency = currency,
                Servings = servings,
                TotalIngredientCost = totalIngredientCost,
                SubRecipeCost = Math.Round(subRecipeCost, 4, MidpointRounding.AwayFromZero),
                AdditionalDirectCost = addDirect,
                AllocatedOverheadCost = addOverhead,
                TotalRecipeCost = totalRecipeCost,
                IngredientCostPerPortion = ingCostPerPortion,
                TotalCostPerPortion = totalCostPerPortion,
                SellingPricePerPortion = sellingPrice,
                FoodCostPercent = fcPct,
                TargetFoodCostPercent = targetFc,
                TargetVariancePercent = targetVar,
                RecommendedSellingPriceNet = recPriceNet,
                RecommendedSellingPriceMarkup = recMarkup,
                EstimatedRevenue = estRev,
                GrossProfit = grossProfit,
                ContributionAmount = contribution,
                ContributionMarginPercent = contribMargin,
                EstimatedNetProfitAfterOverhead = netAfterOverhead,
                CostCompleteness = completeness,
                MissingPriceCount = missingPrices,
                CurrencyMismatchCount = currencyMismatches,
                CircularDependencyDetected = circularDetected,
                CanViewCostDetail = canViewCost,
                Lines = lines,
                AdditionalCosts = input.AdditionalCosts ?? new(),
                Warnings = warnings,
                CalculatedAtUtc = DateTime.UtcNow
            };
        }

        private async Task<SilaCulinaryRecipeReadinessDto> EvaluateReadinessInternalAsync(
            BuyerBusinessProfile buyer,
            SilaCulinaryRecipeDraft draft,
            CulinaryStructuredRecipePayloadDto payload,
            SilaCulinaryCostingConfigDto costCfg,
            CancellationToken cancellationToken)
        {
            List<SilaCulinaryReadinessCheckItemDto> checks = new();

            bool headerOk = !string.IsNullOrWhiteSpace(draft.RecipeName) && !string.IsNullOrWhiteSpace(payload.Header.Category) && draft.BaseServings > 0;
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "HEADER",
                Label = "Recipe Header & Servings",
                Status = headerOk ? "PASSED" : "BLOCKING",
                ResponsibleTeam = "Chef",
                Detail = headerOk ? $"{draft.RecipeName} ({draft.BaseServings} {payload.Header.ServingUom})" : "Recipe name, category, and positive serving count required."
            });

            List<SilaCulinaryMaterialMapping> mappings = await _repository.SilaCulinaryMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            int ingCount = payload.Ingredients?.Count ?? 0;
            int confirmedCount = mappings.Count(m => m.MappingStatus == "CONFIRMED" && m.MaterialId.HasValue);
            bool allMapped = ingCount > 0 && confirmedCount == ingCount;
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "MATERIAL_MAPPINGS",
                Label = "Mandatory Material Mappings",
                Status = allMapped ? "PASSED" : "BLOCKING",
                ResponsibleTeam = "Chef",
                Detail = $"{confirmedCount} / {ingCount} ingredient(s) confirmed against Material Master.",
                RecommendedAction = allMapped ? null : "Confirm all ingredient mappings or resolve missing materials."
            });

            bool uomOk = mappings.All(m => m.UomCompatible);
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "UOM_CONVERSIONS",
                Label = "UOM Conversions Validated",
                Status = uomOk ? "PASSED" : "BLOCKING",
                ResponsibleTeam = "Master Data",
                Detail = uomOk ? "All ingredient UOM conversions verified." : "One or more ingredients have UOM conversion conflicts.",
                RecommendedAction = uomOk ? null : "Submit a UOM Maintenance request or update ingredient UOM."
            });

            List<SilaCulinaryMaterialRequestLink> links = await _repository.SilaCulinaryMaterialRequestLink
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<Guid> reqIds = links.Select(l => l.MaterialRequestId).Distinct().ToList();
            List<SilaCulinaryMaterialRequest> openReqs = reqIds.Count > 0
                ? await _repository.SilaCulinaryMaterialRequest
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && reqIds.Contains(x.Id))
                    .ToListAsync(cancellationToken)
                : new List<SilaCulinaryMaterialRequest>();
            int pendingReqs = openReqs.Count(r => ActiveRequestStatuses.Contains(r.Status));
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "MATERIAL_REQUESTS",
                Label = "Outstanding Material Requests",
                Status = pendingReqs == 0 ? "PASSED" : "BLOCKING",
                ResponsibleTeam = "Master Data",
                Detail = pendingReqs == 0 ? "No blocking open material requests." : $"{pendingReqs} material request(s) still open.",
                RecommendedAction = pendingReqs == 0 ? null : "Complete material creation/sync and confirm mapping."
            });

            bool stepsOk = (payload.Instructions?.Count ?? 0) > 0;
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "PREPARATION_STEPS",
                Label = "Preparation Instructions",
                Status = stepsOk ? "PASSED" : "BLOCKING",
                ResponsibleTeam = "Chef",
                Detail = stepsOk ? $"{payload.Instructions!.Count} preparation step(s) documented." : "Preparation instructions required before publishing."
            });

            SilaCulinaryCostingResultDto costing = await ComputeDeterministicCostingAsync(
                buyer, draft, payload, costCfg, true, new SilaCulinaryCostingCalculateRequestDto(), cancellationToken);
            bool costOk = costing.CostCompleteness == "COMPLETE" || !costCfg.BlockPublishOnMissingCost;
            checks.Add(new SilaCulinaryReadinessCheckItemDto
            {
                Code = "COSTING",
                Label = "Recipe Costing Completeness",
                Status = costing.CostCompleteness == "COMPLETE" ? "PASSED" : (costCfg.BlockPublishOnMissingCost ? "BLOCKING" : "WARNING"),
                ResponsibleTeam = "Cost Control",
                Detail = costing.CostCompleteness == "COMPLETE"
                    ? $"Complete ({costing.Currency} {costing.TotalIngredientCost:0.00}, {costing.IngredientCostPerPortion:0.0000}/portion)."
                    : $"{costing.MissingPriceCount} ingredient(s) missing authorized prices."
            });

            SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == draft.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            string lcStatus = lc?.LifecycleStatus ?? "DRAFT";

            int blocking = checks.Count(c => c.Status == "BLOCKING");
            int warning = checks.Count(c => c.Status == "WARNING");
            int passed = checks.Count(c => c.Status == "PASSED");

            return new SilaCulinaryRecipeReadinessDto
            {
                DraftId = draft.Id,
                DraftReference = draft.DraftReference,
                RecipeName = draft.RecipeName,
                DraftRevision = draft.RevisionNumber,
                LifecycleStatus = lcStatus,
                ReadyForApproval = blocking == 0,
                ReadyForProductionId = blocking == 0 && lcStatus == "APPROVED",
                ReadyForPublishing = blocking == 0 && lc?.ProductionRecipeId != null && lcStatus is "APPROVED" or "RECIPE_ID_CREATED" or "PUBLISHED",
                PassedCount = passed,
                BlockingCount = blocking,
                WarningCount = warning,
                Checks = checks,
                ProductionRecipeId = lc?.ProductionRecipeId,
                ProductionRecipeCode = lc?.ProductionRecipeCode,
                ProductionRecipeVersion = lc?.ProductionRecipeVersion
            };
        }

        private async Task<ItemBuyerMaster> ExecuteStandaloneMaterialActionAsync(
            BuyerBusinessProfile buyer, SilaCulinaryMaterialRequest entity, Guid userId, CancellationToken cancellationToken)
        {
            if (entity.RequestType == "MATERIAL_REACTIVATION" && entity.TargetMaterialId.HasValue)
            {
                ItemBuyerMaster target = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == entity.TargetMaterialId.Value)
                    ?? throw new NotFoundCustomException("Target material not found.", "Target material for reactivation no longer exists.");
                target.IsActive = true;
                return target;
            }

            if (entity.RequestType == "MATERIAL_EXTENSION" && entity.TargetMaterialId.HasValue)
            {
                ItemBuyerMaster target = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == entity.TargetMaterialId.Value && x.IsActive)
                    ?? throw new NotFoundCustomException("Target material not found.", "Target material for extension is missing or inactive.");
                await ExtendMaterialToPropertyAsync(buyer.Id, target.Id, entity.PropertyId, cancellationToken);
                return target;
            }

            if (entity.RequestType == "UOM_MAINTENANCE" && entity.TargetMaterialId.HasValue)
            {
                ItemBuyerMaster target = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == entity.TargetMaterialId.Value && x.IsActive)
                    ?? throw new NotFoundCustomException("Target material not found.", "Target material for UOM maintenance is missing.");
                await EnsureUomConversionAsync(buyer.Id, target.Id, entity, cancellationToken);
                return target;
            }

            if (entity.RequestType is "SPECIFICATION_CORRECTION" or "CLASSIFICATION_MAINTENANCE" && entity.TargetMaterialId.HasValue)
            {
                ItemBuyerMaster target = await _repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == entity.TargetMaterialId.Value && x.IsActive)
                    ?? throw new NotFoundCustomException("Target material not found.", "Target material for correction is missing.");
                target.Description = entity.ProposedDescription;
                if (!string.IsNullOrWhiteSpace(entity.MaterialGroup)) target.MaterialGroup = entity.MaterialGroup;
                if (!string.IsNullOrWhiteSpace(entity.MaterialType)) target.ProductType = entity.MaterialType;
                return target;
            }

            // Default: NEW_MATERIAL via DocumentNumber.STANDALONE_MATERIAL (MAT########)
            string matCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.STANDALONE_MATERIAL, 8, cancellationToken);
            ItemBuyerMaster created = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = matCode,
                Description = entity.ProposedDescription,
                MaterialGroup = entity.MaterialGroup ?? "CULINARY",
                ProductType = entity.MaterialType ?? "ROH",
                BaseUnitOfMeasure = entity.BaseUom,
                OrderUnitOfMeasure = entity.PurchasingUom ?? entity.BaseUom,
                AlternateUnitOfMeasure = entity.AlternateUom ?? entity.BaseUom,
                ValuationClass = "3000",
                UnitOfMeasureMapping = entity.BaseUom,
                Barcode = entity.Barcode,
                Currency = buyer.Currency ?? "AED",
                IsInventoryItem = true,
                BatchManaged = entity.BatchManaged,
                ExpiryManaged = entity.ShelfLifeDays.HasValue,
                ShelfLifeDays = entity.ShelfLifeDays,
                Source = null, // Standalone SILA material
                CompanyCode = entity.CompanyCode,
                IsActive = true
            };
            _repository.ItemBuyerMaster.Create(created);
            await EnsureUomConversionAsync(buyer.Id, created.Id, entity, cancellationToken);
            await ExtendMaterialToPropertyAsync(buyer.Id, created.Id, entity.PropertyId, cancellationToken);
            return created;
        }

        private async Task EnsureUomConversionAsync(Guid buyerId, Guid materialId, SilaCulinaryMaterialRequest entity, CancellationToken cancellationToken)
        {
            string? fromUom = entity.AlternateUom ?? entity.PurchasingUom;
            if (string.IsNullOrWhiteSpace(fromUom) || string.Equals(fromUom, entity.BaseUom, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            decimal num = entity.ConversionNumerator ?? entity.PackSize ?? 0m;
            decimal den = entity.ConversionDenominator ?? 1m;
            if (num <= 0m || den <= 0m) return;

            decimal factor = Math.Round(num / den, 6, MidpointRounding.AwayFromZero);
            bool exists = await _repository.MaterialUomConversion
                .FindByCondition(x => x.MaterialId == materialId && x.FromUom == fromUom && x.ToUom == entity.BaseUom && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                _repository.MaterialUomConversion.Create(new MaterialUomConversion
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    MaterialId = materialId,
                    FromUom = fromUom,
                    ToUom = entity.BaseUom,
                    Factor = factor,
                    IsActive = true
                });
            }
        }

        private async Task ExtendMaterialToPropertyAsync(Guid buyerId, Guid materialId, Guid? propertyId, CancellationToken cancellationToken)
        {
            if (!propertyId.HasValue) return;
            List<Guid> locIds = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.PropertyId == propertyId.Value)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (Guid locId in locIds)
            {
                bool exists = await _repository.InventoryLocationMaterial
                    .FindByCondition(x => x.LocationId == locId && x.MaterialId == materialId && x.IsActive)
                    .AnyAsync(cancellationToken);
                if (!exists)
                {
                    _repository.InventoryLocationMaterial.Create(new InventoryLocationMaterial
                    {
                        Id = Guid.NewGuid(),
                        LocationId = locId,
                        MaterialId = materialId,
                        IsActive = true
                    });
                }
            }
        }

        private async Task<int> ReconnectWaitingDraftsAsync(
            Guid buyerId, SilaCulinaryMaterialRequest request, ItemBuyerMaster material, Guid actorUserId, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequestLink> links = await _repository.SilaCulinaryMaterialRequestLink
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.MaterialRequestId == request.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            int count = 0;
            foreach (SilaCulinaryMaterialRequestLink link in links)
            {
                link.Reconnected = true;
                link.ReconnectedOnUtc = DateTime.UtcNow;

                SilaCulinaryMaterialMapping? mapping = await _repository.SilaCulinaryMaterialMapping
                    .FindByConditionTracked(x => x.BuyerId == buyerId && x.DraftId == link.DraftId && x.IngredientTempRef == link.IngredientTempRef && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                // §17: Do not silently replace an already CONFIRMED mapping
                if (mapping != null && mapping.MappingStatus != "CONFIRMED")
                {
                    string prevStatus = mapping.MappingStatus;
                    mapping.MaterialId = material.Id;
                    mapping.MaterialCode = material.MaterialCode;
                    mapping.MaterialDescription = material.Description;
                    mapping.BaseUom = material.BaseUnitOfMeasure;
                    mapping.MatchMethod = "MATERIAL_REQUEST_FULFILLED";
                    mapping.MatchScore = 1.0m;
                    mapping.MappingStatus = "CANDIDATE_READY";
                    mapping.MissingClassification = "NONE";
                    mapping.MissingReason = $"Material {material.MaterialCode} is now available. Chef confirmation required.";
                    mapping.MappingRevision += 1;

                    _repository.SilaCulinaryMaterialMappingHistory.Create(new SilaCulinaryMaterialMappingHistory
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyerId,
                        MappingId = mapping.Id,
                        DraftId = mapping.DraftId,
                        IngredientTempRef = mapping.IngredientTempRef,
                        MappingRevision = mapping.MappingRevision,
                        Action = "MATERIAL_RECONNECTED_FOR_CONFIRMATION",
                        PreviousStatus = prevStatus,
                        NewStatus = mapping.MappingStatus,
                        NewMaterialCode = material.MaterialCode,
                        Reason = $"Linked from fulfilled Material Request {request.RequestReference}.",
                        ActorUserId = actorUserId,
                        IsActive = true
                    });
                }

                await SilaOperations.UpsertAlertAsync(_repository, buyerId, actorUserId, new SilaOperations.AlertDraft
                {
                    AlertType = "CULINARY_MATERIAL_AVAILABLE",
                    DeduplicationKey = $"MRQ_AVAIL_{request.Id}_{link.DraftId}",
                    Title = $"Requested material available: {material.MaterialCode} ({material.Description})",
                    Message = $"Material request {request.RequestReference} is complete. Open Material Intelligence to confirm mapping.",
                    Severity = SilaOperations.SEVERITY_INFO,
                    SourceModule = "CULINARY_INTELLIGENCE",
                    SourceDocumentType = Common.SILA_REF_CULINARY_MATERIAL_REQUEST,
                    SourceDocumentId = request.Id
                }, cancellationToken);

                AddWorkflowEvent(buyerId, Common.SILA_REF_CULINARY_MATERIAL_MAP, link.DraftId,
                    Common.SILA_AUDIT_CULINARY_MAPPING_REFRESHED,
                    $"Reconnected {link.IngredientTempRef} to newly available material {material.MaterialCode} for chef confirmation.", actorUserId);

                count += 1;
            }
            return count;
        }

        private async Task<(bool IsErpTenant, bool HasErpCreationApi)> CheckErpMaterialCapabilityAsync(
            Guid organizationId, CancellationToken cancellationToken)
        {
            IQueryable<Guid> buyerIds = IntegrationLookup.BuyerIdsOf(_repository, organizationId);
            bool hasGetMaterial = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => buyerIds.Contains(x.BuyerId)
                    && x.ProcessType == IntegrationProcessType.GET_MATERIAL.ToString()
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.IsActive)
                .AnyAsync(cancellationToken);

            // IntegrationProcessType enum has no POST_MATERIAL / SAP creation process configured (§3 & §14)
            return (hasGetMaterial, false);
        }

        private async Task<List<SilaCulinaryDuplicateCandidateDto>> RunDuplicateScreeningAsync(
            Guid buyerId, string description, string? barcode, Guid? propertyId, string requestType, Guid? targetMaterialId, CancellationToken cancellationToken)
        {
            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, bool> plantMap = await LoadPlantEligibilityAsync(
                buyerId, propertyId, materials.Select(m => m.Id).ToList(), cancellationToken);
            return FindDuplicateCandidatesLocal(description, barcode, materials, plantMap);
        }

        private static List<SilaCulinaryDuplicateCandidateDto> FindDuplicateCandidatesLocal(
            string description, string? barcode, List<ItemBuyerMaster> materials, Dictionary<Guid, bool> plantMap)
        {
            string normTarget = NormalizeKey(description);
            string[] tokens = normTarget.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            List<SilaCulinaryDuplicateCandidateDto> list = new();

            foreach (ItemBuyerMaster m in materials)
            {
                double sim = 0d;
                if (!string.IsNullOrWhiteSpace(barcode) && string.Equals(m.Barcode, barcode.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    sim = 1.0;
                }
                else
                {
                    string mNorm = NormalizeKey(m.Description);
                    if (mNorm == normTarget)
                    {
                        sim = 1.0;
                    }
                    else
                    {
                        string[] mTokens = mNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        int overlap = tokens.Intersect(mTokens, StringComparer.OrdinalIgnoreCase).Count();
                        int union = tokens.Union(mTokens, StringComparer.OrdinalIgnoreCase).Count();
                        if (union > 0) sim = (double)overlap / union;
                    }
                }

                if (sim >= 0.55)
                {
                    bool eligible = plantMap.GetValueOrDefault(m.Id, true);
                    string reason = !m.IsActive ? "INACTIVE_MATERIAL_FOUND" : (!eligible ? "NOT_EXTENDED_TO_PLANT" : "SIMILAR_ACTIVE_MATERIAL");
                    list.Add(new SilaCulinaryDuplicateCandidateDto
                    {
                        MaterialId = m.Id,
                        MaterialCode = m.MaterialCode,
                        Description = m.Description,
                        Similarity = Math.Round(sim, 3),
                        IsActive = m.IsActive,
                        PlantEligible = eligible,
                        Reason = reason
                    });
                }
            }
            return list.OrderByDescending(x => x.Similarity).Take(8).ToList();
        }

        private async Task<SilaCulinaryMaterialRequest?> FindMatchingPendingRequestAsync(
            Guid buyerId, string normalizedDesc, string requestType, Guid? propertyId, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequest> active = await _repository.SilaCulinaryMaterialRequest
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.RequestType == requestType)
                .ToListAsync(cancellationToken);

            return active.FirstOrDefault(r =>
                ActiveRequestStatuses.Contains(r.Status)
                && NormalizeKey(r.ProposedDescription) == normalizedDesc
                && (!propertyId.HasValue || r.PropertyId == propertyId || !r.PropertyId.HasValue));
        }

        private async Task AttachLinksToRequestAsync(
            Guid buyerId, SilaCulinaryMaterialRequest request, List<SilaCulinaryMaterialRequestLinkInputDto> links, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequestLink> existing = await _repository.SilaCulinaryMaterialRequestLink
                .FindByCondition(x => x.BuyerId == buyerId && x.MaterialRequestId == request.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (SilaCulinaryMaterialRequestLinkInputDto l in links)
            {
                if (existing.Any(e => e.DraftId == l.DraftId && string.Equals(e.IngredientTempRef, l.IngredientTempRef, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                _repository.SilaCulinaryMaterialRequestLink.Create(new SilaCulinaryMaterialRequestLink
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    MaterialRequestId = request.Id,
                    DraftId = l.DraftId,
                    IngredientTempRef = l.IngredientTempRef,
                    IngredientDescription = string.IsNullOrWhiteSpace(l.IngredientDescription) ? request.ProposedDescription : l.IngredientDescription.Trim(),
                    RequiredQuantity = l.RequiredQuantity,
                    RecipeUom = NormalizeUom(l.RecipeUom),
                    IsActive = true
                });
            }
        }

        private async Task<List<SilaCulinaryMaterialRequestDto>> HydrateMaterialRequestsAsync(
            BuyerBusinessProfile buyer, List<SilaCulinaryMaterialRequest> requests, CancellationToken cancellationToken)
        {
            if (requests.Count == 0) return new List<SilaCulinaryMaterialRequestDto>();
            List<Guid> reqIds = requests.Select(r => r.Id).ToList();

            List<SilaCulinaryMaterialRequestLink> links = await _repository.SilaCulinaryMaterialRequestLink
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && reqIds.Contains(x.MaterialRequestId))
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMaterialRequestHistory> histories = await _repository.SilaCulinaryMaterialRequestHistory
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && reqIds.Contains(x.MaterialRequestId))
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            List<Guid> draftIds = links.Select(l => l.DraftId).Distinct().ToList();
            Dictionary<Guid, SilaCulinaryRecipeDraft> draftById = draftIds.Count > 0
                ? await _repository.SilaCulinaryRecipeDraft
                    .FindByCondition(x => x.BuyerId == buyer.Id && draftIds.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, SilaCulinaryRecipeDraft>();

            (bool isErp, bool hasCreateApi) = await CheckErpMaterialCapabilityAsync(buyer.OrganizationId, cancellationToken);

            ILookup<Guid, SilaCulinaryMaterialRequestLink> linkLookup = links.ToLookup(l => l.MaterialRequestId);
            ILookup<Guid, SilaCulinaryMaterialRequestHistory> histLookup = histories.ToLookup(h => h.MaterialRequestId);

            return requests.Select(r => new SilaCulinaryMaterialRequestDto
            {
                Id = r.Id,
                BuyerId = r.BuyerId,
                RequestReference = r.RequestReference,
                RequestType = r.RequestType,
                Status = r.Status,
                RevisionNumber = r.RevisionNumber,
                ApprovedRevisionNumber = r.ApprovedRevisionNumber,
                ProposedDescription = r.ProposedDescription,
                LongDescription = r.LongDescription,
                IngredientCategory = r.IngredientCategory,
                MaterialType = r.MaterialType,
                MaterialGroup = r.MaterialGroup,
                ProductHierarchy = r.ProductHierarchy,
                Brand = r.Brand,
                Manufacturer = r.Manufacturer,
                ProductSpecification = r.ProductSpecification,
                Barcode = r.Barcode,
                CountryOfOrigin = r.CountryOfOrigin,
                PropertyId = r.PropertyId,
                PropertyName = r.PropertyName,
                CompanyCode = r.CompanyCode,
                PlantCode = r.PlantCode,
                StorageLocation = r.StorageLocation,
                PurchasingOrganization = r.PurchasingOrganization,
                PurchasingGroup = r.PurchasingGroup,
                BaseUom = r.BaseUom,
                PurchasingUom = r.PurchasingUom,
                AlternateUom = r.AlternateUom,
                PackSize = r.PackSize,
                ConversionNumerator = r.ConversionNumerator,
                ConversionDenominator = r.ConversionDenominator,
                NetWeight = r.NetWeight,
                GrossWeight = r.GrossWeight,
                Volume = r.Volume,
                IngredientForm = r.IngredientForm,
                StorageRequirements = r.StorageRequirements,
                ShelfLifeDays = r.ShelfLifeDays,
                BatchManaged = r.BatchManaged,
                PotentialAllergens = r.PotentialAllergens,
                DietaryClassification = r.DietaryClassification,
                QualitySpecifications = r.QualitySpecifications,
                FoodSafetyRequirements = r.FoodSafetyRequirements,
                RequiredQuantity = r.RequiredQuantity,
                RequiredDateUtc = r.RequiredDateUtc,
                RequestedByUserId = r.RequestedByUserId,
                RequestedByName = r.RequestedByName,
                BusinessJustification = r.BusinessJustification,
                SuggestedSupplier = r.SuggestedSupplier,
                TargetMaterialId = r.TargetMaterialId,
                TargetMaterialCode = r.TargetMaterialCode,
                DataOrigin = r.DataOrigin,
                IdempotencyKey = r.IdempotencyKey,
                CorrelationId = r.CorrelationId,
                ExecutionReference = r.ExecutionReference,
                ExecutionStatus = r.ExecutionStatus,
                ResultingMaterialId = r.ResultingMaterialId,
                ResultingMaterialCode = r.ResultingMaterialCode,
                LastError = r.LastError,
                DecisionComment = r.DecisionComment,
                IsErpIntegratedTenant = isErp,
                HasErpCreationApi = hasCreateApi,
                DuplicateCandidates = DeserializeSafe<List<SilaCulinaryDuplicateCandidateDto>>(r.DuplicateCheckJson) ?? new(),
                Attachments = DeserializeSafe<List<string>>(r.AttachmentsJson) ?? new(),
                Links = linkLookup[r.Id].Select(l =>
                {
                    draftById.TryGetValue(l.DraftId, out SilaCulinaryRecipeDraft? dr);
                    return new SilaCulinaryMaterialRequestLinkDto
                    {
                        Id = l.Id,
                        DraftId = l.DraftId,
                        DraftReference = dr?.DraftReference,
                        RecipeName = dr?.RecipeName,
                        IngredientTempRef = l.IngredientTempRef,
                        IngredientDescription = l.IngredientDescription,
                        RequiredQuantity = l.RequiredQuantity,
                        RecipeUom = l.RecipeUom,
                        Reconnected = l.Reconnected,
                        ChefConfirmed = l.ChefConfirmed,
                        ReconnectedOnUtc = l.ReconnectedOnUtc
                    };
                }).ToList(),
                History = histLookup[r.Id].Select(h => new SilaCulinaryMaterialRequestHistoryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    ActorUserId = h.ActorUserId,
                    ActorName = h.ActorName,
                    Comment = h.Comment,
                    CorrelationId = h.CorrelationId,
                    DateCreated = h.DateCreated
                }).ToList(),
                SubmittedOnUtc = r.SubmittedOnUtc,
                ApprovedOnUtc = r.ApprovedOnUtc,
                MaterialAvailableOnUtc = r.MaterialAvailableOnUtc,
                DateCreated = r.DateCreated,
                DateUpdated = r.DateUpdated
            }).ToList();
        }

        private async Task<SilaCulinaryMaterialRequest> LoadTrackedRequestAsync(Guid buyerId, Guid requestId)
        {
            return await _repository.SilaCulinaryMaterialRequest
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == requestId && x.IsActive)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundCustomException("Material request not found.", "The requested material request does not exist in your tenant.");
        }

        private async Task<SilaCulinaryRecipeDraft> LoadAuthorizedDraftAsync(
            Guid buyerId, Guid userId, Guid roleId, Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeDraft draft = await _repository.SilaCulinaryRecipeDraft
                .FindFirstByConditionAsync(x => x.Id == draftId && x.BuyerId == buyerId && x.IsActive)
                ?? throw new NotFoundCustomException("AI Recipe Draft not found.", "The requested recipe draft does not exist in your tenant.");

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

        private async Task<SilaCulinaryRecipeLifecycle> GetOrCreateLifecycleAsync(
            Guid buyerId, SilaCulinaryRecipeDraft draft, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.DraftId == draft.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (lc == null)
            {
                lc = new SilaCulinaryRecipeLifecycle
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    DraftId = draft.Id,
                    ApprovedDraftRevision = draft.RevisionNumber,
                    LifecycleStatus = "DRAFT",
                    CorrelationId = Guid.NewGuid().ToString("N")[..16],
                    IsActive = true
                };
                _repository.SilaCulinaryRecipeLifecycle.Create(lc);
            }
            return lc;
        }

        private async Task<SilaCulinaryAiConfiguration> LoadAiConfigAsync(Guid buyerId)
        {
            return await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive)
                ?? SilaCulinaryAiSettings.CreateDefault(buyerId);
        }

        private async Task<(string? PropertyName, string? CompanyCode, string? PlantCode)> ResolvePropertyOrgAsync(
            Guid buyerId, Guid? propertyId, CancellationToken cancellationToken)
        {
            if (!propertyId.HasValue) return (null, null, null);
            BuyerProperty? prop = await _repository.BuyerProperty
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.Id == propertyId.Value && x.IsActive);
            return (prop?.PropertyName, prop?.CompanyCode, prop?.PlantCode);
        }

        private async Task<Dictionary<Guid, bool>> LoadPlantEligibilityAsync(
            Guid buyerId, Guid? propertyId, List<Guid> materialIds, CancellationToken cancellationToken)
        {
            Dictionary<Guid, bool> map = materialIds.ToDictionary(id => id, _ => true);
            if (!propertyId.HasValue || materialIds.Count == 0) return map;

            List<Guid> locIds = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && x.PropertyId == propertyId.Value)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            if (locIds.Count == 0) return map;

            bool anyStocking = await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId))
                .AnyAsync(cancellationToken);
            if (!anyStocking) return map;

            HashSet<Guid> withAny = (await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId))
                .Select(x => x.MaterialId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
            HashSet<Guid> atProp = (await _repository.InventoryLocationMaterial
                .FindByCondition(x => x.IsActive && locIds.Contains(x.LocationId) && materialIds.Contains(x.MaterialId))
                .Select(x => x.MaterialId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();

            foreach (Guid id in materialIds)
            {
                if (withAny.Contains(id) && !atProp.Contains(id))
                {
                    map[id] = false;
                }
            }
            return map;
        }

        private static (decimal? Price, string Currency, string Source) ResolveMaterialPrice(
            ItemBuyerMaster mat, MaterialOutletPrice? mop, List<string> hierarchy, string defaultCurrency)
        {
            string curr = mat.Currency ?? defaultCurrency;
            foreach (string src in hierarchy)
            {
                switch (src.ToUpperInvariant())
                {
                    case "OUTLET_PRICE":
                        if (mop != null && mop.UnitCost > 0m) return (mop.UnitCost, mop.Currency ?? curr, "OUTLET_PRICE");
                        break;
                    case "MOVING_AVERAGE_PRICE":
                        if (mat.MovingAveragePrice.HasValue && mat.MovingAveragePrice > 0m) return (mat.MovingAveragePrice.Value, curr, "MOVING_AVERAGE_PRICE");
                        break;
                    case "STANDARD_PRICE":
                        if (mat.StandardPrice.HasValue && mat.StandardPrice > 0m) return (mat.StandardPrice.Value, curr, "STANDARD_PRICE");
                        break;
                    case "UNIT_COST":
                        if (mat.UnitCost.HasValue && mat.UnitCost > 0m) return (mat.UnitCost.Value, curr, "UNIT_COST");
                        break;
                }
            }
            return (null, curr, "MISSING");
        }

        private static (bool Ok, decimal? Qty, string Reason) ConvertUomDeterministic(
            decimal qty, string fromUom, string toUom, List<MaterialUomConversion> conversions)
        {
            string f = NormalizeUom(fromUom);
            string t = NormalizeUom(toUom);
            if (f == t) return (true, Math.Round(qty, 4, MidpointRounding.AwayFromZero), "Exact UOM");
            if ((f == "G" && t == "KG") || (f == "ML" && t == "L")) return (true, Math.Round(qty * 0.001m, 4, MidpointRounding.AwayFromZero), "Metric /1000");
            if ((f == "KG" && t == "G") || (f == "L" && t == "ML")) return (true, Math.Round(qty * 1000m, 4, MidpointRounding.AwayFromZero), "Metric *1000");

            MaterialUomConversion? direct = conversions.FirstOrDefault(c => NormalizeUom(c.FromUom) == f && NormalizeUom(c.ToUom) == t && c.Factor > 0m);
            if (direct != null) return (true, Math.Round(qty * direct.Factor, 4, MidpointRounding.AwayFromZero), "Direct conversion");
            MaterialUomConversion? rev = conversions.FirstOrDefault(c => NormalizeUom(c.FromUom) == t && NormalizeUom(c.ToUom) == f && c.Factor > 0m);
            if (rev != null) return (true, Math.Round(qty / rev.Factor, 4, MidpointRounding.AwayFromZero), "Reverse conversion");

            return (false, null, $"No verified conversion between {fromUom} and {toUom}.");
        }

        private static SilaCulinaryRecipeLifecycleSummaryDto MapLifecycleDto(SilaCulinaryRecipeLifecycle lc, Recipe? prod) => new()
        {
            Id = lc.Id,
            DraftId = lc.DraftId,
            ApprovedDraftRevision = lc.ApprovedDraftRevision,
            CostingSnapshotId = lc.CostingSnapshotId,
            ProductionRecipeId = lc.ProductionRecipeId,
            ProductionRecipeCode = lc.ProductionRecipeCode ?? prod?.RecipeCode,
            ProductionRecipeVersion = lc.ProductionRecipeVersion ?? prod?.Version,
            ProductionActiveVersion = prod?.ActiveVersion,
            ProductionStatus = prod?.Status,
            LifecycleStatus = lc.LifecycleStatus,
            ImageAssigned = lc.ImageAssigned,
            ImageOrigin = lc.ImageOrigin,
            ApprovalComment = lc.ApprovalComment,
            RejectionReason = lc.RejectionReason,
            ChangeReason = lc.ChangeReason,
            ApprovedByName = lc.ApprovedByName,
            ApprovedOnUtc = lc.ApprovedOnUtc,
            PublishedOnUtc = lc.PublishedOnUtc,
            EffectiveDateUtc = lc.EffectiveDateUtc
        };

        private CulinaryStructuredRecipePayloadDto ParseDraftPayload(SilaCulinaryRecipeDraft draft)
        {
            try
            {
                return JsonSerializer.Deserialize<CulinaryStructuredRecipePayloadDto>(draft.PayloadJson, SnakeJson)
                    ?? new CulinaryStructuredRecipePayloadDto();
            }
            catch
            {
                return new CulinaryStructuredRecipePayloadDto();
            }
        }

        private async Task<T> PostCulinaryEngineAsync<T>(string path, object body, int timeoutSeconds, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120)));

            string baseUrl = (_configuration[Common.CULINARY_ENGINE_URL] ?? "http://127.0.0.1:8013").TrimEnd('/');
            string json = JsonSerializer.Serialize(body, SnakeJson);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync($"{baseUrl}{path}", content, cts.Token);
            string raw = await response.Content.ReadAsStringAsync(cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Culinary engine error ({(int)response.StatusCode}): {raw}");
            }
            return JsonSerializer.Deserialize<T>(raw, SnakeJson)
                ?? throw new InvalidOperationException("Culinary engine returned empty JSON.");
        }

        private void AddWorkflowEvent(Guid buyerId, string refType, Guid refId, string action, string comment, Guid actorUserId)
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

        private static string NormalizeRequestType(string? raw)
        {
            string val = string.IsNullOrWhiteSpace(raw) ? "NEW_MATERIAL" : raw.Trim().ToUpperInvariant();
            if (!ValidRequestTypes.Contains(val))
            {
                throw new BadRequestCustomException("Invalid material request type.", $"Supported types: {string.Join(", ", ValidRequestTypes)}.");
            }
            return val;
        }

        private static string NormalizeOrigin(string? raw)
        {
            string val = string.IsNullOrWhiteSpace(raw) ? "CHEF_ENTERED" : raw.Trim().ToUpperInvariant();
            return val is "AI_SUGGESTED" or "CHEF_ENTERED" or "EXISTING_MASTER_DATA" or "MASTER_DATA_VERIFIED" or "APPROVED"
                ? val
                : "CHEF_ENTERED";
        }

        private static string NormalizeUom(string? uom)
        {
            string u = (uom ?? "KG").Trim().ToUpperInvariant();
            return u switch
            {
                "KGS" or "KILOGRAM" or "KILOGRAMS" => "KG",
                "GM" or "GMS" or "GRAM" or "GRAMS" => "G",
                "LTR" or "LTRS" or "LITRE" or "LITER" => "L",
                "MLS" or "MILLILITER" or "MILLILITRE" => "ML",
                "PC" or "PCS" or "EACH" or "UNIT" => "EA",
                _ => u
            };
        }

        private static string NormalizeKey(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string[] parts = text.Trim().ToLowerInvariant().Split(new[] { ' ', '-', '_', ',', '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }

        private static string? TrimOrNull(string? val, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(val)) return null;
            string t = val.Trim();
            return t.Length > maxLen ? t[..maxLen] : t;
        }

        private static T? DeserializeSafe<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try { return JsonSerializer.Deserialize<T>(json, CamelJson); }
            catch { return default; }
        }

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

        private static byte[] BuildSimpleTextPdf(string title, List<string> lines)
        {
            StringBuilder stream = new();
            stream.AppendLine("BT");
            stream.AppendLine("/F1 12 Tf");
            stream.AppendLine("40 770 Td");
            stream.AppendLine($"({EscapePdf(title)}) Tj");
            stream.AppendLine("/F1 9 Tf");
            int count = 0;
            foreach (string line in lines.Take(48))
            {
                stream.AppendLine("0 -14 Td");
                stream.AppendLine($"({EscapePdf(line.Length > 105 ? line[..105] : line)}) Tj");
                count++;
            }
            stream.AppendLine("ET");
            string streamContent = stream.ToString();

            List<string> objs = new()
            {
                "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
                "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
                "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj",
                $"4 0 obj << /Length {Encoding.ASCII.GetByteCount(streamContent)} >> stream\n{streamContent}endstream endobj",
                "5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj"
            };

            StringBuilder pdf = new();
            pdf.AppendLine("%PDF-1.4");
            List<int> offsets = new() { 0 };
            foreach (string obj in objs)
            {
                offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
                pdf.AppendLine(obj);
            }
            int xrefPos = Encoding.ASCII.GetByteCount(pdf.ToString());
            pdf.AppendLine($"xref\n0 {objs.Count + 1}");
            pdf.AppendLine("0000000000 65535 f ");
            foreach (int off in offsets.Skip(1))
            {
                pdf.AppendLine($"{off:D10} 00000 n ");
            }
            pdf.AppendLine($"trailer << /Size {objs.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static string EscapePdf(string s) =>
            s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", " ").Replace("\n", " ");

        // Python assist JSON contract
        private sealed class PythonAssistResponse
        {
            public string RecommendedRequestType { get; set; } = "NEW_MATERIAL";
            public string ProposedDescription { get; set; } = string.Empty;
            public string ProposedLongDescription { get; set; } = string.Empty;
            public string IngredientCategory { get; set; } = string.Empty;
            public string ProposedMaterialGroup { get; set; } = string.Empty;
            public string ProposedBaseUom { get; set; } = "KG";
            public string ProductSpecification { get; set; } = string.Empty;
            public string StorageRequirement { get; set; } = string.Empty;
            public List<string>? PotentialAllergens { get; set; }
            public string BusinessJustification { get; set; } = string.Empty;
            public List<PythonDupItem>? PotentialDuplicates { get; set; }
            public string ProviderUsed { get; set; } = "LOCAL_DETERMINISTIC";
            public bool AiSuggested { get; set; } = true;
        }

        private sealed class PythonDupItem
        {
            public string? MaterialId { get; set; }
            public string MaterialCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public double Similarity { get; set; }
            public bool IsActive { get; set; }
            public bool PlantEligible { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
