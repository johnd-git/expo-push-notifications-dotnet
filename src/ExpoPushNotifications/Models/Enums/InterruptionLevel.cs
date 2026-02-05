namespace ExpoPushNotifications.Models.Enums;

/// <summary>
/// Specifies the interruption level for iOS notifications (iOS 15+).
/// </summary>
public enum InterruptionLevel
{
    /// <summary>
    /// The system adds the notification to the notification list without lighting up the screen or playing a sound.
    /// </summary>
    Passive,

    /// <summary>
    /// The system presents the notification immediately, lights up the screen, and can play a sound.
    /// </summary>
    Active,

    /// <summary>
    /// The system presents the notification immediately, lights up the screen, and can play a sound,
    /// but won't break through system notification controls.
    /// </summary>
    TimeSensitive,

    /// <summary>
    /// The system presents the notification immediately, lights up the screen, and bypasses the mute switch to play a sound.
    /// </summary>
    Critical
}
