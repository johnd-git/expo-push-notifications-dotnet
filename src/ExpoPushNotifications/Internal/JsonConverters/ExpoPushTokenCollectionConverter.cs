using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpoPushNotifications.Internal.JsonConverters;

/// <summary>
/// JSON converter that handles the "to" property which can be either a single string or an array of strings.
/// </summary>
internal sealed class ExpoPushTokenCollectionConverter : JsonConverter<IReadOnlyList<string>>
{
    public override IReadOnlyList<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                var singleToken = reader.GetString();
                return singleToken != null ? new List<string> { singleToken } : null;

            case JsonTokenType.StartArray:
                var tokens = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var token = reader.GetString();
                        if (token != null)
                        {
                            tokens.Add(token);
                        }
                    }
                }
                return tokens;

            default:
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            // Write as a single string for single recipient
            writer.WriteStringValue(value[0]);
        }
        else
        {
            // Write as an array for multiple recipients
            writer.WriteStartArray();
            foreach (var token in value)
            {
                writer.WriteStringValue(token);
            }
            writer.WriteEndArray();
        }
    }
}
