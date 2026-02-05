namespace ExpoPushNotifications.Models.Enums;

/// <summary>
/// Error codes returned by the Expo push notification service.
/// </summary>
public enum PushErrorCode
{
    /// <summary>
    /// The device cannot receive push notifications anymore and you should stop sending messages to the corresponding Expo push token.
    /// </summary>
    DeviceNotRegistered,

    /// <summary>
    /// The total notification payload was too large. On Android and iOS, the total payload must be at most 4096 bytes.
    /// </summary>
    MessageTooBig,

    /// <summary>
    /// You are sending messages too frequently to the given device. Implement exponential backoff and slowly retry sending messages.
    /// </summary>
    MessageRateExceeded,

    /// <summary>
    /// Your push notification credentials for your standalone app are invalid.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// An error occurred with the Expo push notification service.
    /// </summary>
    ExpoError,

    /// <summary>
    /// An error occurred with the underlying push notification provider (APNs or FCM).
    /// </summary>
    ProviderError,

    /// <summary>
    /// An error occurred due to incorrect usage of the SDK or API.
    /// </summary>
    DeveloperError
}
