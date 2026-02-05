namespace ExpoPushNotifications;

/// <summary>
/// Configuration options for the Expo push notification client.
/// </summary>
public sealed class ExpoClientOptions
{
    /// <summary>
    /// The Expo access token for authenticated requests.
    /// Required if push security is enabled in your Expo project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can create an access token in the Expo dashboard under Account Settings > Access Tokens.
    /// </para>
    /// <para>
    /// See <see href="https://docs.expo.dev/push-notifications/sending-notifications/#additional-security">
    /// Expo documentation</see> for more information about push security.
    /// </para>
    /// </remarks>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The maximum number of concurrent HTTP requests to the Expo API.
    /// Default is 6.
    /// </summary>
    /// <remarks>
    /// Higher values increase throughput but use more resources.
    /// Lower values reduce resource usage but may decrease throughput.
    /// </remarks>
    public int MaxConcurrentRequests { get; set; } = Constants.DefaultMaxConcurrentRequests;

    /// <summary>
    /// The minimum timeout between retry attempts when rate limited.
    /// Default is 1 second.
    /// </summary>
    /// <remarks>
    /// The SDK uses exponential backoff with a factor of 2, so retries will occur at:
    /// <list type="bullet">
    ///   <item>Attempt 1: <see cref="RetryMinTimeout"/></item>
    ///   <item>Attempt 2: <see cref="RetryMinTimeout"/> * 2</item>
    /// </list>
    /// </remarks>
    public TimeSpan RetryMinTimeout { get; set; } = TimeSpan.FromMilliseconds(Constants.DefaultRetryMinTimeoutMs);

    /// <summary>
    /// The maximum number of retry attempts for rate-limited requests.
    /// Default is 2 (total of 3 attempts including the initial request).
    /// </summary>
    public int MaxRetryAttempts { get; set; } = Constants.DefaultMaxRetryAttempts;

    /// <summary>
    /// The base URL for the Expo push notification service.
    /// Default is "https://exp.host".
    /// </summary>
    /// <remarks>
    /// This is primarily used for testing purposes.
    /// In production, you should use the default value.
    /// </remarks>
    public string BaseUrl { get; set; } = Constants.DefaultBaseUrl;
}
