using System.Text.Json.Serialization;
using ExpoPushNotifications.Internal.JsonConverters;

namespace ExpoPushNotifications.Models;

/// <summary>
/// Represents the response from the Expo push notification service for a single notification.
/// This is returned immediately after sending a notification and indicates whether the message
/// was successfully queued for delivery.
/// </summary>
/// <remarks>
/// <para>
/// A ticket with <c>status: "ok"</c> means the notification was successfully queued for delivery.
/// Use the <see cref="ExpoPushSuccessTicket.Id"/> to later retrieve the delivery receipt.
/// </para>
/// <para>
/// A ticket with <c>status: "error"</c> indicates an immediate error (e.g., invalid token format).
/// Check <see cref="ExpoPushErrorTicket.Details"/> for the specific error code.
/// </para>
/// </remarks>
[JsonConverter(typeof(ExpoPushTicketConverter))]
public abstract record ExpoPushTicket
{
    /// <summary>
    /// The status of the ticket. Either "ok" or "error".
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// Returns true if the ticket indicates success.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Status == "ok";

    /// <summary>
    /// Returns true if the ticket indicates an error.
    /// </summary>
    [JsonIgnore]
    public bool IsError => Status == "error";
}

/// <summary>
/// Represents a successful push notification ticket.
/// The notification was successfully queued for delivery.
/// </summary>
public sealed record ExpoPushSuccessTicket : ExpoPushTicket
{
    /// <summary>
    /// The receipt ID that can be used to retrieve the delivery status later.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

/// <summary>
/// Represents a failed push notification ticket.
/// The notification could not be queued for delivery.
/// </summary>
public sealed record ExpoPushErrorTicket : ExpoPushTicket
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
