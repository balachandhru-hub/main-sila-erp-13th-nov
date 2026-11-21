namespace Buyer.Domain.Dtos
{
    /// <summary>The work waiting at the caller's location (cloud dashboard and mobile home).</summary>
    public class SilaInventoryHomeDto
    {
        /// <summary>null when the caller has no location.</summary>
        public Guid? LocationId { get; set; }
        public string? LocationCode { get; set; }
        public string? LocationName { get; set; }
        public string? LocationType { get; set; }
        public string? PropertyName { get; set; }
        /// <summary>Transfers out of the location waiting for approval.</summary>
        public int PendingTransferApprovals { get; set; }
        /// <summary>Dispatched transfers to the location waiting to be received.</summary>
        public int InTransitToReceive { get; set; }
        public int OpenStockCounts { get; set; }
        public int OpenEnquiries { get; set; }
        public int OpenAlerts { get; set; }
        /// <summary>Stocking rows whose on hand is at or under the minimum stock or reorder point.</summary>
        public int LowStockItems { get; set; }
        /// <summary>Distinct materials with an active balance at the location.</summary>
        public int StockItems { get; set; }
        /// <summary>Pending transfer approvals + open counts + open enquiries + open alerts.</summary>
        public int PendingActions { get; set; }
        /// <summary>Plant code of the property, when mapped.</summary>
        public string? PlantCode { get; set; }
        /// <summary>SAP storage location code of the location, when mapped.</summary>
        public string? StorageLocationCode { get; set; }
    }
}
