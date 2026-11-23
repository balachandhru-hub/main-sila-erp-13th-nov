using Buyer.Domain.Dtos;
using Buyer.Application.Features.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Integration.Dtos;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;
using SharedKernel.Integration;
using SharedKernel.LoggerServices;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Features.Queries.ResolveIntegration
{
    public class ResolveIntegrationQueryHandler : IRequestHandler<ResolveIntegrationQuery, IntegrationApiDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ResolveIntegrationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IntegrationApiDto> Handle(ResolveIntegrationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Resolving integration. OrganizationId: {request.OrganizationId}, ProcessType: {request.ProcessType}, EntityCode: {request.EntityCode}");
            string processType = request.ProcessType.ToString();
            List<ApiIntegrationConfiguration> active = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => IntegrationLookup.BuyerIdsOf(_repository, request.OrganizationId).Contains(x.BuyerId)
                    && x.ProcessType == processType
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.IsActive)
                .ToListAsync(cancellationToken);

            // The API of the named entity (company code) when it has one of its own, otherwise the organization-wide one (ALL).
            // A document of one company code is never sent to the API of another company code.
            // For POST_PO prefer FIVE Create PO when several ALL configs exist.
            string? entityCode = string.IsNullOrWhiteSpace(request.EntityCode) ? null : request.EntityCode.Trim();
            ApiIntegrationConfiguration? PreferCreatePo(IEnumerable<ApiIntegrationConfiguration> rows) =>
                rows.FirstOrDefault(x =>
                    request.ProcessType == IntegrationProcessType.POST_PO
                    && x.Name != null
                    && x.Name.Contains("FIVE", StringComparison.OrdinalIgnoreCase)
                    && x.Name.Contains("Create PO", StringComparison.OrdinalIgnoreCase))
                ?? rows.FirstOrDefault();

            ApiIntegrationConfiguration? configuration =
                PreferCreatePo(active.Where(x => entityCode != null && x.EntityCode.Equals(entityCode, StringComparison.OrdinalIgnoreCase)))
                ?? PreferCreatePo(active.Where(x => x.EntityCode.Equals(IntegrationConstants.ENTITY_CODE_ALL, StringComparison.OrdinalIgnoreCase)))
                ?? (entityCode == null ? PreferCreatePo(active.OrderBy(x => x.DateCreated)) : null);
            if (configuration == null)
            {
                _logger.LogInfo($"No active integration. OrganizationId: {request.OrganizationId}, ProcessType: {request.ProcessType}");
                return new IntegrationApiDto { Configured = false };
            }

            _logger.LogInfo($"Integration resolved. ConfigurationId: {configuration.Id}, BuyerId: {configuration.BuyerId}");
            return new IntegrationApiDto
            {
                Configured = true,
                ProcessType = IntegrationProcessTypes.ReadOrNull(configuration.ProcessType),
                ConfigurationId = configuration.Id,
                Name = configuration.Name,
                SystemName = configuration.SystemName,
                EntityCode = configuration.EntityCode,
                BaseUrl = configuration.BaseUrl,
                ResourcePath = configuration.ResourcePath,
                HttpMethod = configuration.HttpMethod,
                PayloadFormat = configuration.PayloadFormat,
                RequestBody = configuration.RequestBody
            };
        }
    }
}
