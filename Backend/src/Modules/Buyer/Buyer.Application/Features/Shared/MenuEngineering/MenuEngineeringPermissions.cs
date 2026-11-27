using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering
{
    /// <summary>
    /// Authoritative backend permission evaluator for Menu Engineering &amp; ME Studio.
    /// Evaluates Identity permission keys from HttpContext.Items["sila.permissions"] with deterministic role fallback.
    /// Enforces cost/margin redaction when confidential financial access is denied (Section 27 / Scenario 12).
    /// </summary>
    public static class MenuEngineeringPermissions
    {
        public static MenuEngineeringPermissionSetDto Evaluate(
            IHttpContextAccessor? httpContextAccessor,
            Guid roleId,
            bool menuEngineeringEnabled = true,
            bool meStudioEnabled = true,
            MenuEngineeringFeatureFlagsDto? featureFlags = null)
        {
            MenuEngineeringFeatureFlagsDto resolvedFlags = featureFlags ?? new MenuEngineeringFeatureFlagsDto
            {
                MenuEngineering = menuEngineeringEnabled,
                MeStudio = meStudioEnabled
            };
            resolvedFlags.MenuEngineering = menuEngineeringEnabled;
            resolvedFlags.MeStudio = meStudioEnabled;

            HashSet<string>? codes = ReadPermissionCodes(httpContextAccessor);
            bool hasExplicitCodes = codes is { Count: > 0 };

            bool HasAny(params string[] keys) =>
                hasExplicitCodes && keys.Any(k => codes!.Contains(k));

            bool isFullRole = SilaAccess.HasFullAccess(roleId);
            bool isAdmin = roleId == Common.BUYER_ADMIN_ROLE_ID;
            bool isFnb = roleId == Common.FNB_DIRECTOR_ROLE_ID;
            bool isCostController = roleId == Common.COST_CONTROLLER_ROLE_ID;
            bool isFinance = roleId == Common.FINANCE_MANAGER_ROLE_ID;
            bool isStoreMgr = roleId == Common.STORE_MANAGER_ROLE_ID;
            bool isOutletMgr = roleId == Common.OUTLET_MANAGER_ROLE_ID;
            bool isApProcessor = !isAdmin && !isFnb && !isCostController && !isFinance && !isStoreMgr && !isOutletMgr && roleId != Guid.Empty;

            if (isApProcessor && !HasAny("VIEW_SILA_MENU_ENGINEERING", "VIEW_SILA_ME_STUDIO", "MANAGE_SILA_MENU_ENGINEERING"))
            {
                return new MenuEngineeringPermissionSetDto
                {
                    MenuEngineeringEnabled = menuEngineeringEnabled,
                    MeStudioEnabled = meStudioEnabled,
                    FeatureFlags = resolvedFlags
                };
            }

            bool legacyView = HasAny("VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING");
            bool legacyManage = HasAny("MANAGE_SILA_MENU_ENGINEERING");

            bool viewCost = HasAny("VIEW_SILA_MENU_COST") || (isFullRole && (legacyManage || !hasExplicitCodes || legacyView));
            bool viewMargin = HasAny("VIEW_SILA_MENU_MARGIN") || (isFullRole && (legacyManage || !hasExplicitCodes || legacyView));

            return new MenuEngineeringPermissionSetDto
            {
                MenuEngineeringEnabled = menuEngineeringEnabled,
                MeStudioEnabled = meStudioEnabled,
                FeatureFlags = resolvedFlags,
                ViewMeStudio = HasAny("VIEW_SILA_ME_STUDIO") || legacyView || !isApProcessor,
                CreateMenu = HasAny("CREATE_SILA_MENU") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                EditDraftMenu = HasAny("EDIT_SILA_DRAFT_MENU") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                ManageMenuItems = HasAny("MANAGE_SILA_MENU_ITEMS") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr,
                ManagePortionPlans = HasAny("MANAGE_SILA_PORTION_PLANS") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                ManageDemandPlans = HasAny("MANAGE_SILA_DEMAND_PLANS") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                ViewPosSales = HasAny("VIEW_SILA_POS_SALES") || legacyView || !isApProcessor,
                ManagePosMapping = HasAny("MANAGE_SILA_POS_MAPPING") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                ViewMenuPerformance = HasAny("VIEW_SILA_MENU_PERFORMANCE") || legacyView || !isApProcessor,
                ViewMenuCost = viewCost,
                ViewMenuMargin = viewMargin,
                GeneratePurchaseRecommendation = HasAny("GENERATE_SILA_MENU_PURCHASE_REC") || legacyManage || isAdmin || isFnb || isStoreMgr || isOutletMgr || isCostController,
                UseAiMenuIntelligence = HasAny("USE_SILA_AI_MENU_INTELLIGENCE") || legacyView || !isApProcessor,
                RunMenuSimulation = HasAny("RUN_SILA_MENU_SIMULATION") || legacyView || !isApProcessor,
                ApproveMenu = HasAny("APPROVE_SILA_MENU") || isAdmin || isFnb || isCostController || isFinance,
                PublishMenu = HasAny("PUBLISH_SILA_MENU") || isAdmin || isFnb || isFinance,
                ManageMenuSetup = HasAny("MANAGE_SILA_MENU_SETUP") || isAdmin || isFnb || (legacyManage && isStoreMgr)
            };
        }

        public static void EnsureAllowed(bool allowed, string title, string detail)
        {
            if (!allowed)
            {
                throw new ForBiddenCustomException(title, detail);
            }
        }

        public static void RedactFinancials(MenuEngineeringPermissionSetDto perms, MenuItemEnhancedDto item)
        {
            if (!perms.ViewMenuCost)
            {
                item.IngredientCost = null;
                item.CostPerPortion = null;
                item.FoodCostPercent = null;
            }
            if (!perms.ViewMenuMargin)
            {
                item.ContributionMargin = null;
                item.FoodCostPercent = null;
            }
        }

        public static void RedactFinancials(MenuEngineeringPermissionSetDto perms, MenuPerformanceMatrixRowDto row)
        {
            if (!perms.ViewMenuCost)
            {
                row.CostPerPortion = null;
                row.TotalIngredientCost = null;
                row.FoodCostPercent = null;
                row.WasteImpactCost = null;
            }
            if (!perms.ViewMenuMargin)
            {
                row.ContributionMargin = null;
                row.TotalContributionMargin = null;
                row.MarginPercent = null;
                row.FoodCostPercent = null;
                row.CategoryWeightedAvgMargin = null;
                row.EstimatedFinancialImpact = null;
            }
        }

        public static void RedactFinancials(MenuEngineeringPermissionSetDto perms, MeStudioLiveIntelligenceDto live)
        {
            if (!perms.ViewMenuCost)
            {
                live.TotalMenuCost = null;
                live.CostPerGuest = null;
                live.ExpectedFoodCostPercent = null;
            }
            if (!perms.ViewMenuMargin)
            {
                live.ExpectedContributionMargin = null;
                live.ExpectedFoodCostPercent = null;
                live.HistoricalAvgMarginPercent = null;
            }
        }

        private static HashSet<string>? ReadPermissionCodes(IHttpContextAccessor? accessor)
        {
            object? raw = accessor?.HttpContext?.Items?["sila.permissions"];
            if (raw is IEnumerable<string> enumerable)
            {
                return enumerable
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToUpperInvariant())
                    .ToHashSet();
            }
            return null;
        }
    }
}
