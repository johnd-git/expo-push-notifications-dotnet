using System.Text.Json.Serialization;
using ExpoPushNotifications.Internal.JsonConverters;
using ExpoPushNotifications.Models.Enums;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Represents a push notification message to be sent via the Expo push notification service.
/// </summary>
/// <remarks>
/// <para>
/// See <see href="https://docs.expo.dev/push-notifications/sending-notifications/#message-request-format">
/// Expo documentation</see> for more information about message format.
/// </para>
/// </remarks>
public sealed class ExpoPushMessage
{
    /// <summary>
    /// The recipient(s) of the push notification.
    /// Can be a single Expo push token or multiple tokens to send the same message to multiple devices.
    /// </summary>
    [JsonPropertyName("to")]
    [JsonConverter(typeof(ExpoPushTokenCollectionConverter))]
    public required IReadOnlyList<string> To { get; init; }

    /// <summary>
    /// A JSON object delivered to your app. May be up to about 4 KiB.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// The title to display in the notification. Often displayed above the notification body.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    /// <summary>
    /// The subtitle to display in the notification. iOS only.
    /// </summary>
    [JsonPropertyName("subtitle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subtitle { get; init; }

    /// <summary>
    /// The message to display in the notification.
    /// </summary>
    [JsonPropertyName("body")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Body { get; init; }

    /// <summary>
    /// A sound to play when the recipient receives this notification.
    /// Specify "default" to play the device's default notification sound,
    /// or use an <see cref="ExpoPushSound"/> object for more control.
    /// Set to null for no sound.
    /// </summary>
    [JsonPropertyName("sound")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(ExpoPushSoundConverter))]
    public object? Sound { get; init; }

    /// <summary>
    /// Time to live for this notification in seconds.
    /// If the notification is not delivered within this time, it will be discarded.
    /// Default expiration time is 2 weeks (1209600 seconds).
    /// </summary>
    [JsonPropertyName("ttl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Ttl { get; init; }

    /// <summary>
    /// Unix timestamp for when this notification expires.
    /// If both <see cref="Ttl"/> and <see cref="Expiration"/> are specified, <see cref="Expiration"/> takes precedence.
    /// </summary>
    [JsonPropertyName("expiration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Expiration { get; init; }

    /// <summary>
    /// The delivery priority of the notification.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(NullableKebabCaseEnumConverter<PushPriority>))]
    public PushPriority? Priority { get; init; }

    /// <summary>
    /// The interruption level of the notification (iOS 15+ only).
    /// </summary>
    [JsonPropertyName("interruptionLevel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(NullableKebabCaseEnumConverter<InterruptionLevel>))]
    public InterruptionLevel? InterruptionLevel { get; init; }

    /// <summary>
    /// The number to display in the badge on the app icon.
    /// Specify 0 to clear the badge.
    /// </summary>
    [JsonPropertyName("badge")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Badge { get; init; }

    /// <summary>
    /// ID of the notification channel through which this notification is displayed (Android only).
    /// </summary>
    [JsonPropertyName("channelId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChannelId { get; init; }

    /// <summary>
    /// URL of the icon to display in the notification (Android only).
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    /// <summary>
    /// Rich content to include in the notification, such as images.
    /// </summary>
    [JsonPropertyName("richContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExpoPushRichContent? RichContent { get; init; }

    /// <summary>
    /// ID of the notification category that this notification is associated with.
    /// Must be on a category that you have previously defined with the Notifications API.
    /// </summary>
    [JsonPropertyName("categoryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CategoryId { get; init; }

    /// <summary>
    /// Specifies whether this notification can be intercepted by a Notification Service extension (iOS only).
    /// </summary>
    [JsonPropertyName("mutableContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MutableContent { get; init; }

    /// <summary>
    /// Creates a new push message with a single recipient.
    /// </summary>
    /// <param name="to">The Expo push token of the recipient.</param>
    /// <returns>A new <see cref="ExpoPushMessage"/> instance.</returns>
    public static ExpoPushMessage Create(string to) => new() { To = [to] };

    /// <summary>
    /// Creates a new push message with multiple recipients.
    /// </summary>
    /// <param name="to">The Expo push tokens of the recipients.</param>
    /// <returns>A new <see cref="ExpoPushMessage"/> instance.</returns>
    public static ExpoPushMessage Create(IEnumerable<string> to) => new() { To = to.ToList() };
}
