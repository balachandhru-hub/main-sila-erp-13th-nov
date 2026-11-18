using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ProcessSilaPosBatch
{
    /// <summary>Processes the RECEIVED lines of a previewed batch: match, deduct stock, optionally queue ERP posting.</summary>
    public class ProcessSilaPosBatchCommand : IRequest<SilaPosImportResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid BatchId { get; set; }
        /// <summary>When set, only these RECEIVED lines are processed; others stay in PREVIEW for later selection.</summary>
        public List<Guid>? TransactionIds { get; set; }
        /// <summary>
        /// When false, local SILA inventory is deducted but ERP postings are not queued (live SAP stays disabled).
        /// Null uses configuration SilaPos:QueueErpPosting (default true for backward compatibility).
        /// </summary>
        public bool? QueueErpPosting { get; set; }
    }
}
