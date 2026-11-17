using System.Text.Json;
using Buyer.Application.Features.Queries.ResolveIntegration;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;
using SharedKernel.Integration.Rules;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.PullSilaMaterialsFromErp
{
    /// <summary>
    /// Plant-first Material Master pull through GET_MATERIAL (ResolveIntegration: company then ALL).
    /// Company code is derived from BuyerProperty when only plants are supplied.
    /// </summary>
    public class PullSilaMaterialsFromErpCommandHandler : IRequestHandler<PullSilaMaterialsFromErpCommand, SilaMaterialErpPullResultDto>
    {
        private const int MAX_COMPANY_CODE_LENGTH = 20;

        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly IIntegrationHttpExecutor _executor;
        private readonly ILoggerManager _logger;

        public PullSilaMaterialsFromErpCommandHandler(
            IRepositoryWrapper repository, IMediator mediator, IIntegrationHttpExecutor executor, ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _executor = executor;
            _logger = logger;
        }

        public async Task<SilaMaterialErpPullResultDto> Handle(PullSilaMaterialsFromErpCommand request, CancellationToken cancellationToken)
        {
            string? productId = NormalizeProductId(request.Request.ProductId);
            _logger.LogInfo(
                $"Pulling materials from the ERP. OrganizationId: {request.OrganizationId}, CompanyCode: {request.Request.CompanyCode}, Plants: {(request.Request.PlantCodes == null ? 0 : request.Request.PlantCodes.Count)}, ProductId: {productId ?? "(all)"}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);

            List<BuyerProperty> properties = await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<string> plantCodes = (request.Request.PlantCodes ?? new List<string>())
                .Select(x => x.Trim().ToUpperInvariant())
                .Where(x => x.Length > 0 && x.Length <= MAX_COMPANY_CODE_LENGTH)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string companyCode = request.Request.CompanyCode?.Trim().ToUpperInvariant() ?? string.Empty;

            Dictionary<string, List<BuyerProperty>> propertiesByPlant = properties
                .Where(x => !string.IsNullOrWhiteSpace(x.PlantCode))
                .GroupBy(x => x.PlantCode.Trim().ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            // Auto-map missing plants onto BuyerProperty when company code is known (Material Master pull).
            if (plantCodes.Count > 0)
            {
                List<string> unknown = plantCodes.Where(x => !propertiesByPlant.ContainsKey(x)).ToList();
                if (unknown.Count > 0)
                {
                    string companyForMap = companyCode.Length > 0 && companyCode != "ALL"
                        ? companyCode
                        : unknown.Count == 1
                            ? unknown[0]
                            : string.Empty;
                    if (companyForMap.Length == 0)
                    {
                        throw new BadRequestCustomException(
                            "Unknown plant code(s).",
                            $"Plant(s) {string.Join(", ", unknown)} are not mapped. Enter a company code with the plant(s) so they can be mapped, or create a property first.");
                    }

                    foreach (string plant in unknown)
                    {
                        BuyerProperty created = new BuyerProperty
                        {
                            Id = Guid.NewGuid(),
                            BuyerId = buyer.Id,
                            CompanyCode = companyForMap,
                            PlantCode = plant,
                            PropertyName = $"Plant {plant}",
                            MasterApprovalFlowId = null,
                            IsActive = true
                        };
                        _repository.BuyerProperty.Create(created);
                        properties.Add(created);
                        propertiesByPlant[plant] = new List<BuyerProperty> { created };
                        _logger.LogInfo($"Auto-mapped plant to property for ERP pull. Plant: {plant}, Company: {companyForMap}, BuyerId: {buyer.Id}");
                    }

                    await _repository.SaveAsync();
                }
            }

            if (plantCodes.Count > 0 && (companyCode.Length == 0 || companyCode == "ALL"))
            {
                List<string> companies = plantCodes
                    .SelectMany(code => propertiesByPlant.TryGetValue(code, out List<BuyerProperty>? rows) ? rows : new List<BuyerProperty>())
                    .Select(x => x.CompanyCode?.Trim().ToUpperInvariant() ?? string.Empty)
                    .Where(x => x.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (companies.Count == 1)
                {
                    companyCode = companies[0];
                }
                else if (companyCode.Length == 0)
                {
                    companyCode = "ALL";
                }
            }

            // Both blank (no plant, no Product ID): preserve existing company/configured-plant pull (company or plants required).
            if (companyCode.Length == 0 && plantCodes.Count == 0 && productId == null)
            {
                _logger.LogError($"ERP material pull without plant, product or company. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException(
                    "Plant, Product ID or company is required.",
                    "Enter a plant and/or Product ID (and company if needed), or map properties first.");
            }

            // Product ID without plant: must stay inside authorized configured BuyerProperty plants (or company for ResolveIntegration).
            if (productId != null && plantCodes.Count == 0 && propertiesByPlant.Count == 0 && companyCode.Length == 0)
            {
                throw new BadRequestCustomException(
                    "Authorized plant scope is required.",
                    "Product ID pull needs at least one configured BuyerProperty plant or a company code.");
            }

            if (companyCode.Length > MAX_COMPANY_CODE_LENGTH)
            {
                throw new BadRequestCustomException("Company code is invalid.", $"Company code must be at most {MAX_COMPANY_CODE_LENGTH} characters.");
            }

            if (companyCode.Length == 0)
            {
                companyCode = "ALL";
            }

            IntegrationApiDto api = await _mediator.Send(new ResolveIntegrationQuery
            {
                OrganizationId = request.OrganizationId,
                ProcessType = IntegrationProcessType.GET_MATERIAL,
                EntityCode = companyCode
            }, cancellationToken);
            if (!api.Configured || api.ConfigurationId == null)
            {
                _logger.LogError($"No active GET_MATERIAL API. OrganizationId: {request.OrganizationId}, CompanyCode: {companyCode}");
                throw new BadRequestCustomException(
                    "The ERP material API is not configured.",
                    $"Configure and activate a GET_MATERIAL API for company code {companyCode} (or ALL) under Integration first.");
            }

            bool claimed = await _repository.ApiIntegrationConfiguration.TryClaimAsync(api.ConfigurationId.Value, request.OrganizationId, cancellationToken);
            if (!claimed)
            {
                _logger.LogError($"GET_MATERIAL API is busy. ConfigurationId: {api.ConfigurationId}");
                throw new ConflictCustomException("The ERP material pull is already running.", "Wait until the current pull finishes, then try again.");
            }

            ApiIntegrationConfiguration configuration = await IntegrationLookup.GetTrackedAsync(_repository, _logger, api.ConfigurationId.Value, request.OrganizationId);
            ApiIntegrationExecution execution = IntegrationRunRules.NewExecution(configuration, IntegrationExecutionTrigger.MANUAL, true);
            _repository.ApiIntegrationExecution.Create(execution);
            SilaMaterialErpPullResultDto result = new SilaMaterialErpPullResultDto { ConfigurationName = configuration.Name };
            try
            {
                List<ApiFieldMapping> mappings = await _repository.ApiFieldMapping
                    .FindByCondition(x => x.ConfigurationId == configuration.Id)
                    .ToListAsync(cancellationToken);
                if (!mappings.Any(x => x.TargetField.Equals("Material.Code", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new IntegrationException("MAPPING_REQUIRED", "Map the ERP field that holds the material code to Material.Code before pulling.");
                }

                EnsureDescriptionMapping(mappings, configuration.Id, request.UserId);

                string? originalPath = configuration.ResourcePath;
                List<string> effectivePlants = plantCodes;

                // Product ID + plant eligibility via A_ProductPlant (header has no Plant field).
                if (productId != null)
                {
                    HashSet<string> sapPlants = await FetchProductPlantsAsync(
                        configuration, originalPath, productId, result, cancellationToken);

                    if (plantCodes.Count > 0)
                    {
                        List<string> eligible = plantCodes.Where(p => sapPlants.Contains(p)).ToList();
                        if (eligible.Count == 0)
                        {
                            result.SkippedNotFound++;
                            result.Failures.Add(
                                sapPlants.Count == 0
                                    ? $"{productId}: not assigned in SAP to plant(s) {string.Join(", ", plantCodes)} (A_ProductPlant returned no plants)."
                                    : $"{productId}: not assigned in SAP to plant(s) {string.Join(", ", plantCodes)}. SAP plants: {string.Join(", ", sapPlants.OrderBy(x => x))}.");
                            IntegrationRunRules.Complete(configuration, execution);
                            await _repository.SaveAsync();
                            return result;
                        }

                        effectivePlants = eligible;
                    }
                    else
                    {
                        HashSet<string> authorized = new HashSet<string>(
                            propertiesByPlant.Keys, StringComparer.OrdinalIgnoreCase);
                        List<string> eligible = sapPlants.Where(p => authorized.Contains(p)).OrderBy(x => x).ToList();
                        if (eligible.Count == 0)
                        {
                            result.SkippedNotFound++;
                            result.Failures.Add(
                                sapPlants.Count == 0
                                    ? $"{productId}: no SAP plant assignment found in A_ProductPlant within authorized configured plants."
                                    : $"{productId}: SAP plants {string.Join(", ", sapPlants.OrderBy(x => x))} are outside authorized configured plants.");
                            IntegrationRunRules.Complete(configuration, execution);
                            await _repository.SaveAsync();
                            return result;
                        }

                        effectivePlants = eligible;
                    }
                }

                // Filter A_Product at SAP when Product ID is supplied (do not pull-all then filter in memory).
                configuration.ResourcePath = ApplyProductIdFilter(
                    EnsureProductDescriptionExpand(configuration.ResourcePath), productId);
                List<JsonElement> records;
                try
                {
                    records = await _executor.PullRecordsAsync(configuration, true, cancellationToken);
                }
                finally
                {
                    configuration.ResourcePath = originalPath;
                }

                if (productId != null && records.Count == 0)
                {
                    result.SkippedNotFound++;
                    result.Failures.Add($"{productId}: Product not found on A_Product for the configured GET_MATERIAL connection.");
                    IntegrationRunRules.Complete(configuration, execution);
                    await _repository.SaveAsync();
                    return result;
                }

                // Exact SAP Product IDs (leading zeros preserved) for A_ProductDescription filter.
                List<string> productCodes = productId != null
                    ? new List<string> { productId }
                    : records
                        .Select(record => IntegrationRecordReader.Read(mappings, "Material.Code", record)?.Trim())
                        .Where(code => !string.IsNullOrEmpty(code))
                        .Select(code => code!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                Dictionary<string, string> descriptionsByProduct = await FetchEnProductDescriptionsAsync(
                    configuration, originalPath, productCodes, result, cancellationToken);

                await SilaMaterialErpSync.ApplyAsync(
                    _repository, _logger, buyer.Id, request.UserId, companyCode, mappings, records, result, cancellationToken, effectivePlants, descriptionsByProduct);
                execution.RecordsRead = result.Read;
                execution.RecordsCreated = result.New;
                execution.RecordsUpdated = result.Changed;
                if (result.Failed > 0)
                {
                    execution.RecordsFailed = result.Failed;
                    execution.ErrorMessageSafe = "One or more material records could not be applied.";
                }

                IntegrationRunRules.Complete(configuration, execution);
                await _repository.SaveAsync();
            }
            catch (Exception exception) when (exception is IntegrationException or DbUpdateException or JsonException or BaseCustomException)
            {
                // Nothing of a failed pull is kept: the tracked changes are dropped and only the failure is recorded.
                IntegrationException failure = exception as IntegrationException
                    ?? new IntegrationException("INTEGRATION_FAILED", "The material pull failed. No materials were changed.");
                _logger.LogError($"ERP material pull failed. ConfigurationId: {configuration.Id}, Code: {failure.Code}, Error: {SilaLogText.Short(exception.Message)}");
                SilaRetry.DiscardChanges(_repository);
                ApiIntegrationConfiguration failed = await IntegrationLookup.GetTrackedAsync(_repository, _logger, configuration.Id, request.OrganizationId);
                _repository.ApiIntegrationExecution.Create(IntegrationRunRules.Fail(failed, IntegrationExecutionTrigger.MANUAL, true, execution, failure));
                await _repository.SaveAsync();
                if (exception is BaseCustomException)
                {
                    throw;
                }

                throw IntegrationErrors.ToCustomException(failure);
            }

            _logger.LogInfo(
                $"ERP materials pulled. ConfigurationId: {configuration.Id}, Company: {companyCode}, Plants: {plantCodes.Count}, ProductId: {productId ?? "(all)"}, Read: {result.Read}, New: {result.New}, Changed: {result.Changed}, DescriptionsUpdated: {result.DescriptionsUpdated}, DescriptionsMissing: {result.DescriptionsMissing}, SkippedNotFound: {result.SkippedNotFound}, Failed: {result.Failed}");
            return result;
        }

        /// <summary>Trim only; keep as string so SAP leading zeros are preserved.</summary>
        private static string? NormalizeProductId(string? productId)
        {
            if (productId == null)
            {
                return null;
            }

            string trimmed = productId.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        /// <summary>
        /// Adds <c>Product eq '…'</c> on API_PRODUCT_SRV/A_Product at the SAP request (OData-escaped + URL-encoded).
        /// </summary>
        private static string ApplyProductIdFilter(string? resourcePath, string? productId)
        {
            string path = resourcePath?.Trim() ?? string.Empty;
            if (productId == null || path.Length == 0)
            {
                return path;
            }

            string escaped = productId.Replace("'", "''", StringComparison.Ordinal);
            string clause = $"Product eq '{escaped}'";

            int filterAt = path.IndexOf("$filter=", StringComparison.OrdinalIgnoreCase);
            if (filterAt >= 0)
            {
                int valueStart = filterAt + "$filter=".Length;
                int valueEnd = path.IndexOf('&', valueStart);
                if (valueEnd < 0)
                {
                    valueEnd = path.Length;
                }

                string existingRaw = path[valueStart..valueEnd];
                string existing = Uri.UnescapeDataString(existingRaw);
                string merged = string.IsNullOrWhiteSpace(existing)
                    ? clause
                    : $"({existing}) and ({clause})";
                return path[..valueStart] + Uri.EscapeDataString(merged) + path[valueEnd..];
            }

            string encoded = Uri.EscapeDataString(clause);
            return path + (path.Contains('?', StringComparison.Ordinal) ? "&" : "?") + "$filter=" + encoded;
        }

        /// <summary>
        /// Plant assignments for a Product from <c>API_PRODUCT_SRV/A_ProductPlant</c>
        /// (A_Product header has no Plant field).
        /// </summary>
        private async Task<HashSet<string>> FetchProductPlantsAsync(
            ApiIntegrationConfiguration configuration,
            string? productResourcePath,
            string productId,
            SilaMaterialErpPullResultDto result,
            CancellationToken cancellationToken)
        {
            HashSet<string> plants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? serviceRoot = TryApiProductServiceRoot(productResourcePath ?? configuration.ResourcePath);
            if (serviceRoot == null)
            {
                result.Failures.Add("Product plant eligibility skipped: resource path is not API_PRODUCT_SRV/A_Product.");
                return plants;
            }

            string escaped = productId.Replace("'", "''", StringComparison.Ordinal);
            string filter = $"Product eq '{escaped}'";
            string? originalPath = configuration.ResourcePath;
            try
            {
                configuration.ResourcePath =
                    $"{serviceRoot}A_ProductPlant?$filter={Uri.EscapeDataString(filter)}&$format=json";
                List<JsonElement> rows = await _executor.PullRecordsAsync(configuration, true, cancellationToken);
                foreach (JsonElement row in rows)
                {
                    string? plant = ReadJsonText(row, "Plant")?.Trim().ToUpperInvariant();
                    if (!string.IsNullOrEmpty(plant))
                    {
                        plants.Add(plant);
                    }
                }

                _logger.LogInfo(
                    $"Loaded A_ProductPlant for Product {productId}. Plants: {plants.Count}, ConfigurationId: {configuration.Id}");
            }
            catch (Exception exception) when (exception is IntegrationException or JsonException or HttpRequestException)
            {
                _logger.LogError(
                    $"A_ProductPlant fetch failed. Product: {productId}, ConfigurationId: {configuration.Id}, Error: {SilaLogText.Short(exception.Message)}");
                result.Failures.Add(
                    $"Could not load A_ProductPlant for {productId}: {SilaLogText.Short(exception.Message)}.");
            }
            finally
            {
                configuration.ResourcePath = originalPath;
            }

            return plants;
        }

        /// <summary>
        /// Ensures Material.Description is mapped for S/4 Product description expand when the Hub row is missing.
        /// Does not overwrite an operator-configured source path.
        /// </summary>
        private void EnsureDescriptionMapping(List<ApiFieldMapping> mappings, Guid configurationId, Guid userId)
        {
            if (mappings.Any(x => x.TargetField.Equals("Material.Description", StringComparison.OrdinalIgnoreCase) && x.IsActive))
            {
                return;
            }

            ApiFieldMapping mapping = new ApiFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId,
                SourceField = "to_Description/results/0/ProductDescription",
                TargetField = "Material.Description",
                Transformation = "TRIM",
                NullPolicy = IntegrationNullPolicy.IGNORE_NULL,
                IsValidated = true,
                IsActive = true
            };
            _repository.ApiFieldMapping.Create(mapping);
            mappings.Add(mapping);
            _logger.LogInfo($"Added default Material.Description mapping for GET_MATERIAL. ConfigurationId: {configurationId}, UserId: {userId}");
        }

        /// <summary>
        /// Batched EN product texts from
        /// <c>API_PRODUCT_SRV/A_ProductDescription?$filter=Product eq '…' and Language eq 'EN'&amp;$format=json</c>.
        /// Chunks Product IDs (leading zeros preserved) to avoid N+1 SAP calls; URL-encodes the OData filter.
        /// </summary>
        private async Task<Dictionary<string, string>> FetchEnProductDescriptionsAsync(
            ApiIntegrationConfiguration configuration,
            string? productResourcePath,
            List<string> productCodes,
            SilaMaterialErpPullResultDto result,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (productCodes.Count == 0)
            {
                return map;
            }

            string? serviceRoot = TryApiProductServiceRoot(productResourcePath ?? configuration.ResourcePath);
            if (serviceRoot == null)
            {
                result.Failures.Add("Product description entity skipped: resource path is not API_PRODUCT_SRV/A_Product.");
                return map;
            }

            string? originalPath = configuration.ResourcePath;
            const int chunkSize = 20;
            try
            {
                for (int offset = 0; offset < productCodes.Count; offset += chunkSize)
                {
                    List<string> chunk = productCodes.Skip(offset).Take(chunkSize).ToList();
                    // Exact Product IDs in filter (preserve leading zeros); Language = 'EN' exactly.
                    string productFilter = string.Join(
                        " or ",
                        chunk.Select(code => $"Product eq '{code.Replace("'", "''", StringComparison.Ordinal)}'"));
                    string filter = $"Language eq 'EN' and ({productFilter})";
                    configuration.ResourcePath =
                        $"{serviceRoot}A_ProductDescription?$filter={Uri.EscapeDataString(filter)}&$format=json";

                    List<JsonElement> rows = await _executor.PullRecordsAsync(configuration, true, cancellationToken);
                    foreach (JsonElement row in rows)
                    {
                        string? language = ReadJsonText(row, "Language")?.Trim();
                        if (!string.IsNullOrEmpty(language)
                            && !language.Equals("EN", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Keep Product key with leading zeros; dictionary is case-insensitive for join.
                        string? product = ReadJsonText(row, "Product")?.Trim();
                        string? text = ReadJsonText(row, "ProductDescription")?.Trim();
                        if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        map[product] = text;
                    }
                }

                _logger.LogInfo(
                    $"Loaded EN product descriptions from A_ProductDescription. Products: {productCodes.Count}, Descriptions: {map.Count}, ConfigurationId: {configuration.Id}");
                if (map.Count == 0 && productCodes.Count > 0)
                {
                    result.Failures.Add(
                        "A_ProductDescription returned no EN ProductDescription rows for the pulled Product IDs (Language eq 'EN').");
                }
            }
            catch (Exception exception) when (exception is IntegrationException or JsonException or HttpRequestException)
            {
                _logger.LogError(
                    $"A_ProductDescription fetch failed. ConfigurationId: {configuration.Id}, Error: {SilaLogText.Short(exception.Message)}");
                result.Failures.Add(
                    $"Could not load A_ProductDescription (Language EN): {SilaLogText.Short(exception.Message)}.");
            }
            finally
            {
                configuration.ResourcePath = originalPath;
            }

            return map;
        }

        private static string? TryApiProductServiceRoot(string? resourcePath)
        {
            string path = resourcePath?.Trim() ?? string.Empty;
            if (path.Length == 0)
            {
                return null;
            }

            int marker = path.IndexOf("API_PRODUCT_SRV", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                return null;
            }

            int end = marker + "API_PRODUCT_SRV".Length;
            string root = path[..end].TrimEnd('/') + "/";
            return root;
        }

        private static string? ReadJsonText(JsonElement row, string name)
        {
            if (row.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (row.TryGetProperty(name, out JsonElement value))
            {
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
            }

            foreach (JsonProperty property in row.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Adds $expand=to_Description on API_PRODUCT_SRV/A_Product as a first pass; EN text is then
        /// filled from A_ProductDescription when expand is empty.
        /// </summary>
        private static string EnsureProductDescriptionExpand(string? resourcePath)
        {
            string path = resourcePath?.Trim() ?? string.Empty;
            if (path.Length == 0)
            {
                return path;
            }

            // Only expand the product header entity, never A_ProductDescription itself.
            if (path.IndexOf("A_ProductDescription", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return path;
            }

            if (path.IndexOf("A_Product", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return path;
            }

            if (path.IndexOf("to_Description", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return path;
            }

            if (path.IndexOf("$expand=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int expandAt = path.IndexOf("$expand=", StringComparison.OrdinalIgnoreCase);
                int valueStart = expandAt + "$expand=".Length;
                int valueEnd = path.IndexOf('&', valueStart);
                if (valueEnd < 0) valueEnd = path.Length;
                string current = path[valueStart..valueEnd];
                string merged = string.IsNullOrWhiteSpace(current) ? "to_Description" : $"{current},to_Description";
                return path[..valueStart] + merged + path[valueEnd..];
            }

            return path + (path.Contains('?') ? "&" : "?") + "$expand=to_Description";
        }
    }
}
