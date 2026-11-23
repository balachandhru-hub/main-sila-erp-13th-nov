using System.Text;
using Buyer.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Thin authenticated proxy to the isolated Python SILA Voice Engine.
    /// Does not replace Voice PO; forwards the caller's token so business tools reuse existing Buyer APIs.
    /// </summary>
    [ApiController]
    public class SilaVoiceEngineController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SilaVoiceEngineController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/voice-engine/health")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("SilaVoiceEngineHealth")]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Get, "/health", null, cancellationToken);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/voice-engine/agents")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("SilaVoiceEngineAgents")]
        public async Task<IActionResult> Agents(CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Get, "/api/v1/sila-voice/agents", null, cancellationToken);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-engine/sessions")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("StartSilaVoiceEngineSession")]
        public async Task<IActionResult> Start([FromBody] object? body, CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Post, "/api/v1/sila-voice/sessions", body, cancellationToken);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-engine/sessions/{sessionId}/turns")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("SilaVoiceEngineTurn")]
        public async Task<IActionResult> Turn([FromRoute] Guid sessionId, [FromBody] object? body, CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Post, $"/api/v1/sila-voice/sessions/{sessionId}/turns", body, cancellationToken);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-engine/sessions/{sessionId}/cancel")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("CancelSilaVoiceEngineSession")]
        public async Task<IActionResult> Cancel([FromRoute] Guid sessionId, CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Post, $"/api/v1/sila-voice/sessions/{sessionId}/cancel", null, cancellationToken);
        }

        /// <summary>Proxies audible WAV TTS for Voice Agent talk-back when browser speechSynthesis has no voices.</summary>
        [HttpPost]
        [Route("api/v1/buyer/sila/voice-engine/tts")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("SilaVoiceEngineTts")]
        public async Task<IActionResult> Tts([FromBody] object? body, CancellationToken cancellationToken)
        {
            return await ProxyAsync(HttpMethod.Post, "/api/v1/sila-voice/tts", body, cancellationToken, binary: true);
        }

        private async Task<IActionResult> ProxyAsync(
            HttpMethod method,
            string path,
            object? body,
            CancellationToken cancellationToken,
            bool binary = false)
        {
            string? baseUrl = _configuration[Common.VOICE_ENGINE_URL];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Voice engine URL is not configured. Key: InterCallService:VoiceEngineUrl");
                return StatusCode(503, new
                {
                    message = "Voice engine is not configured.",
                    description = "Set InterCallService:VoiceEngineUrl to the SILA Voice Engine (default http://127.0.0.1:8012)."
                });
            }

            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_VOICE_ENGINE);
            using HttpRequestMessage request = new(method, Combine(baseUrl.TrimEnd('/'), path));
            string? auth = Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth))
            {
                request.Headers.TryAddWithoutValidation("Authorization", auth);
            }

            if (Request.Headers.TryGetValue("Cookie", out var cookies))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookies.ToString());
            }

            if (body != null && method != HttpMethod.Get)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            try
            {
                using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
                string? contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
                if (binary || (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)))
                {
                    byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, Encoding.UTF8.GetString(bytes));
                    }

                    return File(bytes, contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ? contentType : "audio/wav");
                }

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = content,
                    ContentType = contentType
                };
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError($"Voice engine proxy failed. Path: {path}, Error: {exception.Message}");
                return StatusCode(503, new
                {
                    message = "Voice engine is unavailable.",
                    description = "Start Backend/sila-voice-engine on port 8012, or check InterCallService:VoiceEngineUrl."
                });
            }
        }

        private static string Combine(string baseUrl, string path)
        {
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            return path.StartsWith('/') ? baseUrl + path : baseUrl + "/" + path;
        }
    }
}
