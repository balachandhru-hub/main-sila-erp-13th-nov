using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>Flexible PO defaults: freeform params + if/then conditions.</summary>
    public static class SilaUserPoFlexRules
    {
        public const string TYPE_PO_CREATE = "PO_CREATE";
        public const string STATUS_ACTIVE = "ACTIVE";
        public const string STATUS_INACTIVE = "INACTIVE";
        public const string OP_EQ = "EQ";
        public const string OP_NE = "NE";
        public const string OP_CONTAINS = "CONTAINS";

        private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["COMPANYCODE"] = "CompanyCode",
            ["COMPANY_CODE"] = "CompanyCode",
            ["COMPANY"] = "CompanyCode",
            ["PLANTCODE"] = "PlantCode",
            ["PLANT_CODE"] = "PlantCode",
            ["PLANT"] = "PlantCode",
            ["STORAGELOCATION"] = "StorageLocation",
            ["STORAGE_LOCATION"] = "StorageLocation",
            ["SLOC"] = "StorageLocation",
            ["PURCHASINGORGANIZATION"] = "PurchasingOrganization",
            ["PURCHASING_ORGANIZATION"] = "PurchasingOrganization",
            ["PURCHASEORG"] = "PurchasingOrganization",
            ["PORG"] = "PurchasingOrganization",
            ["PURCHASINGGROUP"] = "PurchasingGroup",
            ["PURCHASING_GROUP"] = "PurchasingGroup",
            ["PURCHASEGROUP"] = "PurchasingGroup",
            ["PGRP"] = "PurchasingGroup",
            ["ACCOUNTASSIGNMENTCATEGORY"] = "AccountAssignmentCategory",
            ["ACCOUNT_ASSIGNMENT_CATEGORY"] = "AccountAssignmentCategory",
            ["ACCTASSIGNMENT"] = "AccountAssignmentCategory",
            ["ACCT"] = "AccountAssignmentCategory",
            ["AA"] = "AccountAssignmentCategory",
            ["COSTCENTER"] = "CostCenter",
            ["COST_CENTER"] = "CostCenter",
            ["CC"] = "CostCenter",
            ["GLACCOUNT"] = "GlAccount",
            ["GL_ACCOUNT"] = "GlAccount",
            ["GL"] = "GlAccount",
            ["MATERIALGROUP"] = "MaterialGroup",
            ["MATERIAL_GROUP"] = "MaterialGroup",
            ["MATGRP"] = "MaterialGroup",
            ["PROFITCENTER"] = "ProfitCenter",
            ["PREFERRED_SUPPLIER"] = "PreferredSupplierCode",
            ["PREFERREDSUPPLIERCODE"] = "PreferredSupplierCode",
            ["SUPPLIER"] = "PreferredSupplierCode",
            ["CURRENCY"] = "DefaultCurrency",
            ["DEFAULTCURRENCY"] = "DefaultCurrency",
            ["PODOCUMENTTYPE"] = "DefaultPoDocumentType",
            ["DOCUMENTTYPE"] = "DefaultPoDocumentType",
            ["DOCTYPE"] = "DefaultPoDocumentType",
        };

        public static string NormalizeType(string? configType)
        {
            string type = (configType ?? TYPE_PO_CREATE).Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
            return string.IsNullOrWhiteSpace(type) ? TYPE_PO_CREATE : type;
        }

        public static string CanonicalKey(string? key)
        {
            string raw = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string compact = raw.Replace(" ", "").Replace("-", "_");
            if (KeyAliases.TryGetValue(compact, out string? canonical))
            {
                return canonical;
            }

            if (KeyAliases.TryGetValue(compact.Replace("_", ""), out canonical))
            {
                return canonical;
            }

            return raw;
        }

        public static Dictionary<string, string?> BuildParameterBag(
            IEnumerable<SilaUserPoFlexParam> parameters,
            IEnumerable<SilaUserPoFlexCondition> conditions)
        {
            Dictionary<string, string?> bag = new(StringComparer.OrdinalIgnoreCase);
            foreach (SilaUserPoFlexParam row in parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder))
            {
                string key = CanonicalKey(row.ParamKey);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                bag[key] = string.IsNullOrWhiteSpace(row.ParamValue) ? null : row.ParamValue.Trim();
            }

            ApplyConditions(bag, conditions);
            return bag;
        }

        public static void ApplyConditions(
            Dictionary<string, string?> bag,
            IEnumerable<SilaUserPoFlexCondition> conditions)
        {
            foreach (SilaUserPoFlexCondition rule in conditions.Where(c => c.IsActive).OrderBy(c => c.SortOrder))
            {
                string whenParam = CanonicalKey(rule.WhenParam);
                string thenParam = CanonicalKey(rule.ThenParam);
                if (string.IsNullOrWhiteSpace(whenParam) || string.IsNullOrWhiteSpace(thenParam))
                {
                    continue;
                }

                bag.TryGetValue(whenParam, out string? current);
                string op = (rule.WhenOperator ?? OP_EQ).Trim().ToUpperInvariant();
                string expect = rule.WhenValue?.Trim() ?? string.Empty;
                string actual = current?.Trim() ?? string.Empty;
                bool matched = op switch
                {
                    OP_NE => !string.Equals(actual, expect, StringComparison.OrdinalIgnoreCase),
                    OP_CONTAINS => actual.Contains(expect, StringComparison.OrdinalIgnoreCase),
                    _ => string.Equals(actual, expect, StringComparison.OrdinalIgnoreCase)
                };
                if (matched)
                {
                    bag[thenParam] = string.IsNullOrWhiteSpace(rule.ThenValue) ? null : rule.ThenValue.Trim();
                }
            }
        }

        public static void MapBagToDefaults(Dictionary<string, string?> bag, SilaUserPurchasingDefaultsDto defaults)
        {
            defaults.Parameters = new Dictionary<string, string?>(bag, StringComparer.OrdinalIgnoreCase);
            defaults.CompanyCode = Get(bag, "CompanyCode") ?? defaults.CompanyCode;
            defaults.PlantCode = Get(bag, "PlantCode") ?? defaults.PlantCode;
            defaults.StorageLocation = Get(bag, "StorageLocation") ?? defaults.StorageLocation;
            defaults.PurchasingOrganization = Get(bag, "PurchasingOrganization") ?? defaults.PurchasingOrganization;
            defaults.PurchasingGroup = Get(bag, "PurchasingGroup") ?? defaults.PurchasingGroup;
            defaults.AccountAssignmentCategory = Get(bag, "AccountAssignmentCategory") ?? defaults.AccountAssignmentCategory;
            defaults.CostCenter = Get(bag, "CostCenter") ?? defaults.CostCenter;
            defaults.GlAccount = Get(bag, "GlAccount") ?? defaults.GlAccount;
            defaults.MaterialGroup = Get(bag, "MaterialGroup") ?? defaults.MaterialGroup;
            defaults.ProfitCenter = Get(bag, "ProfitCenter") ?? defaults.ProfitCenter;
            defaults.BusinessArea = Get(bag, "BusinessArea") ?? defaults.BusinessArea;
            defaults.InternalOrder = Get(bag, "InternalOrder") ?? defaults.InternalOrder;
            defaults.WbsElement = Get(bag, "WbsElement") ?? defaults.WbsElement;
            defaults.PreferredSupplierCode = Get(bag, "PreferredSupplierCode") ?? defaults.PreferredSupplierCode;
            defaults.DefaultCurrency = Get(bag, "DefaultCurrency") ?? defaults.DefaultCurrency;
            defaults.DefaultPoDocumentType = Get(bag, "DefaultPoDocumentType") ?? defaults.DefaultPoDocumentType;
            defaults.DefaultItemCategory = Get(bag, "DefaultItemCategory") ?? defaults.DefaultItemCategory;
            defaults.DefaultDeliveryLocation = Get(bag, "DefaultDeliveryLocation") ?? defaults.DefaultDeliveryLocation;
            if (!string.IsNullOrWhiteSpace(defaults.AccountAssignmentCategory))
            {
                defaults.RequiresAccountAssignment = true;
            }
        }

        public static SilaUserPoFlexConfigDto ToDto(
            SilaUserPoFlexConfig config,
            IEnumerable<SilaUserPoFlexParam> parameters,
            IEnumerable<SilaUserPoFlexCondition> conditions)
        {
            return new SilaUserPoFlexConfigDto
            {
                Id = config.Id,
                BuyerId = config.BuyerId,
                UserId = config.UserId,
                Username = config.Username,
                DisplayName = config.DisplayName,
                ConfigType = config.ConfigType,
                Name = config.Name,
                Status = config.Status,
                ConfigVersion = config.ConfigVersion,
                IsDefault = config.IsDefault,
                Notes = config.Notes,
                DateUpdated = config.DateUpdated,
                Parameters = parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder)
                    .Select(p => new SilaUserPoFlexParamDto
                    {
                        Id = p.Id,
                        ParamKey = p.ParamKey,
                        ParamValue = p.ParamValue,
                        SortOrder = p.SortOrder
                    }).ToList(),
                Conditions = conditions.Where(c => c.IsActive).OrderBy(c => c.SortOrder)
                    .Select(c => new SilaUserPoFlexConditionDto
                    {
                        Id = c.Id,
                        WhenParam = c.WhenParam,
                        WhenOperator = c.WhenOperator,
                        WhenValue = c.WhenValue,
                        ThenParam = c.ThenParam,
                        ThenValue = c.ThenValue,
                        SortOrder = c.SortOrder
                    }).ToList()
            };
        }

        public static async Task ClearOtherDefaultsAsync(
            IRepositoryWrapper repository, Guid buyerId, Guid userId, string configType, Guid keepId, CancellationToken cancellationToken)
        {
            List<SilaUserPoFlexConfig> others = await repository.SilaUserPoFlexConfig
                .FindByCondition(x => x.BuyerId == buyerId && x.UserId == userId && x.ConfigType == configType
                    && x.IsActive && x.IsDefault && x.Id != keepId)
                .ToListAsync(cancellationToken);
            foreach (SilaUserPoFlexConfig row in others)
            {
                row.IsDefault = false;
                repository.SilaUserPoFlexConfig.Update(row);
            }
        }

        public static async Task EnsureOwnedAsync(
            IRepositoryWrapper repository, ILoggerManager logger, Guid buyerId, Guid id)
        {
            SilaUserPoFlexConfig? row = await repository.SilaUserPoFlexConfig
                .FindFirstByConditionAsync(x => x.Id == id && x.BuyerId == buyerId && x.IsActive);
            if (row == null)
            {
                logger.LogError($"PO flex config not found. Id: {id}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("PO defaults config not found.", "Select an active flex PO defaults config.");
            }
        }

        public static async Task<SilaUserPoFlexConfig?> FindBestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid userId,
            string configType,
            Guid? configId,
            CancellationToken cancellationToken)
        {
            if (configId != null)
            {
                return await repository.SilaUserPoFlexConfig
                    .FindFirstByConditionAsync(x => x.Id == configId && x.BuyerId == buyerId && x.UserId == userId
                        && x.ConfigType == configType && x.IsActive && x.Status == STATUS_ACTIVE);
            }

            List<SilaUserPoFlexConfig> rows = await repository.SilaUserPoFlexConfig
                .FindByCondition(x => x.BuyerId == buyerId && x.UserId == userId && x.ConfigType == configType
                    && x.IsActive && x.Status == STATUS_ACTIVE)
                .OrderByDescending(x => x.IsDefault)
                .ThenByDescending(x => x.DateUpdated)
                .ToListAsync(cancellationToken);
            return rows.FirstOrDefault();
        }

        private static string? Get(Dictionary<string, string?> bag, string key)
        {
            return bag.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
        }
    }
}
