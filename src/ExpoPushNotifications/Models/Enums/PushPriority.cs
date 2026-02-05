namespace ExpoPushNotifications.Models.Enums;

/// <summary>
/// Specifies the priority level for push notifications.
/// </summary>
public enum PushPriority
{
    /// <summary>
    /// Default priority. The notification may be delayed to conserve battery.
    /// </summary>
    Default,

    /// <summary>
    /// Normal priority. The notification may be delayed to conserve battery.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority. The notification is delivered immediately.
    /// </summary>
    High
}
