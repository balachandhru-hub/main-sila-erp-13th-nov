using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ProcessSilaPosBatch
{
    /// <summary>
    /// Confirm posting for a preview batch: Step 1 local validate/deduct, Step 2 SAP API post, Step 3 tracker status update.
    /// </summary>
    public class ProcessSilaPosBatchCommand : IRequest<SilaPosImportResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        /// <summary>When set, only these RECEIVED lines are processed; others stay in PREVIEW for later selection.</summary>
        public List<Guid>? TransactionIds { get; set; }
        /// <summary>
        /// When false, only Step 1 runs (local SILA inventory). Null or true runs Step 2 (SAP Material Document API).
        /// </summary>
        public bool? QueueErpPosting { get; set; }
    }
}
