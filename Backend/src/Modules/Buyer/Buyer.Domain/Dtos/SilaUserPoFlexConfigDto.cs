namespace Buyer.Domain.Dtos
{
    public class SilaUserPoFlexParamDto
    {
        public Guid? Id { get; set; }
        public string ParamKey { get; set; } = string.Empty;
        public string? ParamValue { get; set; }
        public int SortOrder { get; set; }
    }

    public class SilaUserPoFlexConditionDto
    {
        public Guid? Id { get; set; }
        public string WhenParam { get; set; } = string.Empty;
        /// <summary>EQ | NE | CONTAINS</summary>
        public string WhenOperator { get; set; } = "EQ";
        public string? WhenValue { get; set; }
        public string ThenParam { get; set; } = string.Empty;
        public string? ThenValue { get; set; }
        public int SortOrder { get; set; }
    }

    public class SilaUserPoFlexConfigDto
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public Guid UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string ConfigType { get; set; } = "PO_CREATE";
        public string? Name { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public int ConfigVersion { get; set; }
        public bool IsDefault { get; set; }
        public string? Notes { get; set; }
        public List<SilaUserPoFlexParamDto> Parameters { get; set; } = new();
        public List<SilaUserPoFlexConditionDto> Conditions { get; set; } = new();
        public DateTime DateUpdated { get; set; }
    }

    public class SilaUserPoFlexConfigWriteDto
    {
        public Guid? UserId { get; set; }
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string ConfigType { get; set; } = "PO_CREATE";
        public string? Name { get; set; }
        public bool IsDefault { get; set; }
        public string? Notes { get; set; }
        public List<SilaUserPoFlexParamDto> Parameters { get; set; } = new();
        public List<SilaUserPoFlexConditionDto> Conditions { get; set; } = new();
    }
}
