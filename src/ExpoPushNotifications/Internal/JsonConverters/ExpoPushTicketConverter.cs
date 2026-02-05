using System.Text.Json;
using System.Text.Json.Serialization;
using ExpoPushNotifications.Models;

namespace ExpoPushNotifications.Internal.JsonConverters;

/// <summary>
/// JSON converter for <see cref="ExpoPushTicket"/> that handles polymorphic deserialization
/// based on the "status" property.
/// </summary>
internal sealed class ExpoPushTicketConverter : JsonConverter<ExpoPushTicket>
{
    public override ExpoPushTicket? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (!root.TryGetProperty("status", out var statusElement))
        {
            throw new JsonException("ExpoPushTicket must have a 'status' property.");
        }

        var status = statusElement.GetString();
        var json = root.GetRawText();

        return status switch
        {
            "ok" => JsonSerializer.Deserialize<ExpoPushSuccessTicket>(json, GetOptionsWithoutConverter(options)),
            "error" => JsonSerializer.Deserialize<ExpoPushErrorTicket>(json, GetOptionsWithoutConverter(options)),
            _ => throw new JsonException($"Unknown status value: {status}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ExpoPushTicket value, JsonSerializerOptions options)
    {
        var optionsWithoutConverter = GetOptionsWithoutConverter(options);

        switch (value)
        {
            case ExpoPushSuccessTicket successTicket:
                JsonSerializer.Serialize(writer, successTicket, optionsWithoutConverter);
                break;
            case ExpoPushErrorTicket errorTicket:
                JsonSerializer.Serialize(writer, errorTicket, optionsWithoutConverter);
                break;
            default:
                throw new JsonException($"Unknown ticket type: {value.GetType()}");
        }
    }

    private static JsonSerializerOptions GetOptionsWithoutConverter(JsonSerializerOptions options)
    {
        var newOptions = new JsonSerializerOptions(options);
        // Remove this converter to avoid infinite recursion
        for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (newOptions.Converters[i] is ExpoPushTicketConverter)
            {
                newOptions.Converters.RemoveAt(i);
            }
        }
        return newOptions;
    }
}
