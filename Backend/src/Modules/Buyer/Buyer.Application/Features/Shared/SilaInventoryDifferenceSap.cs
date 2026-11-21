using System.Globalization;
using System.Text.Json;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// SAP 701/702 inventory differences cannot be posted via API_MATERIAL_DOCUMENT_SRV (M7/096).
    /// They are created by counting a Physical Inventory Document then calling PostDifferences
    /// on API_PHYSICAL_INVENTORY_DOC_SRV. Credentials reuse FIVE S4 - Positive Stock Adjustment (same S4 host).
    /// </summary>
    public static class SilaInventoryDifferenceSap
    {
        public const string SERVICE = "API_PHYSICAL_INVENTORY_DOC_SRV";
        public const string HEADER_ENTITY = "A_PhysInventoryDocHeader";
        public const string STOCK_TYPE_UNRESTRICTED = "1";

        public static string HeaderPath()
        {
            return $"sap/opu/odata/sap/{SERVICE}/{HEADER_ENTITY}";
        }

        public static string ItemSetPath()
        {
            return $"sap/opu/odata/sap/{SERVICE}/A_PhysInventoryDocItem";
        }

        public static string ItemPath(string fiscalYear, string document, string item)
        {
            string year = (fiscalYear ?? string.Empty).Trim().Trim('\'');
            string doc = (document ?? string.Empty).Trim().Trim('\'');
            string line = (item ?? string.Empty).Trim().Trim('\'');
            if (line.Length > 0 && line.Length < 3 && line.All(char.IsDigit))
            {
                line = line.PadLeft(3, '0');
            }

            return $"sap/opu/odata/sap/{SERVICE}/A_PhysInventoryDocItem(FiscalYear='{year}',PhysicalInventoryDocument='{doc}',PhysicalInventoryDocumentItem='{line}')";
        }

        /// <summary>OData function import that posts counted differences for one item (produces 701 surplus / 702 shortage).</summary>
        public static string PostDifferencesPath(
            string fiscalYear,
            string document,
            string item,
            string material,
            DateTime postingDate)
        {
            string year = (fiscalYear ?? string.Empty).Trim().Trim('\'');
            string doc = (document ?? string.Empty).Trim().Trim('\'');
            string line = (item ?? string.Empty).Trim().Trim('\'');
            if (line.Length > 0 && line.Length < 3 && line.All(char.IsDigit))
            {
                line = line.PadLeft(3, '0');
            }

            string mat = (material ?? string.Empty).Trim().Trim('\'');
            string date = postingDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            return $"sap/opu/odata/sap/{SERVICE}/PostDifferencesOnItem?FiscalYear='{year}'&PhysicalInventoryDocument='{doc}'&PhysicalInventoryDocumentItem='{line}'&Material='{mat}'&PostingDate=datetime'{date}'";
        }

        /// <summary>
        /// Create PI header with material item (no Plant/StorageLocation on item — those are header-only).
        /// Count qty is entered afterwards via PATCH. A second item POST is used when deep-insert returns no item.
        /// </summary>
        public static string BuildCreateBody(
            string plant,
            string storageLocation,
            string material,
            string headerText,
            DateTime businessDate)
        {
            string postingDate = SilaInventorySap.FormatDate(businessDate);
            string text = string.IsNullOrWhiteSpace(headerText) ? "SILA STOCK COUNT" : headerText.Trim();
            if (text.Length > 40)
            {
                text = text[..40];
            }

            Dictionary<string, object?> item = new Dictionary<string, object?>
            {
                ["Material"] = material,
                ["PhysicalInventoryStockType"] = STOCK_TYPE_UNRESTRICTED
            };

            Dictionary<string, object?> header = new Dictionary<string, object?>
            {
                ["Plant"] = plant.Trim(),
                ["StorageLocation"] = storageLocation.Trim(),
                ["DocumentDate"] = postingDate,
                ["PhysInventoryPlannedCountDate"] = postingDate,
                ["PhysicalInventoryDocumentDesc"] = text,
                ["to_PhysicalInventoryDocumentItem"] = new Dictionary<string, object?>
                {
                    ["results"] = new List<Dictionary<string, object?>> { item }
                }
            };
            return JsonSerializer.Serialize(header);
        }

        /// <summary>POST one PI item onto an existing document.</summary>
        public static string BuildItemBody(string fiscalYear, string document, string material)
        {
            Dictionary<string, object?> item = new Dictionary<string, object?>
            {
                ["FiscalYear"] = fiscalYear.Trim(),
                ["PhysicalInventoryDocument"] = document.Trim(),
                ["Material"] = material,
                ["PhysicalInventoryStockType"] = STOCK_TYPE_UNRESTRICTED
            };
            return JsonSerializer.Serialize(item);
        }

        /// <summary>PATCH body to enter the physical count on a PI item (QuantityInUnitOfEntry per SAP API Hub).</summary>
        public static string BuildCountPatchBody(string material, decimal physicalQty, string entryUnit, DateTime countDate)
        {
            Dictionary<string, object?> body = new Dictionary<string, object?>
            {
                ["Material"] = material,
                ["QuantityInUnitOfEntry"] = SilaInventorySap.FormatQuantity(Math.Abs(physicalQty)),
                ["UnitOfEntry"] = entryUnit.Trim().ToUpperInvariant(),
                ["PhysicalInventoryLastCountDate"] = SilaInventorySap.FormatDate(countDate)
            };
            return JsonSerializer.Serialize(body);
        }

        public static bool TryReadDocument(string? payload, out string fiscalYear, out string document, out string item)
        {
            fiscalYear = string.Empty;
            document = string.Empty;
            item = "1";
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            try
            {
                using JsonDocument json = JsonDocument.Parse(payload);
                JsonElement root = json.RootElement;
                if (root.TryGetProperty("d", out JsonElement d))
                {
                    root = d;
                }

                fiscalYear = ReadString(root, "FiscalYear") ?? string.Empty;
                document = ReadString(root, "PhysicalInventoryDocument") ?? string.Empty;
                if (root.TryGetProperty("to_PhysicalInventoryDocumentItem", out JsonElement nav))
                {
                    JsonElement results = nav;
                    if (nav.TryGetProperty("results", out JsonElement listed))
                    {
                        results = listed;
                    }

                    if (results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement row in results.EnumerateArray())
                        {
                            string? line = ReadString(row, "PhysicalInventoryDocumentItem");
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                item = line;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(document))
                {
                    // SAP sometimes returns HTTP 400 after creating the PI doc (M7/710 in errordetails).
                    document = ReadCreatedDocumentNumber(payload) ?? string.Empty;
                }

                if (string.IsNullOrWhiteSpace(document)
                    && TryReadActiveDocument(payload, out string activeYear, out string activeDoc, out string activeItem))
                {
                    fiscalYear = activeYear;
                    document = activeDoc;
                    item = activeItem;
                }

                if (string.IsNullOrWhiteSpace(fiscalYear))
                {
                    fiscalYear = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
                }

                return !string.IsNullOrWhiteSpace(fiscalYear) && !string.IsNullOrWhiteSpace(document);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>True when SAP created the PI document even though HTTP status was not 2xx (M7/710).</summary>
        public static bool CreatedDespiteHttpError(string? payload)
        {
            return !string.IsNullOrWhiteSpace(ReadCreatedDocumentNumber(payload))
                || TryReadActiveDocument(payload, out _, out _, out _);
        }

        /// <summary>M7/724 No data changed — count already matches; treat PATCH as success and continue to PostDifferences.</summary>
        public static bool CountAlreadyEntered(string? payload)
        {
            return !string.IsNullOrWhiteSpace(payload)
                && payload.Contains("M7/724", StringComparison.Ordinal);
        }

        /// <summary>
        /// M7/495: material already has an open PI — reuse it.
        /// Example: "Physical inventory for 1000000//1060/FL01 is already active in 100000191/2026/001"
        /// </summary>
        public static bool TryReadActiveDocument(string? payload, out string fiscalYear, out string document, out string item)
        {
            fiscalYear = string.Empty;
            document = string.Empty;
            item = "1";
            if (string.IsNullOrWhiteSpace(payload))
            {
                return false;
            }

            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                payload,
                @"already active in\s+(\d+)/(\d+)/(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            document = match.Groups[1].Value;
            fiscalYear = match.Groups[2].Value;
            // Keep SAP item padding (001) — unpadded keys fail entity lookup / PATCH.
            item = match.Groups[3].Value;
            if (string.IsNullOrWhiteSpace(item))
            {
                item = "001";
            }

            return true;
        }

        private static string? ReadCreatedDocumentNumber(string payload)
        {
            // "Physical inventory document 100000190 created"
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(
                payload,
                @"Physical inventory document\s+(\d+)\s+created",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>Backward-compatible overload.</summary>
        public static bool TryReadDocument(string? payload, out string fiscalYear, out string document)
        {
            return TryReadDocument(payload, out fiscalYear, out document, out _);
        }

        /// <summary>Material document produced when differences are posted (701/702).</summary>
        public static string? ReadMaterialDocument(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using JsonDocument json = JsonDocument.Parse(payload);
                JsonElement root = json.RootElement;
                if (root.TryGetProperty("d", out JsonElement d))
                {
                    root = d;
                }

                string? document = ReadString(root, "MaterialDocument");
                if (!string.IsNullOrWhiteSpace(document))
                {
                    string? year = ReadString(root, "MaterialDocumentYear");
                    return string.IsNullOrWhiteSpace(year) ? document : $"{document}/{year}";
                }

                if (root.TryGetProperty("to_PhysicalInventoryDocumentItem", out JsonElement items)
                    || (root.TryGetProperty("d", out JsonElement nested) && nested.TryGetProperty("to_PhysicalInventoryDocumentItem", out items)))
                {
                    JsonElement results = items;
                    if (items.TryGetProperty("results", out JsonElement listed))
                    {
                        results = listed;
                    }

                    if (results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in results.EnumerateArray())
                        {
                            string? matDoc = ReadString(item, "MaterialDocument");
                            if (!string.IsNullOrWhiteSpace(matDoc))
                            {
                                string? year = ReadString(item, "MaterialDocumentYear");
                                return string.IsNullOrWhiteSpace(year) ? matDoc : $"{matDoc}/{year}";
                            }
                        }
                    }
                }

                return SilaInventorySap.ReadMaterialDocument(payload);
            }
            catch (JsonException)
            {
                return SilaInventorySap.ReadMaterialDocument(payload);
            }
        }

        private static string? ReadString(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            string? text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}
