using System.Text.Json;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Dtos;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;
using SharedKernel.Integration.Rules;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;
using Buyer.Application.Features.Shared;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Features.Commands.AlignIntegrationConnection
{
    public class AlignIntegrationConnectionCommandHandler : IRequestHandler<AlignIntegrationConnectionCommand, IntegrationConfigurationResponseDto>
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private const string MaterialDocumentService = "API_MATERIAL_DOCUMENT_SRV";
        private const string MaterialDocumentEntity = "A_MaterialDocumentHeader";

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _credentials;

        public AlignIntegrationConnectionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials)
        {
            _repository = repository;
            _logger = logger;
            _credentials = credentials;
        }

        public async Task<IntegrationConfigurationResponseDto> Handle(AlignIntegrationConnectionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Aligning integration connection. TargetId: {request.ConfigurationId}, SourceId: {request.SourceConfigurationId}, OrganizationId: {request.OrganizationId}");
            if (request.ConfigurationId == request.SourceConfigurationId)
            {
                throw new BadRequestCustomException("Source and target are the same.", "Choose a different working connection to copy from.");
            }

            ApiIntegrationConfiguration target = await IntegrationLookup.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            ApiIntegrationConfiguration source = await IntegrationLookup.GetTrackedAsync(_repository, _logger, request.SourceConfigurationId, request.OrganizationId);

            target.BaseUrl = source.BaseUrl;
            target.Protocol = source.Protocol;
            target.AuthenticationType = source.AuthenticationType;
            target.Username = source.Username;
            target.TimeoutSeconds = source.TimeoutSeconds > 0 ? source.TimeoutSeconds : target.TimeoutSeconds;
            target.RetryCount = source.RetryCount;
            target.ProtectedPassword = source.ProtectedPassword;
            target.ProtectedClientId = source.ProtectedClientId;
            target.ProtectedClientSecret = source.ProtectedClientSecret;
            target.ProtectedBearerToken = source.ProtectedBearerToken;
            target.ProtectedApiKey = source.ProtectedApiKey;
            target.ApiKeyHeader = source.ApiKeyHeader;
            target.TokenEndpoint = source.TokenEndpoint;
            target.TokenScope = source.TokenScope;
            target.TokenHeadersJson = source.TokenHeadersJson;
            target.TokenBodyJson = source.TokenBodyJson;
            if (string.IsNullOrWhiteSpace(target.SystemName))
            {
                target.SystemName = source.SystemName;
            }

            target.ResourcePath = NormalizeResourcePath(target.ResourcePath, target.HttpMethod);
            target.HeadersJson = MergeHeaders(source.HeadersJson, target.HeadersJson, target.HttpMethod);
            target.LastErrorSafe = null;
            if (target.Status != IntegrationConfigurationStatus.ACTIVE)
            {
                target.Status = IntegrationConfigurationStatus.DRAFT;
            }

            await _repository.SaveAsync();
            _logger.LogInfo($"Integration connection aligned. TargetId: {target.Id}, SourceId: {source.Id}");
            return IntegrationResponseBuilder.Integration(target, _credentials);
        }

        /// <summary>
        /// Material Document posts need the entity set. A service-root-only path was returning HTTP 401 on test.
        /// </summary>
        public static string? NormalizeResourcePath(string? resourcePath, string? httpMethod)
        {
            string path = (resourcePath ?? string.Empty).Trim().Trim('/');
            if (path.Length == 0)
            {
                return resourcePath;
            }

            bool materialDocument = path.Contains(MaterialDocumentService, StringComparison.OrdinalIgnoreCase);
            if (!materialDocument)
            {
                return path;
            }

            if (path.EndsWith(MaterialDocumentService, StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(MaterialDocumentService + "/", StringComparison.OrdinalIgnoreCase))
            {
                return path.TrimEnd('/') + "/" + MaterialDocumentEntity;
            }

            return path;
        }

        public static string? MergeHeaders(string? sourceHeadersJson, string? targetHeadersJson, string? httpMethod)
        {
            Dictionary<string, string> merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string>? source = IntegrationConfigurationRules.ReadHeaders(sourceHeadersJson);
            if (source != null)
            {
                foreach (KeyValuePair<string, string> header in source)
                {
                    merged[header.Key] = header.Value;
                }
            }

            Dictionary<string, string>? target = IntegrationConfigurationRules.ReadHeaders(targetHeadersJson);
            bool push = string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase)
                || string.Equals(httpMethod, "PUT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(httpMethod, "PATCH", StringComparison.OrdinalIgnoreCase);

            if (push)
            {
                if (!merged.ContainsKey("Content-Type"))
                {
                    merged["Content-Type"] = "application/json";
                }

                KeyValuePair<string, string>? csrf = merged
                    .Where(header => IntegrationConfigurationRules.IsCsrfSwitch(header.Value))
                    .Select(header => (KeyValuePair<string, string>?)header)
                    .FirstOrDefault()
                    ?? target?
                        .Where(header => IntegrationConfigurationRules.IsCsrfSwitch(header.Value))
                        .Select(header => (KeyValuePair<string, string>?)header)
                        .FirstOrDefault();
                merged[csrf?.Key ?? "x-csrf-token"] = "Fetch";
            }

            return merged.Count == 0 ? null : JsonSerializer.Serialize(merged, JsonOptions);
        }
    }
}
