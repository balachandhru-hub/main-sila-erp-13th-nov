using System.Text.Json;
using System.Text.Json.Serialization;

namespace Buyer.Domain.Serialization
{
    /// <summary>
    /// Treats JSON "" / whitespace as null for Guid? so model binding does not 400.
    /// </summary>
    public sealed class EmptyStringNullableGuidConverter : JsonConverter<Guid?>
    {
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? raw = reader.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return null;
                }

                if (Guid.TryParse(raw.Trim(), out Guid value))
                {
                    return value;
                }

                throw new JsonException($"The JSON value could not be converted to System.Nullable`1[System.Guid]. Path value: '{raw}'.");
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                throw new JsonException("Expected a Guid string or null.");
            }

            // Allow default Guid JSON token forms if present.
            if (reader.TryGetGuid(out Guid direct))
            {
                return direct;
            }

            throw new JsonException("The JSON value could not be converted to System.Nullable`1[System.Guid].");
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }
}
