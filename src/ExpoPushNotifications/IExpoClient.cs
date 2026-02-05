using ExpoPushNotifications.Models;

namespace ExpoPushNotifications;

/// <summary>
/// Interface for the Expo push notification client.
/// </summary>
public interface IExpoClient
{
    /// <summary>
    /// Sends push notifications to the Expo push notification service.
    /// </summary>
    /// <param name="messages">The messages to send.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A list of tickets indicating whether each notification was successfully queued for delivery.
    /// The tickets are in the same order as the input messages.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method automatically handles chunking if the number of message recipients exceeds the API limit.
    /// </para>
    /// <para>
    /// A successful ticket (with <c>status: "ok"</c>) does not mean the notification was delivered;
    /// it means the notification was queued for delivery. Use <see cref="GetPushNotificationReceiptsAsync"/>
    /// to check delivery status.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ExpoPushTicket>> SendPushNotificationsAsync(
        IEnumerable<ExpoPushMessage> messages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves delivery receipts for previously sent notifications.
    /// </summary>
    /// <param name="receiptIds">The receipt IDs from successful push tickets.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A dictionary mapping receipt IDs to their delivery status.</returns>
    /// <remarks>
    /// <para>
    /// Receipts should be fetched after some delay (15+ minutes recommended) to allow time for delivery.
    /// </para>
    /// <para>
    /// This method automatically handles chunking if the number of receipt IDs exceeds the API limit.
    /// </para>
    /// </remarks>
    Task<IReadOnlyDictionary<string, ExpoPushReceipt>> GetPushNotificationReceiptsAsync(
        IEnumerable<string> receiptIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits a list of messages into chunks that don't exceed the API limit.
    /// </summary>
    /// <param name="messages">The messages to chunk.</param>
    /// <returns>An enumerable of message chunks.</returns>
    /// <remarks>
    /// <para>
    /// Each chunk contains at most <see cref="Expo.PushNotificationChunkSizeLimit"/> message slots.
    /// Messages with multiple recipients count as multiple slots.
    /// </para>
    /// </remarks>
    IEnumerable<IReadOnlyList<ExpoPushMessage>> ChunkPushNotifications(
        IEnumerable<ExpoPushMessage> messages);

    /// <summary>
    /// Splits a list of receipt IDs into chunks that don't exceed the API limit.
    /// </summary>
    /// <param name="receiptIds">The receipt IDs to chunk.</param>
    /// <returns>An enumerable of receipt ID chunks.</returns>
    IEnumerable<IReadOnlyList<string>> ChunkPushNotificationReceiptIds(
        IEnumerable<string> receiptIds);
}
