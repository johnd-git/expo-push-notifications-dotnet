using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpoPushNotifications.Internal.JsonConverters;

/// <summary>
/// JSON converter that serializes enums using kebab-case (e.g., TimeSensitive -> "time-sensitive").
/// </summary>
internal sealed class KebabCaseEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> EnumToString = [];
    private static readonly Dictionary<string, TEnum> StringToEnum = new(StringComparer.OrdinalIgnoreCase);

    static KebabCaseEnumConverter()
    {
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = value.ToString();
            var kebabCase = ToKebabCase(name);
            EnumToString[value] = kebabCase;
            StringToEnum[kebabCase] = value;
            StringToEnum[name] = value; // Also support PascalCase for reading
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (stringValue != null && StringToEnum.TryGetValue(stringValue, out var enumValue))
            {
                return enumValue;
            }
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(EnumToString[value]);
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    result.Append('-');
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }
}

/// <summary>
/// JSON converter for nullable enums using kebab-case.
/// </summary>
internal sealed class NullableKebabCaseEnumConverter<TEnum> : JsonConverter<TEnum?> where TEnum : struct, Enum
{
    private readonly KebabCaseEnumConverter<TEnum> _innerConverter = new();

    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        return _innerConverter.Read(ref reader, typeof(TEnum), options);
    }

    public override void Write(Utf8JsonWriter writer, TEnum? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            _innerConverter.Write(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
