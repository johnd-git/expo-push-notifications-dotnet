using System.Text.Json.Serialization;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Rich content configuration for push notifications.
/// </summary>
public sealed class ExpoPushRichContent
{
    /// <summary>
    /// URL of an image to display in the notification.
    /// The image will be downloaded and displayed as part of the notification content.
    /// </summary>
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Image { get; init; }
}
