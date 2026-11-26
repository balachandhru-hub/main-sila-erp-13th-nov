using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.TestSilaCulinaryAiConnection
{
    public class TestSilaCulinaryAiConnectionCommandHandler : IRequestHandler<TestSilaCulinaryAiConnectionCommand, SilaCulinaryAiConnectionTestResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISilaCulinaryAiProviderGateway _gateway;

        public TestSilaCulinaryAiConnectionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISilaCulinaryAiProviderGateway gateway)
        {
            _repository = repository;
            _logger = logger;
            _gateway = gateway;
        }

        public async Task<SilaCulinaryAiConnectionTestResultDto> Handle(
            TestSilaCulinaryAiConnectionCommand command,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, command.OrganizationId);
            SilaCulinaryAiConfiguration? entity = await _repository.SilaCulinaryAiConfiguration.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.IsActive);

            SilaCulinaryAiConfiguration effective = entity ?? SilaCulinaryAiSettings.CreateDefault(buyer.Id);
            SilaCulinaryAiConnectionTestResultDto result = await _gateway.TestConnectionAsync(
                effective,
                command.Request,
                cancellationToken);

            if (entity != null)
            {
                entity.LastTestProvider = result.Provider;
                entity.LastTestStatus = result.StatusCode;
                entity.LastTestMessage = result.Message.Length > 990 ? result.Message[..990] : result.Message;
                entity.LastTestedOn = result.TestedAtUtc;

                _repository.InventoryWorkflowEvent.Create(new InventoryWorkflowEvent
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ReferenceType = Common.SILA_REF_CULINARY_AI_SETUP,
                    ReferenceId = entity.Id,
                    Action = Common.SILA_AUDIT_CULINARY_AI_TESTED,
                    Comment = $"Provider={result.Provider}; Status={result.StatusCode}; LatencyMs={result.LatencyMs}",
                    ActorUserId = command.UserId,
                    IsActive = true
                });

                await _repository.SaveAsync();
            }

            return result;
        }
    }
}
