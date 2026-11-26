using System.Diagnostics;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using SharedKernel.Integration.Services;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryAiProviderGateway
    {
        Task<SilaCulinaryAiConnectionTestResultDto> TestConnectionAsync(
            SilaCulinaryAiConfiguration config,
            SilaCulinaryAiConnectionTestRequestDto request,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Phase 0 provider abstraction: validates tenant privacy policies, decrypts credentials server-side only,
    /// performs authorized connection/model-availability checks, and never sends tenant recipes or fabricates results.
    /// </summary>
    public class SilaCulinaryAiProviderGateway : ISilaCulinaryAiProviderGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIntegrationCredentialProtector _protector;

        public SilaCulinaryAiProviderGateway(
            IHttpClientFactory httpClientFactory,
            IIntegrationCredentialProtector protector)
        {
            _httpClientFactory = httpClientFactory;
            _protector = protector;
        }

        public async Task<SilaCulinaryAiConnectionTestResultDto> TestConnectionAsync(
            SilaCulinaryAiConfiguration config,
            SilaCulinaryAiConnectionTestRequestDto request,
            CancellationToken cancellationToken)
        {
            string provider = (request.Provider ?? SilaCulinaryAiSettings.MODE_LOCAL).Trim().ToUpperInvariant();
            DateTime now = DateTime.UtcNow;

            if (provider == SilaCulinaryAiSettings.MODE_LOCAL)
            {
                if (!config.LocalEnabled)
                {
                    return Failed(provider, "PROVIDER_DISABLED", "Local AI is currently disabled in Culinary AI Engine Setup.", now);
                }

                string? endpoint = !string.IsNullOrWhiteSpace(request.LocalEndpointOverride)
                    ? request.LocalEndpointOverride.Trim()
                    : config.LocalEndpoint?.Trim();
                if (string.IsNullOrWhiteSpace(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? baseUri)
                    || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
                {
                    return Failed(provider, "INVALID_ENDPOINT", "Provide a valid http/https URL for the Local AI endpoint.", now);
                }

                return await TestLocalAsync(baseUri, config.LocalModel, config.RequestTimeoutSeconds, now, cancellationToken);
            }

            if (provider == SilaCulinaryAiSettings.MODE_GEMINI)
            {
                if (!config.AllowExternalAi)
                {
                    return Failed(provider, "EXTERNAL_AI_DISABLED", "External AI providers are blocked by tenant privacy policy. Enable 'Allow external AI providers' to test Gemini.", now);
                }
                if (!config.GeminiEnabled)
                {
                    return Failed(provider, "PROVIDER_DISABLED", "Gemini provider is disabled.", now);
                }
                string? apiKey = UnprotectOrNull(config.ProtectedGeminiApiKey);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Failed(provider, "MISSING_CREDENTIALS", "No Gemini API credential is saved on the server.", now);
                }
                return await TestGeminiAsync(apiKey, config.RequestTimeoutSeconds, now, cancellationToken);
            }

            if (provider == SilaCulinaryAiSettings.MODE_CLAUDE)
            {
                if (!config.AllowExternalAi)
                {
                    return Failed(provider, "EXTERNAL_AI_DISABLED", "External AI providers are blocked by tenant privacy policy. Enable 'Allow external AI providers' to test Claude.", now);
                }
                if (!config.ClaudeEnabled)
                {
                    return Failed(provider, "PROVIDER_DISABLED", "Claude provider is disabled.", now);
                }
                string? apiKey = UnprotectOrNull(config.ProtectedClaudeApiKey);
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Failed(provider, "MISSING_CREDENTIALS", "No Claude API credential is saved on the server.", now);
                }
                return await TestClaudeAsync(apiKey, config.RequestTimeoutSeconds, now, cancellationToken);
            }

            return Failed(provider, "INVALID_PROVIDER", "Unsupported provider. Choose LOCAL, GEMINI, or CLAUDE.", now);
        }

        private string? UnprotectOrNull(string? encrypted)
        {
            if (string.IsNullOrWhiteSpace(encrypted)) return null;
            try
            {
                return _protector.Unprotect(encrypted);
            }
            catch
            {
                return null;
            }
        }

        private async Task<SilaCulinaryAiConnectionTestResultDto> TestLocalAsync(
            Uri baseUri, string? expectedModel, int timeoutSeconds, DateTime now, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_AI);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 30)));
            Stopwatch sw = Stopwatch.StartNew();

            string baseStr = baseUri.ToString().TrimEnd('/');
            // Probe Ollama /api/tags first, then generic /health
            foreach (string probe in new[] { $"{baseStr}/api/tags", $"{baseStr}/health" })
            {
                try
                {
                    using HttpResponseMessage response = await client.GetAsync(probe, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        sw.Stop();
                        string body = await response.Content.ReadAsStringAsync(cts.Token);
                        List<string> models = ExtractModelNames(body);
                        bool modelMatched = string.IsNullOrWhiteSpace(expectedModel)
                            || models.Count == 0
                            || models.Any(m => m.Contains(expectedModel.Trim(), StringComparison.OrdinalIgnoreCase));
                        string msg = modelMatched
                            ? $"Local endpoint reachable at {baseStr}."
                            : $"Local endpoint reachable at {baseStr}, but configured model '{expectedModel}' was not listed.";
                        return new SilaCulinaryAiConnectionTestResultDto
                        {
                            Provider = SilaCulinaryAiSettings.MODE_LOCAL,
                            Success = true,
                            StatusCode = modelMatched ? "CONNECTED" : "MODEL_NOT_FOUND",
                            Message = msg,
                            LatencyMs = (int)sw.ElapsedMilliseconds,
                            TestedAtUtc = now,
                            AvailableModels = models
                        };
                    }
                }
                catch (Exception) when (!cts.IsCancellationRequested)
                {
                    // Try next probe path.
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    return Failed(SilaCulinaryAiSettings.MODE_LOCAL, "CONNECTION_FAILED",
                        $"Local endpoint {baseStr} could not be reached: {ex.Message}", now, (int)sw.ElapsedMilliseconds);
                }
            }

            sw.Stop();
            return Failed(SilaCulinaryAiSettings.MODE_LOCAL, "CONNECTION_FAILED",
                $"Local endpoint {baseStr} did not return success on /api/tags or /health.", now, (int)sw.ElapsedMilliseconds);
        }

        private async Task<SilaCulinaryAiConnectionTestResultDto> TestGeminiAsync(
            string apiKey, int timeoutSeconds, DateTime now, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_AI);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 30)));
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                using HttpRequestMessage req = new(HttpMethod.Get, "https://generativelanguage.googleapis.com/v1beta/models");
                req.Headers.Add("x-goog-api-key", apiKey);
                using HttpResponseMessage response = await client.SendAsync(req, cts.Token);
                sw.Stop();
                if (!response.IsSuccessStatusCode)
                {
                    return Failed(SilaCulinaryAiSettings.MODE_GEMINI, "CONNECTION_FAILED",
                        $"Gemini returned HTTP {(int)response.StatusCode}. Verify credential and network egress policy.", now, (int)sw.ElapsedMilliseconds);
                }
                string body = await response.Content.ReadAsStringAsync(cts.Token);
                return new SilaCulinaryAiConnectionTestResultDto
                {
                    Provider = SilaCulinaryAiSettings.MODE_GEMINI,
                    Success = true,
                    StatusCode = "CONNECTED",
                    Message = "Gemini API key validated via model list endpoint.",
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                    TestedAtUtc = now,
                    AvailableModels = ExtractModelNames(body)
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Failed(SilaCulinaryAiSettings.MODE_GEMINI, "CONNECTION_FAILED",
                    $"Gemini connection check failed: {ex.Message}", now, (int)sw.ElapsedMilliseconds);
            }
        }

        private async Task<SilaCulinaryAiConnectionTestResultDto> TestClaudeAsync(
            string apiKey, int timeoutSeconds, DateTime now, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_AI);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 30)));
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                using HttpRequestMessage req = new(HttpMethod.Get, "https://api.anthropic.com/v1/models");
                req.Headers.Add("x-api-key", apiKey);
                req.Headers.Add("anthropic-version", "2023-06-01");
                using HttpResponseMessage response = await client.SendAsync(req, cts.Token);
                sw.Stop();
                if (!response.IsSuccessStatusCode)
                {
                    return Failed(SilaCulinaryAiSettings.MODE_CLAUDE, "CONNECTION_FAILED",
                        $"Claude returned HTTP {(int)response.StatusCode}. Verify credential and network egress policy.", now, (int)sw.ElapsedMilliseconds);
                }
                string body = await response.Content.ReadAsStringAsync(cts.Token);
                return new SilaCulinaryAiConnectionTestResultDto
                {
                    Provider = SilaCulinaryAiSettings.MODE_CLAUDE,
                    Success = true,
                    StatusCode = "CONNECTED",
                    Message = "Anthropic Claude API key validated via model list endpoint.",
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                    TestedAtUtc = now,
                    AvailableModels = ExtractModelNames(body)
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return Failed(SilaCulinaryAiSettings.MODE_CLAUDE, "CONNECTION_FAILED",
                    $"Claude connection check failed: {ex.Message}", now, (int)sw.ElapsedMilliseconds);
            }
        }

        private static List<string> ExtractModelNames(string json)
        {
            List<string> names = new();
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;
                foreach (string prop in new[] { "models", "data" })
                {
                    if (root.TryGetProperty(prop, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in arr.EnumerateArray().Take(12))
                        {
                            if (item.TryGetProperty("name", out JsonElement n) && n.GetString() is string ns)
                                names.Add(ns);
                            else if (item.TryGetProperty("id", out JsonElement id) && id.GetString() is string ids)
                                names.Add(ids);
                        }
                    }
                }
            }
            catch
            {
                // Ignore non-JSON bodies (e.g. plain health checks).
            }
            return names;
        }

        private static SilaCulinaryAiConnectionTestResultDto Failed(
            string provider, string code, string message, DateTime now, int latencyMs = 0) => new()
        {
            Provider = provider,
            Success = false,
            StatusCode = code,
            Message = message,
            LatencyMs = latencyMs,
            TestedAtUtc = now
        };
    }
}
