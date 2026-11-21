using Buyer.Application.Features.Commands.PostPendingErpPostings;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ProcessSilaPosBatch
{
    /// <summary>
    /// POS sales consumption confirm posting:
    /// Step 1 — validate and update SILA inventory locally;
    /// Step 2 — call the SAP Material Document API and post;
    /// Step 3 — sale/posting status is updated so the Transaction Tracker shows results.
    /// </summary>
    public class ProcessSilaPosBatchCommandHandler : IRequestHandler<ProcessSilaPosBatchCommand, SilaPosImportResultDto>
    {
        /// <summary>Shared with pull / reprocess. Confirm posting always posts SAP unless QueueErpPosting is explicitly false.</summary>
        public const string CONFIG_QUEUE_ERP = "SilaPos:QueueErpPosting";

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public ProcessSilaPosBatchCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public Task<SilaPosImportResultDto> Handle(ProcessSilaPosBatchCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunForJobAsync(_repository, _logger, nameof(ProcessSilaPosBatchCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<SilaPosImportResultDto> HandleOnceAsync(ProcessSilaPosBatchCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Processing POS batch. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}, BatchId: {request.BatchId}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            PosSalesBatch? batch = await _repository.PosSalesBatch.FindFirstByConditionAsync(
                x => x.Id == request.BatchId && x.BuyerId == buyer.Id && x.IsActive);
            if (batch == null)
            {
                _logger.LogError($"POS batch not found. BatchId: {request.BatchId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Sales batch not found.", "Select a sales batch of this organization.");
            }

            if (batch.Status != Common.SILA_POS_BATCH_PREVIEW)
            {
                _logger.LogError($"POS batch is not a preview. BatchId: {batch.Id}, Status: {batch.Status}");
                throw new BadRequestCustomException("This batch is already processed.", "Open the Transaction Tracker to follow its lines or reprocess failed ones.");
            }

            List<PosSalesTransaction> allReceived = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BatchId == batch.Id && x.BuyerId == buyer.Id && x.IsActive && x.Status == Common.SILA_POS_RECEIVED)
                .OrderBy(x => x.BusinessDate)
                .ThenBy(x => x.SourceTransactionId)
                .ThenBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);

            HashSet<Guid>? selected = request.TransactionIds == null || request.TransactionIds.Count == 0
                ? null
                : request.TransactionIds.ToHashSet();
            List<PosSalesTransaction> lines = selected == null
                ? allReceived
                : allReceived.Where(x => selected.Contains(x.Id)).ToList();
            if (selected != null && lines.Count == 0)
            {
                throw new BadRequestCustomException(
                    "No selected lines to process.",
                    "Select ready RECEIVED lines from this preview batch, or omit the selection to process all.");
            }

            _repository.PosSalesTransaction.UpdateRange(lines);
            int alreadyPosted = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BatchId == batch.Id && x.BuyerId == buyer.Id && x.IsActive
                    && (x.Status == Common.SILA_POS_INVENTORY_DEDUCTED || x.Status == Common.SILA_POS_POSTED))
                .CountAsync(cancellationToken);

            // Confirm posting always runs Step 2 (SAP) unless the caller explicitly opts out.
            bool queueErp = request.QueueErpPosting != false;
            int failed = await SilaPosProcessing.ProcessAsync(
                _repository, _logger, buyer.Id, request.UserId, lines, queueErp, cancellationToken);

            int remaining = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BatchId == batch.Id && x.BuyerId == buyer.Id && x.IsActive && x.Status == Common.SILA_POS_RECEIVED)
                .CountAsync(cancellationToken);
            if (remaining <= 0)
            {
                batch.Status = Common.SILA_POS_BATCH_PROCESSED;
            }

            batch.Accepted = lines.Count;
            _repository.PosSalesBatch.Update(batch);
            await _repository.SaveAsync();

            int localUpdated = lines.Count(x =>
                x.Status == Common.SILA_POS_INVENTORY_DEDUCTED
                || x.Status == Common.SILA_POS_POSTED
                || x.ErpPostingId != null);

            if (queueErp)
            {
                await PostQueuedSapAsync(buyer.Id, lines, cancellationToken);
            }

            (int sapPosted, int sapFailed, int postingUnknown) = await CountSapOutcomesAsync(buyer.Id, lines, cancellationToken);

            SilaPosImportResultDto result = new SilaPosImportResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Rows = batch.Rows,
                Accepted = lines.Count,
                Duplicates = batch.Duplicates,
                Invalid = batch.Invalid,
                Processed = lines.Count - failed,
                Failed = failed,
                AlreadyPosted = alreadyPosted,
                LocalUpdated = localUpdated,
                SapPosted = sapPosted,
                SapFailed = sapFailed,
                PostingUnknown = postingUnknown
            };

            _logger.LogInfo(
                $"POS batch processed. BatchNumber: {batch.BatchNumber}, LocalUpdated: {localUpdated}, SapPosted: {sapPosted}, SapFailed: {sapFailed}, Failed: {failed}");
            return result;
        }

        private async Task PostQueuedSapAsync(Guid buyerId, List<PosSalesTransaction> lines, CancellationToken cancellationToken)
        {
            // One FIVE_POS_UPDATE call per file/API batch — PostConsumptionGroupAsync posts all sibling sales together.
            Guid? leaderPostingId = lines.Where(x => x.ErpPostingId != null).Select(x => x.ErpPostingId!.Value).FirstOrDefault();
            if (leaderPostingId == null || leaderPostingId == Guid.Empty)
            {
                return;
            }

            try
            {
                await _mediator.Send(new PostPendingErpPostingsCommand { BuyerId = buyerId, PostingId = leaderPostingId }, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    $"Immediate POS ERP post failed after inventory deduction. BuyerId: {buyerId}, PostingId: {leaderPostingId}, Error: {SilaLogText.FromException(exception)}");
            }
        }

        private async Task<(int SapPosted, int SapFailed, int PostingUnknown)> CountSapOutcomesAsync(
            Guid buyerId,
            List<PosSalesTransaction> lines,
            CancellationToken cancellationToken)
        {
            List<Guid> postingIds = lines.Where(x => x.ErpPostingId != null).Select(x => x.ErpPostingId!.Value).Distinct().ToList();
            if (postingIds.Count == 0)
            {
                return (0, 0, 0);
            }

            List<InventoryErpPosting> postings = await _repository.InventoryErpPosting
                .FindByCondition(x => x.BuyerId == buyerId && postingIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            int sapPosted = postings.Count(x => x.Status == Common.SILA_POSTING_POSTED || SilaTrackerRules.ErpAlreadyAccepted(x));
            int sapFailed = postings.Count(x => x.Status == Common.SILA_POSTING_FAILED || x.Status == Common.SILA_POSTING_SKIPPED);
            int postingUnknown = postings.Count(x => x.Status == Common.SILA_POSTING_UNKNOWN);
            return (sapPosted, sapFailed, postingUnknown);
        }
    }
}
