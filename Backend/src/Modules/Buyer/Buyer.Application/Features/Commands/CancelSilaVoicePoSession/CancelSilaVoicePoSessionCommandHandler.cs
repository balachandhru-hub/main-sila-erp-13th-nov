using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CancelSilaVoicePoSession
{
    public class CancelSilaVoicePoSessionCommandHandler : IRequestHandler<CancelSilaVoicePoSessionCommand, SilaVoicePoSessionDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CancelSilaVoicePoSessionCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaVoicePoSessionDto> Handle(CancelSilaVoicePoSessionCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaVoicePoSession? session = await _repository.SilaVoicePoSession
                .FindFirstByConditionAsync(x => x.Id == request.SessionId && x.BuyerId == buyer.Id && x.IsActive);
            if (session == null || session.UserId != request.UserId)
            {
                throw new NotFoundCustomException("Voice PO session not found.", "Start a new Voice PO session.");
            }

            if (session.Status == SilaVoicePoAgentRules.STATUS_CREATED)
            {
                throw new ConflictCustomException(
                    "Voice PO already created.",
                    $"Session {session.SessionNumber} already created purchase order {session.CreatedPoNumber}.");
            }

            if (session.Status != SilaVoicePoAgentRules.STATUS_CANCELLED)
            {
                session.Status = SilaVoicePoAgentRules.STATUS_CANCELLED;
                session.SpokenReply = "Voice purchase order cancelled.";
                InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
                ledger.AddEvent(Common.SILA_REF_VOICE_PO, session.Id, Common.AUDIT_CANCELLED, session.SessionNumber);
                await _repository.SaveAsync();
            }

            List<SilaVoicePoTurn> turns = await _repository.SilaVoicePoTurn
                .FindByCondition(x => x.SessionId == session.Id && x.IsActive)
                .OrderBy(x => x.TurnNumber)
                .ToListAsync(cancellationToken);
            return SilaVoicePoSessionMapper.ToDto(session, turns);
        }
    }
}
