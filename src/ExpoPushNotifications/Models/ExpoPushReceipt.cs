using System.Text.Json.Serialization;
using ExpoPushNotifications.Internal.JsonConverters;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Represents a delivery receipt for a push notification.
/// Receipts indicate the final delivery status of a notification after it has been processed.
/// </summary>
/// <remarks>
/// <para>
/// Receipts should be fetched after some delay (15+ minutes recommended) to allow time for delivery.
/// </para>
/// <para>
/// A receipt with <c>status: "ok"</c> means the notification was successfully delivered to the push provider (APNs/FCM).
/// </para>
/// <para>
/// A receipt with <c>status: "error"</c> indicates a delivery failure.
/// Check <see cref="ExpoPushErrorReceipt.Details"/> for the specific error code and take appropriate action.
/// </para>
/// </remarks>
[JsonConverter(typeof(ExpoPushReceiptConverter))]
public abstract record ExpoPushReceipt
{
    /// <summary>
    /// The status of the receipt. Either "ok" or "error".
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Returns true if the receipt indicates successful delivery.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Status == "ok";

    /// <summary>
    /// Returns true if the receipt indicates a delivery error.
    /// </summary>
    [JsonIgnore]
    public bool IsError => Status == "error";
}

/// <summary>
/// Represents a successful delivery receipt.
/// The notification was delivered to the push provider (APNs/FCM).
/// </summary>
public sealed record ExpoPushSuccessReceipt : ExpoPushReceipt
{
    /// <summary>
    /// Optional details about the successful delivery.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonExtensionData]
    public IDictionary<string, object>? Details { get; init; }
}

/// <summary>
/// Represents a failed delivery receipt.
/// The notification could not be delivered to the push provider.
/// </summary>
public sealed record ExpoPushErrorReceipt : ExpoPushReceipt
{
    /// <summary>
    /// A human-readable error message describing what went wrong.
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Additional details about the error, including the error code.
    /// </summary>
    [JsonPropertyName("details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExpoPushErrorDetails? Details { get; init; }
}
