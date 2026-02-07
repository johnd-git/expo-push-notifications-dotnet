namespace ExpoPushNotifications;

/// <summary>
/// Constants used by the Expo push notification SDK.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// The default base URL for the Expo push notification service.
    /// </summary>
    public const string DefaultBaseUrl = "https://exp.host";

    /// <summary>
    /// The API endpoint for sending push notifications.
    /// </summary>
    public const string SendPushNotificationsPath = "/--/api/v2/push/send";

    /// <summary>
    /// The API endpoint for retrieving push notification receipts.
    /// </summary>
    public const string GetPushNotificationReceiptsPath = "/--/api/v2/push/getReceipts";

    /// <summary>
    /// The maximum number of push notification message slots per request.
    /// Messages with multiple recipients count as multiple slots.
    /// </summary>
    public const int PushNotificationChunkLimit = 100;

    /// <summary>
    /// The maximum number of receipt IDs per request.
    /// </summary>
    public const int PushNotificationReceiptChunkLimit = 300;

    /// <summary>
    /// The default maximum number of concurrent HTTP requests.
    /// </summary>
    public const int DefaultMaxConcurrentRequests = 6;

    /// <summary>
    /// The default minimum timeout between retries (in milliseconds).
    /// </summary>
    public const int DefaultRetryMinTimeoutMs = 1000;

    /// <summary>
    /// The default number of retry attempts for rate-limited requests.
    /// </summary>
    public const int DefaultMaxRetryAttempts = 2;

    /// <summary>
    /// The default timeout for each HTTP request attempt (in seconds).
    /// </summary>
    public const int DefaultAttemptTimeoutSeconds = 10;

    /// <summary>
    /// The default total timeout for an HTTP request including retries (in seconds).
    /// </summary>
    public const int DefaultTotalRequestTimeoutSeconds = 100;

    /// <summary>
    /// The minimum payload size (in bytes) before compression is applied.
    /// </summary>
    public const int CompressionThresholdBytes = 1024;

    /// <summary>
    /// The User-Agent header value format for SDK requests.
    /// </summary>
    public const string UserAgentFormat = "expo-push-notifications-dotnet/{0}";
}
