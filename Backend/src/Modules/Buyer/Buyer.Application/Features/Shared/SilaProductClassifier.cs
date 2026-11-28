using System.Text.RegularExpressions;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Hospitality class of a stocked material (FOOD | BEVERAGE | MINIBAR | TOBACCO | GENERAL) used by Live Inventory
    /// to group stock. It is derived from the Item Master text and the stocking location, never stored.
    /// </summary>
    public static class SilaProductClassifier
    {
        public const string FOOD = "FOOD";
        public const string BEVERAGE = "BEVERAGE";
        public const string MINIBAR = "MINIBAR";
        public const string TOBACCO = "TOBACCO";
        public const string GENERAL = "GENERAL";

        public static readonly string[] Classes = { BEVERAGE, FOOD, MINIBAR, TOBACCO, GENERAL };

        private static readonly Regex TobaccoPattern = new Regex(
            @"\b(tobacco|cigar|cigars|cigarette|cigarettes|shisha|hookah|vape|snus|molasses)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MinibarPattern = new Regex(
            @"(mini\s?bar|miniature|\b(5|10|20|30|40|50)\s?ml\b)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex BeveragePattern = new Regex(
            @"\b(gin|vodka|whisk(?:e)?y|rum|tequila|mezcal|brandy|cognac|liqueur|vermouth|aperitif|wine|champagne|prosecco|beer|lager|ale|stout|cider|spirit|spirits|bourbon|scotch|sake|tonic|soda|cola|juice|water|coffee|tea|syrup|bitters|mixer|beverage|bev|soft drink|energy drink)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex FoodPattern = new Regex(
            @"\b(food|meat|beef|lamb|chicken|poultry|fish|seafood|shrimp|prawn|vegetable|fruit|dairy|milk|cheese|butter|cream|egg|flour|rice|pasta|bread|bakery|sugar|salt|spice|herb|oil|sauce|chocolate|dessert|grocery|frozen|fresh|dry)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Infer(MaterialEntity material, string? storeCategory, string? locationName)
        {
            string text = string.Join(' ', new[] { material.MaterialGroup, material.ProductType, material.Description, material.ValuationClass }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (TobaccoPattern.IsMatch(text)) return TOBACCO;
            if (MinibarPattern.IsMatch(text)) return MINIBAR;
            if (BeveragePattern.IsMatch(text)) return BEVERAGE;
            if (FoodPattern.IsMatch(text)) return FOOD;

            if (!string.IsNullOrWhiteSpace(locationName) && Regex.IsMatch(locationName, @"mini\s?bar", RegexOptions.IgnoreCase)) return MINIBAR;
            return (storeCategory ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "BEVERAGE" => BEVERAGE,
                "FOOD" => FOOD,
                "TOBACCO" => TOBACCO,
                _ => GENERAL
            };
        }

        public static string? Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string upper = value.Trim().ToUpperInvariant();
            return Array.IndexOf(Classes, upper) >= 0 ? upper : null;
        }
    }
}
