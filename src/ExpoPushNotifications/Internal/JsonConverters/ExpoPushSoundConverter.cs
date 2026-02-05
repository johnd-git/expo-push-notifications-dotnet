using System.Text.Json;
using System.Text.Json.Serialization;
using ExpoPushNotifications.Models;

namespace ExpoPushNotifications.Internal.JsonConverters;

/// <summary>
/// JSON converter that handles the "sound" property which can be either a string (e.g., "default")
/// or an <see cref="ExpoPushSound"/> object.
/// </summary>
internal sealed class ExpoPushSoundConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;

            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.StartObject:
                return JsonSerializer.Deserialize<ExpoPushSound>(ref reader, options);

            default:
                throw new JsonException($"Unexpected token type for sound: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;

            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;

            case ExpoPushSound soundObject:
                JsonSerializer.Serialize(writer, soundObject, options);
                break;

            default:
                throw new JsonException($"Unexpected sound type: {value.GetType()}");
        }
    }
}
