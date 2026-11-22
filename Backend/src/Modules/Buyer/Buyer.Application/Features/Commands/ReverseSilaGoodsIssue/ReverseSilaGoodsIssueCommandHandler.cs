using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ReverseSilaGoodsIssue
{
    /// <summary>
    /// Reverses a posted goods issue: stock returns to the store (IN) and leaves the outlet (OUT), then queues ERP.
    /// </summary>
    public class ReverseSilaGoodsIssueCommandHandler : IRequestHandler<ReverseSilaGoodsIssueCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ReverseSilaGoodsIssueCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public Task<Guid> Handle(ReverseSilaGoodsIssueCommand request, CancellationToken cancellationToken)
        {
            return SilaRetry.RunAsync(_repository, _logger, nameof(ReverseSilaGoodsIssueCommand), () => HandleOnceAsync(request, cancellationToken));
        }

        private async Task<Guid> HandleOnceAsync(ReverseSilaGoodsIssueCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            GoodsIssue? original = await _repository.GoodsIssue
                .FindFirstByConditionAsync(x => x.Id == request.GoodsIssueId && x.BuyerId == buyer.Id && x.IsActive);
            if (original == null)
            {
                throw new NotFoundCustomException("Goods issue not found.", "Select an active goods issue to reverse.");
            }

            InventoryLocation from = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, original.FromLocationId);
            InventoryLocation to = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, original.ToLocationId);
            await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, request.UserId, request.RoleId, from.Id, cancellationToken);

            bool alreadyReversed = await _repository.InventoryWorkflowEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.ReferenceType == Common.SILA_REF_GOODS_ISSUE && x.ReferenceId == original.Id
                    && x.Action == "REVERSE")
                .AnyAsync(cancellationToken);
            if (alreadyReversed)
            {
                throw new BadRequestCustomException("Already reversed.", "This goods issue already has a reverse event.");
            }

            List<GoodsIssueItem> lines = await _repository.GoodsIssueItem
                .FindByCondition(x => x.GoodsIssueId == original.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0)
            {
                throw new BadRequestCustomException("No lines to reverse.", "The goods issue has no active lines.");
            }

            string reason = string.IsNullOrWhiteSpace(request.Request.Reason)
                ? $"Reverse {original.IssueNumber}"
                : request.Request.Reason.Trim();
            SilaInputRules.MaxLength(_logger, reason, SilaInputRules.COMMENT_LENGTH, "Reason");

            string issueNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.GOODS_ISSUE, 6, cancellationToken);
            GoodsIssue reverse = new GoodsIssue
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                IssueNumber = issueNumber,
                FromLocationId = to.Id,
                ToLocationId = from.Id,
                WeeklyBucketId = null,
                IssuedBy = request.UserId,
                Comment = reason,
                IsActive = true
            };
            _repository.GoodsIssue.Create(reverse);

            Dictionary<Guid, ItemBuyerMaster> materials = await SilaAccess.GetMaterialsAsync(
                _repository, _logger, buyer.Id, lines.Select(x => x.MaterialId), cancellationToken);
            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            foreach (GoodsIssueItem line in lines)
            {
                if (!materials.TryGetValue(line.MaterialId, out ItemBuyerMaster? material))
                {
                    throw new BadRequestCustomException("Material is not stocked.", "Choose an active stock inventory material.");
                }

                _repository.GoodsIssueItem.Create(new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    GoodsIssueId = reverse.Id,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    MaterialName = material.Description ?? material.MaterialCode ?? string.Empty,
                    Quantity = line.Quantity,
                    Uom = line.Uom,
                    BaseQuantity = line.BaseQuantity,
                    IsActive = true
                });

                // Stock leaves the outlet and returns to the store.
                await ledger.PostAsync(Movement(reverse, to.Id, material, Common.SILA_DIRECTION_OUT, Common.SILA_TXN_GOODS_ISSUE_OUT, line.BaseQuantity, line.Quantity, line.Uom, reason), cancellationToken);
                await ledger.PostAsync(Movement(reverse, from.Id, material, Common.SILA_DIRECTION_IN, Common.SILA_TXN_GOODS_ISSUE_IN, line.BaseQuantity, line.Quantity, line.Uom, reason), cancellationToken);
            }

            ledger.QueueErpPosting(Common.SILA_REF_GOODS_ISSUE, reverse.Id, reverse.IssueNumber, from.Id, Common.SILA_MOVEMENT_GOODS_ISSUE);
            ledger.AddEvent(Common.SILA_REF_GOODS_ISSUE, original.Id, "REVERSE", $"Reversed as {reverse.IssueNumber}: {reason}");
            ledger.AddEvent(Common.SILA_REF_GOODS_ISSUE, reverse.Id, "CREATED", $"Reverse of {original.IssueNumber}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Goods issue reversed. Original: {original.IssueNumber}, ReverseId: {reverse.Id}");
            return reverse.Id;
        }

        private static InventoryMovement Movement(
            GoodsIssue issue, Guid locationId, ItemBuyerMaster material, string direction, string type,
            decimal baseQuantity, decimal enteredQuantity, string enteredUom, string reason)
        {
            return new InventoryMovement
            {
                LocationId = locationId,
                Material = material,
                Direction = direction,
                TransactionType = type,
                BaseQuantity = baseQuantity,
                EnteredQuantity = enteredQuantity,
                EnteredUom = enteredUom,
                ReferenceType = Common.SILA_REF_GOODS_ISSUE,
                ReferenceId = issue.Id,
                ReferenceNumber = issue.IssueNumber,
                Reason = reason
            };
        }
    }
}
