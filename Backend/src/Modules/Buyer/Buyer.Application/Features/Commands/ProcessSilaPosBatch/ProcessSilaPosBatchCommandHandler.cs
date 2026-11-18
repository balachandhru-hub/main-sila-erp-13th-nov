using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ProcessSilaPosBatch
{
    /// <summary>
    /// Step 2 of the upload: processes selected (or all) RECEIVED lines of a PREVIEW batch (match, deduct stock, optionally queue ERP)
    /// and marks the batch PROCESSED when no RECEIVED lines remain.
    /// </summary>
    public class ProcessSilaPosBatchCommandHandler : IRequestHandler<ProcessSilaPosBatchCommand, SilaPosImportResultDto>
    {
        public const string CONFIG_QUEUE_ERP = "SilaPos:QueueErpPosting";

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public ProcessSilaPosBatchCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
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

            bool queueErp = request.QueueErpPosting
                ?? _configuration.GetValue(CONFIG_QUEUE_ERP, true);
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
            await _repository.SaveAsync();

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
                AlreadyPosted = alreadyPosted
            };
            SilaPosBatchDto counts = new SilaPosBatchDto { Id = batch.Id };
            await SilaPosBatchCounts.AddAsync(_repository, buyer.Id, new List<SilaPosBatchDto> { counts }, cancellationToken);
            result.PostingUnknown = counts.PostingUnknown;

            _logger.LogInfo($"POS batch processed. BatchNumber: {batch.BatchNumber}, Lines: {lines.Count}, Failed: {failed}");
            return result;
        }
    }
}
