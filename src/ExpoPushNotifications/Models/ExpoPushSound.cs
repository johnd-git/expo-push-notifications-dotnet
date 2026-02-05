using System.Text.Json.Serialization;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Configuration for the sound played when a notification is received.
/// </summary>
public sealed class ExpoPushSound
{
    /// <summary>
    /// Specifies whether the notification sound should be played as a critical alert (iOS only).
    /// Critical alerts ignore the mute switch and Do Not Disturb settings.
    /// Requires special entitlement from Apple.
    /// </summary>
    [JsonPropertyName("critical")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Critical { get; init; }

    /// <summary>
    /// The name of the sound file to play.
    /// Use "default" for the default notification sound, or specify a custom sound file name.
    /// Set to null to play no sound.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// The volume for the critical alert's sound. Must be a value between 0.0 (silent) and 1.0 (full volume).
    /// Only applicable when <see cref="Critical"/> is true.
    /// </summary>
    [JsonPropertyName("volume")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Volume { get; init; }

    /// <summary>
    /// Creates a sound configuration for the default notification sound.
    /// </summary>
    public static ExpoPushSound Default => new() { Name = "default" };
}
