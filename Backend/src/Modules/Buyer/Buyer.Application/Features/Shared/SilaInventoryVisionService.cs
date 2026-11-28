using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaInventoryVisionService
    {
        /// <summary>Reads the liquid level of one photographed bottle. Nothing is posted; the caller confirms a count.</summary>
        Task<SilaInventoryBottleVisionDto> AnalyzeBottleAsync(
            Guid organizationId, Guid userId, IFormFile file, double? nominalMl, string? description, CancellationToken cancellationToken);
    }

    public class SilaInventoryVisionService : ISilaInventoryVisionService
    {
        private const long MaxPhotoBytes = 6 * 1024 * 1024;

        private static readonly JsonSerializerOptions SnakeJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private readonly IRepositoryWrapper _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationCredentialProtector _protector;
        private readonly ILoggerManager _logger;

        public SilaInventoryVisionService(
            IRepositoryWrapper repository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IIntegrationCredentialProtector protector,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _protector = protector;
            _logger = logger;
        }

        public async Task<SilaInventoryBottleVisionDto> AnalyzeBottleAsync(
            Guid organizationId, Guid userId, IFormFile file, double? nominalMl, string? description, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestCustomException("No bottle photo uploaded.", "Take or choose a JPG, PNG or WEBP photo of one bottle.");
            }

            if (file.Length > MaxPhotoBytes)
            {
                throw new BadRequestCustomException("Photo too large.", "The bottle photo can be 6 MB at most.");
            }

            if (nominalMl is < 0 or > 5000)
            {
                throw new BadRequestCustomException("Invalid bottle size.", "The pack size must be between 0 and 5000 ml.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryAiConfiguration cfg = await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IsActive)
                ?? SilaCulinaryAiSettings.CreateDefault(buyer.Id);

            byte[] bytes;
            await using (Stream stream = file.OpenReadStream())
            using (MemoryStream buffer = new())
            {
                await stream.CopyToAsync(buffer, cancellationToken);
                bytes = buffer.ToArray();
            }

            object request = new
            {
                image_base64 = Convert.ToBase64String(bytes),
                mime_type = string.IsNullOrWhiteSpace(file.ContentType) ? "image/jpeg" : file.ContentType,
                nominal_ml = nominalMl is > 0 ? nominalMl : null,
                description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                provider_config = BuildProviderConfig(cfg)
            };

            string baseUrl = (_configuration[Common.CULINARY_ENGINE_URL] ?? "http://127.0.0.1:8013").TrimEnd('/');
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
            using StringContent content = new(JsonSerializer.Serialize(request, SnakeJson), Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));

            HttpResponseMessage response;
            try
            {
                response = await client.PostAsync($"{baseUrl}/v1/inventory/bottle-level", content, timeout.Token);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError($"Vision AI engine unreachable. UserId: {userId}, Error: {ex.Message}");
                throw new BadRequestCustomException("Vision AI is unavailable.", "The Vision AI engine did not answer. Enter the level by hand and try again later.");
            }

            using (response)
            {
                string raw = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Vision AI engine rejected the photo. Status: {(int)response.StatusCode}, UserId: {userId}");
                    throw new BadRequestCustomException("Bottle photo could not be read.", ExtractDetail(raw));
                }

                SilaInventoryBottleVisionDto result = JsonSerializer.Deserialize<SilaInventoryBottleVisionDto>(raw, SnakeJson)
                    ?? throw new BadRequestCustomException("Bottle photo could not be read.", "The Vision AI engine returned an empty answer.");
                _logger.LogInfo($"Bottle level read. UserId: {userId}, Method: {result.Method}, Fill: {result.FillRatio}, Confidence: {result.Confidence}");
                return result;
            }
        }

        private object? BuildProviderConfig(SilaCulinaryAiConfiguration cfg)
        {
            string mode = (cfg.OperatingMode ?? SilaCulinaryAiSettings.MODE_LOCAL).ToUpperInvariant();
            string fallback = (cfg.FallbackPolicy ?? "NONE").ToUpperInvariant() switch
            {
                "LOCAL_ONLY" or "LOCAL_IF_ALLOWED" => "LOCAL_ONLY",
                "CONFIGURED_ORDER" or "CONFIGURED_SECONDARY" => "CONFIGURED_ORDER",
                _ => "NONE"
            };

            bool local = cfg.LocalEnabled && !string.IsNullOrWhiteSpace(cfg.LocalEndpoint);
            bool external = cfg.AllowExternalAi;
            bool geminiReady = external && (mode == SilaCulinaryAiSettings.MODE_GEMINI || mode == SilaCulinaryAiSettings.MODE_HYBRID)
                && cfg.GeminiEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedGeminiApiKey);
            bool claudeReady = external && (mode == SilaCulinaryAiSettings.MODE_CLAUDE || mode == SilaCulinaryAiSettings.MODE_HYBRID)
                && cfg.ClaudeEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedClaudeApiKey);

            if (mode == SilaCulinaryAiSettings.MODE_LOCAL ? !local : !(geminiReady || claudeReady || local))
            {
                return null;
            }

            return new
            {
                tenant_id = cfg.BuyerId.ToString(),
                operating_mode = mode,
                tenant_external_ai_allowed = geminiReady || claudeReady,
                local_enabled = local,
                local_endpoint = cfg.LocalEndpoint,
                local_model = string.IsNullOrWhiteSpace(cfg.LocalModel) ? "qwen2.5:7b-instruct" : cfg.LocalModel,
                gemini_enabled = geminiReady,
                gemini_model = string.IsNullOrWhiteSpace(cfg.GeminiModel) ? "gemini-2.5-flash" : cfg.GeminiModel,
                gemini_api_key = geminiReady ? _protector.Unprotect(cfg.ProtectedGeminiApiKey) : null,
                claude_enabled = claudeReady,
                claude_model = string.IsNullOrWhiteSpace(cfg.ClaudeModel) ? "claude-sonnet-4-20250514" : cfg.ClaudeModel,
                claude_api_key = claudeReady ? _protector.Unprotect(cfg.ProtectedClaudeApiKey) : null,
                timeout_seconds = Math.Clamp(cfg.RequestTimeoutSeconds, 1, 120),
                max_output_tokens = Math.Clamp(cfg.MaxOutputTokens, 256, 4096),
                temperature = 0.0,
                max_retries = 0,
                fallback_policy = fallback
            };
        }

        private static string ExtractDetail(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "The Vision AI engine could not read this photo.";
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(raw);
                if (doc.RootElement.TryGetProperty("detail", out JsonElement detail))
                {
                    string text = detail.ToString();
                    return text.Length > 240 ? text[..240] : text;
                }
            }
            catch (JsonException)
            {
            }

            return "The Vision AI engine could not read this photo.";
        }
    }
}
