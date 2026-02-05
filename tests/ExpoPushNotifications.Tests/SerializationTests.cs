using System.Text.Json;
using ExpoPushNotifications.Models;
using ExpoPushNotifications.Models.Enums;
using FluentAssertions;
using Xunit;

namespace ExpoPushNotifications.Tests;

public class ExpoPushMessageSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void Serialize_SimpleMessage_ProducesCorrectJson()
    {
        var message = new ExpoPushMessage
        {
            To = ["ExpoPushToken[token1]"],
            Title = "Hello",
            Body = "World"
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"to\":\"ExpoPushToken[token1]\"");
        json.Should().Contain("\"title\":\"Hello\"");
        json.Should().Contain("\"body\":\"World\"");
    }

    [Fact]
    public void Serialize_MessageWithMultipleRecipients_ProducesArray()
    {
        var message = new ExpoPushMessage
        {
            To = ["token1", "token2", "token3"],
            Body = "Test"
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"to\":[\"token1\",\"token2\",\"token3\"]");
    }

    [Fact]
    public void Serialize_MessageWithPriority_UsesKebabCase()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            Priority = PushPriority.High
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"priority\":\"high\"");
    }

    [Fact]
    public void Serialize_MessageWithInterruptionLevel_UsesKebabCase()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            InterruptionLevel = InterruptionLevel.TimeSensitive
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"interruptionLevel\":\"time-sensitive\"");
    }

    [Fact]
    public void Serialize_MessageWithSoundString_SerializesAsString()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            Sound = "default"
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"sound\":\"default\"");
    }

    [Fact]
    public void Serialize_MessageWithSoundObject_SerializesAsObject()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            Sound = new ExpoPushSound { Name = "custom", Critical = true, Volume = 0.8 }
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"sound\":");
        json.Should().Contain("\"name\":\"custom\"");
        json.Should().Contain("\"critical\":true");
        json.Should().Contain("\"volume\":0.8");
    }

    [Fact]
    public void Serialize_MessageWithRichContent_IncludesImage()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            RichContent = new ExpoPushRichContent { Image = "https://example.com/image.jpg" }
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"richContent\":");
        json.Should().Contain("\"image\":\"https://example.com/image.jpg\"");
    }

    [Fact]
    public void Serialize_MessageWithData_IncludesCustomData()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"],
            Data = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42
            }
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"data\":");
        json.Should().Contain("\"key1\":\"value1\"");
        json.Should().Contain("\"key2\":42");
    }

    [Fact]
    public void Serialize_MessageWithNullProperties_OmitsNullValues()
    {
        var message = new ExpoPushMessage
        {
            To = ["token"]
        };

        var json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().NotContain("\"title\"");
        json.Should().NotContain("\"body\"");
        json.Should().NotContain("\"sound\"");
        json.Should().NotContain("\"data\"");
    }
}

public class ExpoPushTicketSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_SuccessTicket_ReturnsExpoPushSuccessTicket()
    {
        var json = """{"status":"ok","id":"receipt-id-123"}""";

        var ticket = JsonSerializer.Deserialize<ExpoPushTicket>(json, JsonOptions);

        ticket.Should().BeOfType<ExpoPushSuccessTicket>();
        var successTicket = (ExpoPushSuccessTicket)ticket!;
        successTicket.Status.Should().Be("ok");
        successTicket.Id.Should().Be("receipt-id-123");
        successTicket.IsSuccess.Should().BeTrue();
        successTicket.IsError.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_ErrorTicket_ReturnsExpoPushErrorTicket()
    {
        var json = """
            {
                "status": "error",
                "message": "Invalid push token",
                "details": {
                    "error": "DeviceNotRegistered",
                    "expoPushToken": "ExpoPushToken[invalid]"
                }
            }
            """;

        var ticket = JsonSerializer.Deserialize<ExpoPushTicket>(json, JsonOptions);

        ticket.Should().BeOfType<ExpoPushErrorTicket>();
        var errorTicket = (ExpoPushErrorTicket)ticket!;
        errorTicket.Status.Should().Be("error");
        errorTicket.Message.Should().Be("Invalid push token");
        errorTicket.IsSuccess.Should().BeFalse();
        errorTicket.IsError.Should().BeTrue();
        errorTicket.Details.Should().NotBeNull();
        errorTicket.Details!.Error.Should().Be(PushErrorCode.DeviceNotRegistered);
        errorTicket.Details.ExpoPushToken.Should().Be("ExpoPushToken[invalid]");
    }

    [Fact]
    public void Deserialize_TicketArray_DeserializesCorrectly()
    {
        var json = """
            [
                {"status":"ok","id":"id1"},
                {"status":"error","message":"Error message"}
            ]
            """;

        var tickets = JsonSerializer.Deserialize<List<ExpoPushTicket>>(json, JsonOptions);

        tickets.Should().HaveCount(2);
        tickets![0].Should().BeOfType<ExpoPushSuccessTicket>();
        tickets[1].Should().BeOfType<ExpoPushErrorTicket>();
    }
}

public class ExpoPushReceiptSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Deserialize_SuccessReceipt_ReturnsExpoPushSuccessReceipt()
    {
        var json = """{"status":"ok"}""";

        var receipt = JsonSerializer.Deserialize<ExpoPushReceipt>(json, JsonOptions);

        receipt.Should().BeOfType<ExpoPushSuccessReceipt>();
        receipt!.Status.Should().Be("ok");
        receipt.IsSuccess.Should().BeTrue();
        receipt.IsError.Should().BeFalse();
    }

    [Fact]
    public void Deserialize_ErrorReceipt_ReturnsExpoPushErrorReceipt()
    {
        var json = """
            {
                "status": "error",
                "message": "The device cannot receive push notifications",
                "details": {
                    "error": "DeviceNotRegistered"
                }
            }
            """;

        var receipt = JsonSerializer.Deserialize<ExpoPushReceipt>(json, JsonOptions);

        receipt.Should().BeOfType<ExpoPushErrorReceipt>();
        var errorReceipt = (ExpoPushErrorReceipt)receipt!;
        errorReceipt.Status.Should().Be("error");
        errorReceipt.Message.Should().Be("The device cannot receive push notifications");
        errorReceipt.Details.Should().NotBeNull();
        errorReceipt.Details!.Error.Should().Be(PushErrorCode.DeviceNotRegistered);
    }

    [Fact]
    public void Deserialize_ReceiptMap_DeserializesCorrectly()
    {
        var json = """
            {
                "receipt1": {"status":"ok"},
                "receipt2": {"status":"error","message":"Failed"}
            }
            """;

        var receipts = JsonSerializer.Deserialize<Dictionary<string, ExpoPushReceipt>>(json, JsonOptions);

        receipts.Should().HaveCount(2);
        receipts!["receipt1"].Should().BeOfType<ExpoPushSuccessReceipt>();
        receipts["receipt2"].Should().BeOfType<ExpoPushErrorReceipt>();
    }
}

public class PushErrorCodeSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory]
    [InlineData("DeviceNotRegistered", PushErrorCode.DeviceNotRegistered)]
    [InlineData("MessageTooBig", PushErrorCode.MessageTooBig)]
    [InlineData("MessageRateExceeded", PushErrorCode.MessageRateExceeded)]
    [InlineData("InvalidCredentials", PushErrorCode.InvalidCredentials)]
    [InlineData("ExpoError", PushErrorCode.ExpoError)]
    [InlineData("ProviderError", PushErrorCode.ProviderError)]
    [InlineData("DeveloperError", PushErrorCode.DeveloperError)]
    public void Deserialize_ErrorCode_ParsesCorrectly(string jsonValue, PushErrorCode expected)
    {
        var json = $$$"""{"status":"error","message":"test","details":{"error":"{{{jsonValue}}}"}}""";

        var receipt = JsonSerializer.Deserialize<ExpoPushErrorReceipt>(json, JsonOptions);

        receipt!.Details!.Error.Should().Be(expected);
    }
}
