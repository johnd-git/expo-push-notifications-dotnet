using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExpoPushNotifications.Exceptions;
using ExpoPushNotifications.Models;
using Microsoft.Extensions.Options;

namespace ExpoPushNotifications;

/// <summary>
/// Client for sending push notifications via the Expo push notification service.
/// </summary>
/// <remarks>
/// <para>
/// This class is thread-safe and designed to be used as a singleton.
/// For best results in ASP.NET Core, register it using the AddExpoClient extension method.
/// </para>
/// <para>
/// See <see href="https://docs.expo.dev/push-notifications/sending-notifications/">Expo documentation</see>
/// for more information about push notifications.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var expo = new Expo(new ExpoClientOptions { AccessToken = "your-token" });
/// var tickets = await expo.SendPushNotificationsAsync(messages);
/// </code>
/// </example>
public sealed partial class Expo : IExpoClient, IDisposable
{
    /// <summary>
    /// The maximum number of push notification message slots per request.
    /// </summary>
    public static int PushNotificationChunkSizeLimit => Constants.PushNotificationChunkLimit;

    /// <summary>
    /// The maximum number of receipt IDs per request.
    /// </summary>
    public static int PushNotificationReceiptChunkSizeLimit => Constants.PushNotificationReceiptChunkLimit;

    private static readonly string SdkVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly ExpoClientOptions _options;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Expo"/> class with default options.
    /// </summary>
    public Expo() : this(new ExpoClientOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expo"/> class with the specified options.
    /// </summary>
    /// <param name="options">The client options.</param>
    public Expo(ExpoClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = CreateHttpClient(options);
        _concurrencyLimiter = new SemaphoreSlim(options.MaxConcurrentRequests);
        _ownsHttpClient = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Expo"/> class with an injected HttpClient.
    /// Used for dependency injection scenarios.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="options">The client options.</param>
    public Expo(HttpClient httpClient, IOptions<ExpoClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _concurrencyLimiter = new SemaphoreSlim(_options.MaxConcurrentRequests);
        _ownsHttpClient = false;

        ConfigureHttpClient(_httpClient, _options);
    }

    /// <summary>
    /// Validates whether the specified token is a valid Expo push token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>
    /// <c>true</c> if the token is a valid Expo push token (ExponentPushToken[...],
    /// ExpoPushToken[...], or UUID format); otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method validates the format of the token but does not verify
    /// that it is registered with Expo's push notification service.
    /// </remarks>
    public static bool IsExpoPushToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // Check for ExponentPushToken[...] or ExpoPushToken[...] format
        if ((token.StartsWith("ExponentPushToken[", StringComparison.Ordinal) ||
             token.StartsWith("ExpoPushToken[", StringComparison.Ordinal)) &&
            token.EndsWith(']'))
        {
            return true;
        }

        // Check for UUID format (used by some native implementations)
        return UuidRegex().IsMatch(token);
    }

    [GeneratedRegex(@"^[a-f\d]{8}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{4}-[a-f\d]{12}$", RegexOptions.IgnoreCase)]
    private static partial Regex UuidRegex();

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpoPushTicket>> SendPushNotificationsAsync(
        IEnumerable<ExpoPushMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
        {
            return [];
        }

        var allTickets = new List<ExpoPushTicket>();

        foreach (var chunk in ChunkPushNotifications(messageList))
        {
            var tickets = await SendChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
            allTickets.AddRange(tickets);
        }

        return allTickets;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, ExpoPushReceipt>> GetPushNotificationReceiptsAsync(
        IEnumerable<string> receiptIds,
        CancellationToken cancellationToken = default)
    {
        var receiptIdList = receiptIds.ToList();
        if (receiptIdList.Count == 0)
        {
            return new Dictionary<string, ExpoPushReceipt>();
        }

        var allReceipts = new Dictionary<string, ExpoPushReceipt>();

        foreach (var chunk in ChunkPushNotificationReceiptIds(receiptIdList))
        {
            var receipts = await GetReceiptsChunkAsync(chunk, cancellationToken).ConfigureAwait(false);
            foreach (var kvp in receipts)
            {
                allReceipts[kvp.Key] = kvp.Value;
            }
        }

        return allReceipts;
    }

    /// <inheritdoc />
    public IEnumerable<IReadOnlyList<ExpoPushMessage>> ChunkPushNotifications(
        IEnumerable<ExpoPushMessage> messages)
    {
        var currentChunk = new List<ExpoPushMessage>();
        int currentSlotCount = 0;

        foreach (var message in messages)
        {
            int messageSlots = message.To.Count;

            // Skip messages with no recipients
            if (messageSlots == 0)
            {
                continue;
            }

            // If this message alone exceeds the limit, split it
            if (messageSlots > PushNotificationChunkSizeLimit)
            {
                // Flush current chunk first
                if (currentChunk.Count > 0)
                {
                    yield return currentChunk;
                    currentChunk = [];
                    currentSlotCount = 0;
                }

                // Split the message's recipients into multiple messages
                foreach (var recipientChunk in ChunkList(message.To, PushNotificationChunkSizeLimit))
                {
                    yield return
                    [
                        new ExpoPushMessage
                        {
                            To = recipientChunk,
                            Data = message.Data,
                            Title = message.Title,
                            Subtitle = message.Subtitle,
                            Body = message.Body,
                            Sound = message.Sound,
                            Ttl = message.Ttl,
                            Expiration = message.Expiration,
                            Priority = message.Priority,
                            InterruptionLevel = message.InterruptionLevel,
                            Badge = message.Badge,
                            ChannelId = message.ChannelId,
                            Icon = message.Icon,
                            RichContent = message.RichContent,
                            CategoryId = message.CategoryId,
                            MutableContent = message.MutableContent
                        }
                    ];
                }
                continue;
            }

            // If adding this message would exceed the limit, flush current chunk
            if (currentSlotCount + messageSlots > PushNotificationChunkSizeLimit)
            {
                yield return currentChunk;
                currentChunk = [];
                currentSlotCount = 0;
            }

            currentChunk.Add(message);
            currentSlotCount += messageSlots;
        }

        // Yield final chunk if not empty
        if (currentChunk.Count > 0)
        {
            yield return currentChunk;
        }
    }

    /// <inheritdoc />
    public IEnumerable<IReadOnlyList<string>> ChunkPushNotificationReceiptIds(
        IEnumerable<string> receiptIds)
    {
        return ChunkList(receiptIds.ToList(), PushNotificationReceiptChunkSizeLimit);
    }

    private static IEnumerable<IReadOnlyList<T>> ChunkList<T>(IReadOnlyList<T> list, int chunkSize)
    {
        for (int i = 0; i < list.Count; i += chunkSize)
        {
            int count = Math.Min(chunkSize, list.Count - i);
            yield return list.Skip(i).Take(count).ToList();
        }
    }

    private async Task<IReadOnlyList<ExpoPushTicket>> SendChunkAsync(
        IReadOnlyList<ExpoPushMessage> messages,
        CancellationToken cancellationToken)
    {
        await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = new Uri(_options.BaseUrl + Constants.SendPushNotificationsPath);
                var json = JsonSerializer.Serialize(messages, JsonOptions);

                using var request = CreateRequest(HttpMethod.Post, url, json);
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                HandleErrorResponse(response, responseText);

                var result = JsonSerializer.Deserialize<ApiResponse<List<ExpoPushTicket>>>(responseText, JsonOptions)
                    ?? throw new ExpoApiException("Failed to parse API response", response.StatusCode, responseText: responseText);

                if (result.Data == null)
                {
                    throw new ExpoApiException("API response missing data field", response.StatusCode, responseText: responseText);
                }

                // Validate ticket count matches expected message count
                int expectedCount = GetActualMessageCount(messages);
                if (result.Data.Count != expectedCount)
                {
                    throw new ExpoApiException(
                        $"Expected Expo to respond with {expectedCount} tickets but got {result.Data.Count}",
                        response.StatusCode,
                        responseText: responseText);
                }

                return result.Data;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, ExpoPushReceipt>> GetReceiptsChunkAsync(
        IReadOnlyList<string> receiptIds,
        CancellationToken cancellationToken)
    {
        await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                var url = new Uri(_options.BaseUrl + Constants.GetPushNotificationReceiptsPath);
                var requestBody = new { ids = receiptIds };
                var json = JsonSerializer.Serialize(requestBody, JsonOptions);

                using var request = CreateRequest(HttpMethod.Post, url, json);
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                HandleErrorResponse(response, responseText);

                var result = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, ExpoPushReceipt>>>(responseText, JsonOptions)
                    ?? throw new ExpoApiException("Failed to parse API response", response.StatusCode, responseText: responseText);

                if (result.Data == null)
                {
                    throw new ExpoApiException(
                        "Expected Expo to respond with a map from receipt IDs to receipts but received data of another type",
                        response.StatusCode,
                        responseText: responseText);
                }

                return result.Data;
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        int attempt = 0;
        while (true)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (ExpoApiException ex) when (ex.IsRateLimitError && attempt < _options.MaxRetryAttempts)
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(_options.RetryMinTimeout.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri url, string json)
    {
        var request = new HttpRequestMessage(method, url);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        if (jsonBytes.Length > Constants.CompressionThresholdBytes)
        {
            // Compress the payload
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            var compressedBytes = outputStream.ToArray();
            request.Content = new ByteArrayContent(compressedBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content.Headers.ContentEncoding.Add("gzip");
        }
        else
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static void HandleErrorResponse(HttpResponseMessage response, string responseText)
    {
        if (response.IsSuccessStatusCode)
        {
            // Check for errors in the response body (Expo returns 200 with errors in body)
            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseText, JsonOptions);
                if (result?.Errors != null && result.Errors.Count > 0)
                {
                    var primaryError = result.Errors[0];
                    var otherErrors = result.Errors.Count > 1
                        ? result.Errors.Skip(1).Select(e => new ExpoApiException(e.Message, response.StatusCode, e.Code)).ToList()
                        : null;

                    throw new ExpoApiException(
                        primaryError.Message,
                        response.StatusCode,
                        primaryError.Code,
                        primaryError.Details,
                        responseText,
                        otherErrors);
                }
            }
            catch (JsonException)
            {
                throw new ExpoApiException(
                    "Expo responded with malformed JSON",
                    response.StatusCode,
                    responseText: responseText);
            }

            return;
        }

        // Handle non-success status codes
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ExpoApiException("Rate limited by Expo API", HttpStatusCode.TooManyRequests, responseText: responseText);
        }

        // Try to parse error from response body
        try
        {
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseText, JsonOptions);
            if (result?.Errors != null && result.Errors.Count > 0)
            {
                var primaryError = result.Errors[0];
                throw new ExpoApiException(
                    primaryError.Message,
                    response.StatusCode,
                    primaryError.Code,
                    primaryError.Details,
                    responseText);
            }
        }
        catch (JsonException)
        {
            // Response is not JSON
        }

        throw new ExpoApiException(
            $"Expo API returned status code {(int)response.StatusCode}",
            response.StatusCode,
            responseText: responseText);
    }

    private static int GetActualMessageCount(IEnumerable<ExpoPushMessage> messages)
    {
        return messages.Sum(m => m.To.Count);
    }

    private static HttpClient CreateHttpClient(ExpoClientOptions options)
    {
        var client = new HttpClient();
        ConfigureHttpClient(client, options);
        return client;
    }

    private static void ConfigureHttpClient(HttpClient client, ExpoClientOptions options)
    {
        client.BaseAddress ??= new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"expo-push-notifications-dotnet/{SdkVersion}");

        if (!string.IsNullOrEmpty(options.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        }
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        _concurrencyLimiter.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Internal API response structure.
    /// </summary>
    private sealed class ApiResponse<T>
    {
        public T? Data { get; set; }
        public List<ApiError>? Errors { get; set; }
    }

    /// <summary>
    /// Internal API error structure.
    /// </summary>
    private sealed class ApiError
    {
        public required string Message { get; set; }
        public string? Code { get; set; }
        public object? Details { get; set; }
    }
}
