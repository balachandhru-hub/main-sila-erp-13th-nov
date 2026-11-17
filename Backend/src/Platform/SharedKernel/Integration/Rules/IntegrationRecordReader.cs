using System.Globalization;
using System.Text.Json;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;

namespace SharedKernel.Integration.Rules
{
    /// <summary>
    /// Reads mapped values out of one record of an ERP payload.
    /// </summary>
    public static class IntegrationRecordReader
    {
        public static string? Read(IReadOnlyList<ApiFieldMapping> mappings, string targetField, JsonElement record)
        {
            ApiFieldMapping? mapping = mappings.FirstOrDefault(item => item.TargetField.Equals(targetField, StringComparison.OrdinalIgnoreCase));
            return MapValue(mapping, record);
        }

        public static string? MapValue(ApiFieldMapping? mapping, JsonElement source)
        {
            if (mapping == null)
            {
                return null;
            }

            JsonElement? value = ReadPath(source, mapping.SourceField);
            if (value == null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return mapping.NullPolicy == IntegrationNullPolicy.DEFAULT_VALUE ? mapping.DefaultValue : null;
            }

            string? text = ElementText(value.Value);
            return mapping.Transformation switch
            {
                "TRIM" => text?.Trim(),
                "UPPER" => text?.Trim().ToUpperInvariant(),
                "LOWER" => text?.Trim().ToLowerInvariant(),
                _ => text
            };
        }

        /// <summary>
        /// Walks dotted / slash paths. Numeric segments index into JSON arrays (OData <c>results/0/...</c>).
        /// </summary>
        public static JsonElement? ReadPath(JsonElement source, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            JsonElement current = source;
            foreach (string segment in path.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.ValueKind == JsonValueKind.Array)
                {
                    if (!int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                        || index < 0
                        || index >= current.GetArrayLength())
                    {
                        return null;
                    }

                    current = current[index];
                    continue;
                }

                if (current.ValueKind != JsonValueKind.Object || !TryGetPropertyIgnoreCase(current, segment, out current))
                {
                    return null;
                }
            }

            return current;
        }

        /// <summary>
        /// SAP Product description from <c>to_Description.results</c> (prefer EN), then common header fields.
        /// Used when Material.Description is unmapped or empty so the Description column is populated.
        /// </summary>
        public static string? ReadSapProductDescription(JsonElement record, string preferredLanguage = "EN")
        {
            string preferred = string.IsNullOrWhiteSpace(preferredLanguage) ? "EN" : preferredLanguage.Trim().ToUpperInvariant();
            if (TryGetPropertyIgnoreCase(record, "to_Description", out JsonElement toDescription))
            {
                JsonElement results = toDescription;
                if (toDescription.ValueKind == JsonValueKind.Object
                    && TryGetPropertyIgnoreCase(toDescription, "results", out JsonElement nested))
                {
                    results = nested;
                }

                if (results.ValueKind == JsonValueKind.Array)
                {
                    string? preferredText = null;
                    string? englishText = null;
                    string? anyText = null;
                    foreach (JsonElement row in results.EnumerateArray())
                    {
                        string? text = CleanText(
                            PropertyText(row, "ProductDescription")
                            ?? PropertyText(row, "MaterialDescription")
                            ?? PropertyText(row, "Description"));
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        anyText ??= text;
                        string lang = (PropertyText(row, "Language") ?? PropertyText(row, "LanguageISOCode") ?? string.Empty)
                            .Trim()
                            .ToUpperInvariant();
                        if (lang == preferred || lang.StartsWith(preferred, StringComparison.Ordinal))
                        {
                            preferredText = text;
                            break;
                        }

                        if (englishText == null && (lang == "EN" || lang.StartsWith("EN", StringComparison.Ordinal)))
                        {
                            englishText = text;
                        }
                    }

                    string? fromExpand = preferredText ?? englishText ?? anyText;
                    if (!string.IsNullOrWhiteSpace(fromExpand))
                    {
                        return fromExpand;
                    }
                }
            }

            return CleanText(
                PropertyText(record, "ProductDescription")
                ?? PropertyText(record, "ProductName")
                ?? PropertyText(record, "MaterialDescription")
                ?? PropertyText(record, "Description"));
        }

        public static decimal? ParseDecimal(string? value)
        {
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result) ? result : null;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out value))
            {
                return true;
            }

            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in obj.EnumerateObject())
                {
                    if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string? PropertyText(JsonElement obj, string name)
        {
            return TryGetPropertyIgnoreCase(obj, name, out JsonElement value) ? ElementText(value) : null;
        }

        private static string? ElementText(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.Object or JsonValueKind.Array => null,
                _ => value.ToString()
            };
        }

        private static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
