using System.Text.Json.Serialization;
using ExpoPushNotifications.Internal.JsonConverters;
using ExpoPushNotifications.Models.Enums;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Details about a push notification error.
/// </summary>
public sealed class ExpoPushErrorDetails
{
    /// <summary>
    /// The error code indicating the type of error that occurred.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(NullableKebabCaseEnumConverter<PushErrorCode>))]
    public PushErrorCode? Error { get; init; }

    /// <summary>
    /// The Expo push token that caused the error.
    /// Only present for certain error types.
    /// </summary>
    [JsonPropertyName("expoPushToken")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExpoPushToken { get; init; }
}
