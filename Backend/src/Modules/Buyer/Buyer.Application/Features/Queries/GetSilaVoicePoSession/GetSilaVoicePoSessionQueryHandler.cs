using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaVoicePoSession
{
    public class GetSilaVoicePoSessionQueryHandler : IRequestHandler<GetSilaVoicePoSessionQuery, SilaVoicePoSessionDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaVoicePoSessionQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaVoicePoSessionDto> Handle(GetSilaVoicePoSessionQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaVoicePoSession? session = await _repository.SilaVoicePoSession
                .FindFirstByConditionAsync(x => x.Id == request.SessionId && x.BuyerId == buyer.Id && x.IsActive);
            if (session == null || session.UserId != request.UserId)
            {
                throw new NotFoundCustomException("Voice PO session not found.", "Start a new Voice PO session.");
            }

            List<SilaVoicePoTurn> turns = await _repository.SilaVoicePoTurn
                .FindByCondition(x => x.SessionId == session.Id && x.IsActive)
                .OrderBy(x => x.TurnNumber)
                .ToListAsync(cancellationToken);
            return SilaVoicePoSessionMapper.ToDto(session, turns);
        }
    }
}
