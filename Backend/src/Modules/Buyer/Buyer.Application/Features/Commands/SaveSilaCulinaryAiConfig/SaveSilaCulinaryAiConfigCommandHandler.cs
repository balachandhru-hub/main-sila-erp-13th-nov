using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveSilaCulinaryAiConfig
{
    public class SaveSilaCulinaryAiConfigCommandHandler : IRequestHandler<SaveSilaCulinaryAiConfigCommand, SilaCulinaryAiConfigDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationCredentialProtector _protector;

        public SaveSilaCulinaryAiConfigCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector protector)
        {
            _repository = repository;
            _logger = logger;
            _protector = protector;
        }

        public async Task<SilaCulinaryAiConfigDto> Handle(SaveSilaCulinaryAiConfigCommand command, CancellationToken cancellationToken)
        {
            SilaCulinaryAiConfigWriteDto input = command.Request;
            SilaCulinaryAiSettings.ValidateWrite(input);

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, command.OrganizationId);
            SilaCulinaryAiConfiguration? entity = await _repository.SilaCulinaryAiConfiguration.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.IsActive);

            bool isNew = entity == null;
            entity ??= new SilaCulinaryAiConfiguration
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
            };

            entity.OperatingMode = SilaCulinaryAiSettings.NormalizeMode(input.OperatingMode);
            entity.AllowExternalAi = input.AllowExternalAi;
            entity.LocalEnabled = input.LocalEnabled;
            entity.LocalEndpoint = string.IsNullOrWhiteSpace(input.LocalEndpoint) ? null : input.LocalEndpoint.Trim();
            entity.LocalModel = string.IsNullOrWhiteSpace(input.LocalModel) ? null : input.LocalModel.Trim();
            entity.GeminiEnabled = input.GeminiEnabled;
            entity.GeminiModel = string.IsNullOrWhiteSpace(input.GeminiModel) ? null : input.GeminiModel.Trim();
            entity.ClaudeEnabled = input.ClaudeEnabled;
            entity.ClaudeModel = string.IsNullOrWhiteSpace(input.ClaudeModel) ? null : input.ClaudeModel.Trim();

            if (input.ClearGeminiKey)
            {
                entity.ProtectedGeminiApiKey = null;
            }
            else if (!SilaCulinaryAiSettings.IsMaskedPlaceholder(input.GeminiApiKey))
            {
                entity.ProtectedGeminiApiKey = _protector.Protect(input.GeminiApiKey!.Trim());
            }

            if (input.ClearClaudeKey)
            {
                entity.ProtectedClaudeApiKey = null;
            }
            else if (!SilaCulinaryAiSettings.IsMaskedPlaceholder(input.ClaudeApiKey))
            {
                entity.ProtectedClaudeApiKey = _protector.Protect(input.ClaudeApiKey!.Trim());
            }

            entity.RequestTimeoutSeconds = input.RequestTimeoutSeconds;
            entity.MaxOutputTokens = input.MaxOutputTokens;
            entity.Temperature = Math.Round(input.Temperature, 2);
            entity.DailyRequestLimit = input.DailyRequestLimit;
            entity.MonthlyRequestLimit = input.MonthlyRequestLimit;
            entity.DailyTokenBudget = input.DailyTokenBudget;
            entity.MonthlyCostBudgetUsd = Math.Round(input.MonthlyCostBudgetUsd, 2);
            entity.RetryCount = input.RetryCount;
            entity.FallbackPolicy = SilaCulinaryAiSettings.NormalizeFallback(input.FallbackPolicy);
            entity.TaskRoutingJson = SilaCulinaryAiSettings.SerializeTaskRouting(input.TaskRouting);
            entity.FeatureFlagsJson = SilaCulinaryAiSettings.SerializeFeatureFlags(input.FeatureFlags);
            entity.IsActive = true;

            if (isNew)
            {
                _repository.SilaCulinaryAiConfiguration.Create(entity);
            }

            _repository.InventoryWorkflowEvent.Create(new InventoryWorkflowEvent
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ReferenceType = Common.SILA_REF_CULINARY_AI_SETUP,
                ReferenceId = entity.Id,
                Action = Common.SILA_AUDIT_CULINARY_AI_SAVED,
                Comment = $"Mode={entity.OperatingMode}; AllowExternalAi={entity.AllowExternalAi}; Local={entity.LocalEnabled}; Gemini={entity.GeminiEnabled}; Claude={entity.ClaudeEnabled}",
                ActorUserId = command.UserId,
                IsActive = true
            });

            await _repository.SaveAsync();
            _logger.LogInfo($"Saved Culinary AI Engine configuration. BuyerId: {buyer.Id}, Mode: {entity.OperatingMode}");
            return SilaCulinaryAiSettings.ToDto(entity, buyer.Id, SilaAccess.IsAdministrator(command.RoleId), _protector);
        }
    }
}
