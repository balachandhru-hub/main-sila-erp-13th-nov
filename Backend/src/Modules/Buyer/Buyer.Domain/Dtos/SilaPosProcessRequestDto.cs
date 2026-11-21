namespace Buyer.Domain.Dtos
{
    /// <summary>Optional body for processing a PREVIEW batch with line selection and ERP queue control.</summary>
    public class SilaPosProcessRequestDto
    {
        /// <summary>When set, only these RECEIVED sale transaction ids are processed.</summary>
        public List<Guid>? TransactionIds { get; set; }

        /// <summary>
        /// When false, deduct SILA inventory but do not queue ERP postings (live SAP stays disabled).
        /// Null uses SilaPos:QueueErpPosting configuration.
        /// </summary>
        public bool? QueueErpPosting { get; set; }
    }
}
