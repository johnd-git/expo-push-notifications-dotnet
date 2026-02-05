using System.Text.Json;
using System.Text.Json.Serialization;
using ExpoPushNotifications.Models;

namespace ExpoPushNotifications.Internal.JsonConverters;

/// <summary>
/// JSON converter for <see cref="ExpoPushReceipt"/> that handles polymorphic deserialization
/// based on the "status" property.
/// </summary>
internal sealed class ExpoPushReceiptConverter : JsonConverter<ExpoPushReceipt>
{
    public override ExpoPushReceipt? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("status", out var statusElement))
        {
            throw new JsonException("ExpoPushReceipt must have a 'status' property.");
        }

        var status = statusElement.GetString();
        var json = root.GetRawText();

        return status switch
        {
            "ok" => JsonSerializer.Deserialize<ExpoPushSuccessReceipt>(json, GetOptionsWithoutConverter(options)),
            "error" => JsonSerializer.Deserialize<ExpoPushErrorReceipt>(json, GetOptionsWithoutConverter(options)),
            _ => throw new JsonException($"Unknown status value: {status}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ExpoPushReceipt value, JsonSerializerOptions options)
    {
        var optionsWithoutConverter = GetOptionsWithoutConverter(options);

        switch (value)
        {
            case ExpoPushSuccessReceipt successReceipt:
                JsonSerializer.Serialize(writer, successReceipt, optionsWithoutConverter);
                break;
            case ExpoPushErrorReceipt errorReceipt:
                JsonSerializer.Serialize(writer, errorReceipt, optionsWithoutConverter);
                break;
            default:
                throw new JsonException($"Unknown receipt type: {value.GetType()}");
        }
    }

    private static JsonSerializerOptions GetOptionsWithoutConverter(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        // Remove this converter to avoid infinite recursion
        for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (newOptions.Converters[i] is ExpoPushReceiptConverter)
            {
                newOptions.Converters.RemoveAt(i);
            }
        }
        return newOptions;
    }
}
