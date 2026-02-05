using ExpoPushNotifications;
using ExpoPushNotifications.Models;
using FluentAssertions;
using Xunit;

namespace ExpoPushNotifications.Tests;

public class ChunkPushNotificationsTests
{
    private readonly Expo _client = new();

    [Fact]
    public void ChunkPushNotifications_EmptyList_ReturnsEmpty()
    {
        var messages = Array.Empty<ExpoPushMessage>();
        var chunks = _client.ChunkPushNotifications(messages).ToList();
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void ChunkPushNotifications_SingleMessage_ReturnsSingleChunk()
    {
        var messages = new[] { ExpoPushMessage.Create("token1") };
        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(1);
    }

    [Fact]
    public void ChunkPushNotifications_10Messages_ReturnsSingleChunk()
    {
        var messages = Enumerable.Range(0, 10)
            .Select(i => ExpoPushMessage.Create($"token{i}"))
            .ToList();

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(10);
    }

    [Fact]
    public void ChunkPushNotifications_999Messages_Returns10Chunks()
    {
        var messages = Enumerable.Range(0, 999)
            .Select(i => ExpoPushMessage.Create($"token{i}"))
            .ToList();

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(10);
        chunks.Take(9).Should().AllSatisfy(c => c.Should().HaveCount(100));
        chunks.Last().Should().HaveCount(99);
        chunks.Sum(c => c.Count).Should().Be(999);
    }

    [Fact]
    public void ChunkPushNotifications_Exactly100Messages_ReturnsSingleChunk()
    {
        var messages = Enumerable.Range(0, 100)
            .Select(i => ExpoPushMessage.Create($"token{i}"))
            .ToList();

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(100);
    }

    [Fact]
    public void ChunkPushNotifications_101Messages_Returns2Chunks()
    {
        var messages = Enumerable.Range(0, 101)
            .Select(i => ExpoPushMessage.Create($"token{i}"))
            .ToList();

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(100);
        chunks[1].Should().HaveCount(1);
    }

    [Fact]
    public void ChunkPushNotifications_MessageWithMultipleRecipients_CountsCorrectly()
    {
        var messages = new[]
        {
            new ExpoPushMessage { To = Enumerable.Range(0, 50).Select(i => $"token{i}").ToList() },
            new ExpoPushMessage { To = Enumerable.Range(50, 50).Select(i => $"token{i}").ToList() }
        };

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(2);
    }

    [Fact]
    public void ChunkPushNotifications_MessageWith101Recipients_SplitsMessage()
    {
        var messages = new[]
        {
            new ExpoPushMessage
            {
                To = Enumerable.Range(0, 101).Select(i => $"token{i}").ToList(),
                Body = "Test message"
            }
        };

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(1);
        chunks[0][0].To.Should().HaveCount(100);
        chunks[1].Should().HaveCount(1);
        chunks[1][0].To.Should().HaveCount(1);

        // Verify message properties are preserved
        chunks[0][0].Body.Should().Be("Test message");
        chunks[1][0].Body.Should().Be("Test message");
    }

    [Fact]
    public void ChunkPushNotifications_MixedSingleAndMultipleRecipients_ChunksCorrectly()
    {
        var messages = new[]
        {
            ExpoPushMessage.Create("token1"),
            new ExpoPushMessage { To = ["token2", "token3", "token4"] },
            ExpoPushMessage.Create("token5")
        };

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(3);
    }

    [Fact]
    public void ChunkPushNotifications_MessageWithEmptyRecipients_SkipsMessage()
    {
        var messages = new[]
        {
            ExpoPushMessage.Create("token1"),
            new ExpoPushMessage { To = [] },
            ExpoPushMessage.Create("token2")
        };

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(2);
    }

    [Fact]
    public void ChunkPushNotifications_99ThenMessageWith2Recipients_SplitsCorrectly()
    {
        var messages = Enumerable.Range(0, 99)
            .Select(i => ExpoPushMessage.Create($"token{i}"))
            .ToList();

        messages.Add(new ExpoPushMessage { To = ["tokenA", "tokenB"] });

        var chunks = _client.ChunkPushNotifications(messages).ToList();

        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(99);
        chunks[1].Should().HaveCount(1);
        chunks[1][0].To.Should().HaveCount(2);
    }
}

public class ChunkPushNotificationReceiptIdsTests
{
    private readonly Expo _client = new();

    [Fact]
    public void ChunkPushNotificationReceiptIds_EmptyList_ReturnsEmpty()
    {
        var receiptIds = Array.Empty<string>();
        var chunks = _client.ChunkPushNotificationReceiptIds(receiptIds).ToList();
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void ChunkPushNotificationReceiptIds_SingleId_ReturnsSingleChunk()
    {
        var receiptIds = new[] { "receipt1" };
        var chunks = _client.ChunkPushNotificationReceiptIds(receiptIds).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(1);
    }

    [Fact]
    public void ChunkPushNotificationReceiptIds_300Ids_ReturnsSingleChunk()
    {
        var receiptIds = Enumerable.Range(0, 300).Select(i => $"receipt{i}").ToList();
        var chunks = _client.ChunkPushNotificationReceiptIds(receiptIds).ToList();

        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(300);
    }

    [Fact]
    public void ChunkPushNotificationReceiptIds_301Ids_Returns2Chunks()
    {
        var receiptIds = Enumerable.Range(0, 301).Select(i => $"receipt{i}").ToList();
        var chunks = _client.ChunkPushNotificationReceiptIds(receiptIds).ToList();

        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(300);
        chunks[1].Should().HaveCount(1);
    }

    [Fact]
    public void ChunkPushNotificationReceiptIds_2999Ids_Returns10Chunks()
    {
        var receiptIds = Enumerable.Range(0, 2999).Select(i => $"receipt{i}").ToList();
        var chunks = _client.ChunkPushNotificationReceiptIds(receiptIds).ToList();

        chunks.Should().HaveCount(10);
        chunks.Take(9).Should().AllSatisfy(c => c.Should().HaveCount(300));
        chunks.Last().Should().HaveCount(299);
        chunks.Sum(c => c.Count).Should().Be(2999);
    }
}
