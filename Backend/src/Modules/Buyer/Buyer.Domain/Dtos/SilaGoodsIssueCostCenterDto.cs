namespace Buyer.Domain.Dtos
{
    public class SilaGoodsIssueCostCenterDto
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
        public string PlantCode { get; set; } = string.Empty;
        /// <summary>PLANT | STORAGE</summary>
        public string Scope { get; set; } = string.Empty;
        public string StorageLocationCode { get; set; } = string.Empty;
        public string CostCenter { get; set; } = string.Empty;
        public string Status { get; set; } = "ACTIVE";
        public DateTime DateUpdated { get; set; }
    }

    public class SilaGoodsIssueCostCenterWriteDto
    {
        public Guid PropertyId { get; set; }
        /// <summary>PLANT | STORAGE</summary>
        public string Scope { get; set; } = "PLANT";
        /// <summary>Required when Scope = STORAGE (e.g. FL10).</summary>
        public string? StorageLocationCode { get; set; }
        public string CostCenter { get; set; } = string.Empty;
    }
}
