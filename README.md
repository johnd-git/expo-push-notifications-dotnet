# expo-push-notifications-dotnet

[![CI](https://github.com/johnd-git/expo-push-notifications-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/johnd-git/expo-push-notifications-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ExpoPushNotifications.svg)](https://www.nuget.org/packages/ExpoPushNotifications)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Server-side library for sending Expo push notifications from .NET applications.

## Installation

```bash
dotnet add package ExpoPushNotifications
```

## Quick Start

### Basic Usage

```csharp
using ExpoPushNotifications;
using ExpoPushNotifications.Models;

// Create a client
var expo = new Expo(new ExpoClientOptions
{
    AccessToken = Environment.GetEnvironmentVariable("EXPO_ACCESS_TOKEN")
});

// Build messages
var messages = new List<ExpoPushMessage>();

foreach (var pushToken in somePushTokens)
{
    // Validate the token
    if (!Expo.IsExpoPushToken(pushToken))
    {
        Console.WriteLine($"Invalid push token: {pushToken}");
        continue;
    }

    messages.Add(new ExpoPushMessage
    {
        To = [pushToken],
        Title = "Hello",
        Body = "World",
        Sound = "default",
        Data = new Dictionary<string, object> { ["key"] = "value" }
    });
}

// Send notifications in chunks
foreach (var chunk in expo.ChunkPushNotifications(messages))
{
    try
    {
        var tickets = await expo.SendPushNotificationsAsync(chunk);

        foreach (var ticket in tickets)
        {
            if (ticket is ExpoPushErrorTicket errorTicket)
            {
                Console.WriteLine($"Error: {errorTicket.Message}");
            }
        }
    }
    catch (ExpoApiException ex)
    {
        Console.WriteLine($"API error: {ex.Message}");
    }
}
```

### With Dependency Injection (ASP.NET Core)

```csharp
// In Program.cs
builder.Services.AddExpoClient(options =>
{
    options.AccessToken = builder.Configuration["Expo:AccessToken"];
    options.MaxConcurrentRequests = 6;
});

// In your service
public class NotificationService
{
    private readonly IExpoClient _expo;

    public NotificationService(IExpoClient expo)
    {
        _expo = expo;
    }

    public async Task SendNotificationAsync(string token, string title, string body)
    {
        var messages = new[]
        {
            new ExpoPushMessage
            {
                To = [token],
                Title = title,
                Body = body,
                Sound = "default"
            }
        };

        var tickets = await _expo.SendPushNotificationsAsync(messages);
        // Process tickets...
    }
}
```

### Checking Delivery Receipts

```csharp
// Collect receipt IDs from successful tickets
var receiptIds = tickets
    .OfType<ExpoPushSuccessTicket>()
    .Select(t => t.Id)
    .ToList();

// Wait some time for delivery (15+ minutes recommended)
await Task.Delay(TimeSpan.FromMinutes(15));

// Retrieve receipts
foreach (var chunk in expo.ChunkPushNotificationReceiptIds(receiptIds))
{
    var receipts = await expo.GetPushNotificationReceiptsAsync(chunk);

    foreach (var (id, receipt) in receipts)
    {
        if (receipt is ExpoPushErrorReceipt errorReceipt)
        {
            Console.WriteLine($"Failed to deliver {id}: {errorReceipt.Message}");

            if (errorReceipt.Details?.Error == PushErrorCode.DeviceNotRegistered)
            {
                // Remove the token from your database
            }
        }
    }
}
```

## API Reference

### Expo Class

| Method | Description |
|--------|-------------|
| `SendPushNotificationsAsync(messages)` | Sends push notifications and returns tickets |
| `GetPushNotificationReceiptsAsync(receiptIds)` | Retrieves delivery receipts for sent notifications |
| `ChunkPushNotifications(messages)` | Splits messages into chunks of 100 recipients max |
| `ChunkPushNotificationReceiptIds(receiptIds)` | Splits receipt IDs into chunks of 300 max |
| `IsExpoPushToken(token)` | Validates Expo push token format (static) |

### Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `AccessToken` | `null` | Expo access token for authenticated requests |
| `MaxConcurrentRequests` | `6` | Maximum concurrent HTTP requests |
| `RetryMinTimeout` | `1 second` | Minimum timeout between retries |
| `MaxRetryAttempts` | `2` | Maximum retry attempts for rate-limited requests |
| `BaseUrl` | `https://exp.host` | API base URL (for testing) |

### Push Message Properties

| Property | Type | Description |
|----------|------|-------------|
| `To` | `IReadOnlyList<string>` | Recipient token(s) - required |
| `Title` | `string?` | Notification title |
| `Body` | `string?` | Notification body text |
| `Sound` | `object?` | Sound configuration ("default" or ExpoPushSound object) |
| `Data` | `IReadOnlyDictionary<string, object>?` | Custom data payload |
| `Ttl` | `int?` | Time to live in seconds |
| `Priority` | `PushPriority?` | Delivery priority (default, normal, high) |
| `Badge` | `int?` | Badge number to display |
| `ChannelId` | `string?` | Android notification channel ID |
| `InterruptionLevel` | `InterruptionLevel?` | iOS interruption level |
| `RichContent` | `ExpoPushRichContent?` | Rich notification content (images) |

### Error Codes

| Code | Description |
|------|-------------|
| `DeviceNotRegistered` | The device can no longer receive notifications |
| `MessageTooBig` | Payload exceeds 4096 bytes |
| `MessageRateExceeded` | Too many messages sent to this device |
| `InvalidCredentials` | Push credentials are invalid |
| `ExpoError` | Expo service error |
| `ProviderError` | APNs or FCM provider error |
| `DeveloperError` | SDK/API usage error |

## Error Handling

The SDK automatically retries rate-limited requests (HTTP 429) with exponential backoff.

```csharp
try
{
    var tickets = await expo.SendPushNotificationsAsync(messages);
}
catch (ExpoApiException ex) when (ex.IsRateLimitError)
{
    // All retries exhausted
    Console.WriteLine("Rate limited after retries");
}
catch (ExpoApiException ex) when (ex.IsAuthenticationError)
{
    // Invalid access token
    Console.WriteLine("Authentication failed");
}
catch (ExpoApiException ex)
{
    Console.WriteLine($"API error: {ex.Message}, Code: {ex.ErrorCode}");
}
```

## Requirements

- .NET 8.0, 9.0, or 10.0

## Related Projects

- [expo-server-sdk-node](https://github.com/expo/expo-server-sdk-node) - Node.js SDK (official)
- [Expo Documentation](https://docs.expo.dev/push-notifications/sending-notifications/)

## License

MIT
