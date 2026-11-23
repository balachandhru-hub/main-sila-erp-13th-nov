using System.Text.RegularExpressions;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Resolve and maintain user purchasing profiles.
    /// Stock materials: no forced cost center / GL.
    /// Consumable / non-stock / service: account assignment category + cost center + GL required from the profile.
    /// </summary>
    public static class SilaUserPurchasingProfileRules
    {
        public const string STATUS_ACTIVE = "ACTIVE";
        public const string STATUS_INACTIVE = "INACTIVE";
        public const string INVENTORY_STOCK = "STOCK";
        public const string INVENTORY_NON_STOCK = "NON_STOCK";
        public const string INVENTORY_SERVICE = "SERVICE";
        public const string INVENTORY_CONSUMABLE = "CONSUMABLE";

        private const int MAX_CODE = 50;
        private const int MAX_NOTES = 1000;
        private static readonly Regex CurrencyPattern = new("^[A-Za-z]{3}$", RegexOptions.Compiled);

        public static bool RequiresAccountAssignment(string? inventoryType)
        {
            string type = NormalizeInventoryType(inventoryType);
            return type is INVENTORY_NON_STOCK or INVENTORY_SERVICE or INVENTORY_CONSUMABLE;
        }

        public static string NormalizeInventoryType(string? inventoryType)
        {
            if (string.IsNullOrWhiteSpace(inventoryType))
            {
                return INVENTORY_STOCK;
            }

            string type = inventoryType.Trim().ToUpperInvariant();
            return type switch
            {
                "STOCK" or "INVENTORY" => INVENTORY_STOCK,
                "NON_STOCK" or "NONSTOCK" or "NON-STOCK" => INVENTORY_NON_STOCK,
                "SERVICE" => INVENTORY_SERVICE,
                "CONSUMABLE" or "CONSUMABLES" => INVENTORY_CONSUMABLE,
                _ => type
            };
        }

        public static SilaUserPurchasingProfileDto ToDto(SilaUserPurchasingProfile row)
        {
            return new SilaUserPurchasingProfileDto
            {
                Id = row.Id,
                BuyerId = row.BuyerId,
                UserId = row.UserId,
                Username = row.Username,
                DisplayName = row.DisplayName,
                Status = row.Status,
                ProfileVersion = row.ProfileVersion,
                IsDefault = row.IsDefault,
                PropertyId = row.PropertyId,
                OutletId = row.OutletId,
                LocationId = row.LocationId,
                CompanyCode = row.CompanyCode,
                PlantCode = row.PlantCode,
                StorageLocation = row.StorageLocation,
                PurchasingOrganization = row.PurchasingOrganization,
                PurchasingGroup = row.PurchasingGroup,
                DefaultDeliveryLocation = row.DefaultDeliveryLocation,
                AccountAssignmentCategory = row.AccountAssignmentCategory,
                CostCenter = row.CostCenter,
                GlAccount = row.GlAccount,
                ProfitCenter = row.ProfitCenter,
                BusinessArea = row.BusinessArea,
                InternalOrder = row.InternalOrder,
                WbsElement = row.WbsElement,
                PreferredSupplierId = row.PreferredSupplierId,
                PreferredSupplierCode = row.PreferredSupplierCode,
                DefaultCurrency = row.DefaultCurrency,
                DefaultDeliveryLeadTimeDays = row.DefaultDeliveryLeadTimeDays,
                DefaultPoDocumentType = row.DefaultPoDocumentType,
                DefaultItemCategory = row.DefaultItemCategory,
                Notes = row.Notes,
                DateUpdated = row.DateUpdated
            };
        }

        public static void Apply(SilaUserPurchasingProfile row, SilaUserPurchasingProfileWriteDto input, Guid userId)
        {
            row.UserId = input.UserId ?? userId;
            row.Username = Clean(input.Username, 200);
            row.DisplayName = Clean(input.DisplayName, 200);
            row.IsDefault = input.IsDefault;
            row.PropertyId = input.PropertyId;
            row.OutletId = input.OutletId;
            row.LocationId = input.LocationId;
            row.CompanyCode = CleanUpper(input.CompanyCode, MAX_CODE);
            row.PlantCode = CleanUpper(input.PlantCode, MAX_CODE);
            row.StorageLocation = CleanUpper(input.StorageLocation, MAX_CODE);
            row.PurchasingOrganization = CleanUpper(input.PurchasingOrganization, MAX_CODE);
            row.PurchasingGroup = CleanUpper(input.PurchasingGroup, MAX_CODE);
            row.DefaultDeliveryLocation = Clean(input.DefaultDeliveryLocation, 100);
            row.AccountAssignmentCategory = CleanUpper(input.AccountAssignmentCategory, 10);
            row.CostCenter = CleanUpper(input.CostCenter, MAX_CODE);
            row.GlAccount = CleanUpper(input.GlAccount, MAX_CODE);
            row.ProfitCenter = CleanUpper(input.ProfitCenter, MAX_CODE);
            row.BusinessArea = CleanUpper(input.BusinessArea, MAX_CODE);
            row.InternalOrder = CleanUpper(input.InternalOrder, MAX_CODE);
            row.WbsElement = CleanUpper(input.WbsElement, MAX_CODE);
            row.PreferredSupplierId = input.PreferredSupplierId;
            row.PreferredSupplierCode = CleanUpper(input.PreferredSupplierCode, MAX_CODE);
            row.DefaultCurrency = CleanUpper(input.DefaultCurrency, 3);
            row.DefaultDeliveryLeadTimeDays = input.DefaultDeliveryLeadTimeDays;
            row.DefaultPoDocumentType = CleanUpper(input.DefaultPoDocumentType, 10);
            row.DefaultItemCategory = CleanUpper(input.DefaultItemCategory, 10);
            row.Notes = Clean(input.Notes, MAX_NOTES);
            row.Status = STATUS_ACTIVE;
            row.IsActive = true;
        }

        public static List<string> ValidateWrite(SilaUserPurchasingProfileWriteDto input)
        {
            List<string> errors = new();
            if (!string.IsNullOrWhiteSpace(input.DefaultCurrency) && !CurrencyPattern.IsMatch(input.DefaultCurrency.Trim()))
            {
                errors.Add("Default currency must be a 3-letter ISO code.");
            }

            if (input.DefaultDeliveryLeadTimeDays != null && input.DefaultDeliveryLeadTimeDays < 0)
            {
                errors.Add("Default delivery lead time cannot be negative.");
            }

            if (!string.IsNullOrWhiteSpace(input.Notes) && input.Notes.Trim().Length > MAX_NOTES)
            {
                errors.Add($"Notes must be at most {MAX_NOTES} characters.");
            }

            return errors;
        }

        public static async Task ClearOtherDefaultsAsync(
            IRepositoryWrapper repository, Guid buyerId, Guid userId, Guid keepId, CancellationToken cancellationToken)
        {
            List<SilaUserPurchasingProfile> others = await repository.SilaUserPurchasingProfile
                .FindByCondition(x => x.BuyerId == buyerId && x.UserId == userId && x.IsActive && x.IsDefault && x.Id != keepId)
                .ToListAsync(cancellationToken);
            foreach (SilaUserPurchasingProfile other in others)
            {
                other.IsDefault = false;
            }
        }

        public static async Task<SilaUserPurchasingProfile?> FindBestAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid userId,
            Guid? profileId,
            Guid? propertyId,
            Guid? outletId,
            Guid? locationId,
            string? plantCode,
            CancellationToken cancellationToken)
        {
            if (profileId != null)
            {
                return await repository.SilaUserPurchasingProfile.FindFirstByConditionAsync(
                    x => x.Id == profileId.Value && x.BuyerId == buyerId && x.UserId == userId && x.IsActive && x.Status == STATUS_ACTIVE);
            }

            List<SilaUserPurchasingProfile> profiles = await repository.SilaUserPurchasingProfile
                .FindByCondition(x => x.BuyerId == buyerId && x.UserId == userId && x.IsActive && x.Status == STATUS_ACTIVE)
                .ToListAsync(cancellationToken);
            if (profiles.Count == 0)
            {
                return null;
            }

            string? plant = CleanUpper(plantCode, MAX_CODE);
            SilaUserPurchasingProfile? scored = profiles
                .Select(p => new { Profile = p, Score = Score(p, propertyId, outletId, locationId, plant) })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Profile.IsDefault)
                .ThenByDescending(x => x.Profile.ProfileVersion)
                .ThenByDescending(x => x.Profile.DateUpdated)
                .Select(x => x.Profile)
                .FirstOrDefault();
            return scored;
        }

        private static int Score(
            SilaUserPurchasingProfile profile, Guid? propertyId, Guid? outletId, Guid? locationId, string? plantCode)
        {
            int score = profile.IsDefault ? 10 : 0;
            if (locationId != null && profile.LocationId == locationId) score += 40;
            if (outletId != null && profile.OutletId == outletId) score += 25;
            if (propertyId != null && profile.PropertyId == propertyId) score += 15;
            if (!string.IsNullOrEmpty(plantCode) && string.Equals(profile.PlantCode, plantCode, StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }

            return score;
        }

        public static async Task<SilaUserPurchasingDefaultsDto> ResolveAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid requestingUserId,
            ResolveSilaUserPurchasingDefaultsRequestDto request,
            CancellationToken cancellationToken)
        {
            Guid userId = request.UserId ?? requestingUserId;
            string? inventoryType = request.InventoryType;
            if (string.IsNullOrWhiteSpace(inventoryType) && !string.IsNullOrWhiteSpace(request.MaterialCode))
            {
                string code = request.MaterialCode.Trim().ToUpperInvariant();
                ItemBuyerMaster? material = await repository.ItemBuyerMaster
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.MaterialCode == code);
                if (material != null)
                {
                    inventoryType = material.InventoryType
                        ?? (material.IsInventoryItem ? INVENTORY_STOCK : INVENTORY_NON_STOCK);
                }
            }

            inventoryType = NormalizeInventoryType(inventoryType);
            SilaUserPurchasingProfile? profile = await FindBestAsync(
                repository,
                buyerId,
                userId,
                request.ProfileId,
                request.PropertyId,
                request.OutletId,
                request.LocationId,
                request.PlantCode,
                cancellationToken);

            SilaUserPurchasingDefaultsDto defaults = new()
            {
                InventoryType = inventoryType,
                RequiresAccountAssignment = RequiresAccountAssignment(inventoryType)
            };

            if (profile == null)
            {
                logger.LogInfo($"No purchasing profile for user. BuyerId: {buyerId}, UserId: {userId}");
                defaults.ProfileFound = false;
                if (defaults.RequiresAccountAssignment)
                {
                    defaults.ValidationErrors.Add("No purchasing profile is configured for consumable account assignment.");
                }

                return defaults;
            }

            defaults.ProfileFound = true;
            defaults.ProfileId = profile.Id;
            defaults.ProfileVersion = profile.ProfileVersion;
            defaults.IsDefault = profile.IsDefault;
            defaults.PropertyId = profile.PropertyId ?? request.PropertyId;
            defaults.OutletId = profile.OutletId ?? request.OutletId;
            defaults.LocationId = profile.LocationId ?? request.LocationId;
            defaults.CompanyCode = profile.CompanyCode;
            defaults.PlantCode = profile.PlantCode ?? CleanUpper(request.PlantCode, MAX_CODE);
            defaults.StorageLocation = profile.StorageLocation;
            defaults.PurchasingOrganization = profile.PurchasingOrganization;
            defaults.PurchasingGroup = profile.PurchasingGroup;
            defaults.DefaultDeliveryLocation = profile.DefaultDeliveryLocation;
            defaults.ProfitCenter = profile.ProfitCenter;
            defaults.BusinessArea = profile.BusinessArea;
            defaults.InternalOrder = profile.InternalOrder;
            defaults.WbsElement = profile.WbsElement;
            defaults.PreferredSupplierId = profile.PreferredSupplierId;
            defaults.PreferredSupplierCode = profile.PreferredSupplierCode;
            defaults.DefaultCurrency = profile.DefaultCurrency;
            defaults.DefaultDeliveryLeadTimeDays = profile.DefaultDeliveryLeadTimeDays;
            defaults.DefaultPoDocumentType = profile.DefaultPoDocumentType;
            defaults.DefaultItemCategory = profile.DefaultItemCategory;

            // Stock: never force account assignment from the profile.
            if (defaults.RequiresAccountAssignment)
            {
                defaults.AccountAssignmentCategory = profile.AccountAssignmentCategory;
                defaults.CostCenter = profile.CostCenter;
                defaults.GlAccount = profile.GlAccount;
                if (string.IsNullOrWhiteSpace(defaults.AccountAssignmentCategory))
                {
                    defaults.ValidationErrors.Add("Account assignment category is required for consumable items.");
                }

                if (string.IsNullOrWhiteSpace(defaults.CostCenter))
                {
                    defaults.ValidationErrors.Add("Cost center is required for consumable items.");
                }

                if (string.IsNullOrWhiteSpace(defaults.GlAccount))
                {
                    defaults.ValidationErrors.Add("GL account is required for consumable items.");
                }
            }
            else
            {
                defaults.AccountAssignmentCategory = null;
                defaults.CostCenter = null;
                defaults.GlAccount = null;
            }

            return defaults;
        }

        public static async Task EnsureOwnedAsync(
            IRepositoryWrapper repository, ILoggerManager logger, Guid buyerId, Guid id, Guid? userIdFilter = null)
        {
            SilaUserPurchasingProfile? row = await repository.SilaUserPurchasingProfile
                .FindFirstByConditionAsync(x => x.Id == id && x.BuyerId == buyerId && x.IsActive);
            if (row == null || (userIdFilter != null && row.UserId != userIdFilter))
            {
                logger.LogError($"Purchasing profile not found. Id: {id}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Purchasing profile not found.", "Select an active user purchasing profile.");
            }
        }

        private static string? Clean(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }

        private static string? CleanUpper(string? value, int max)
        {
            string? cleaned = Clean(value, max);
            return cleaned?.ToUpperInvariant();
        }
    }
}
