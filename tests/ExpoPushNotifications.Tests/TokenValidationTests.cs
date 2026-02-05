using ExpoPushNotifications;
using FluentAssertions;
using Xunit;

namespace ExpoPushNotifications.Tests;

public class TokenValidationTests
{
    [Theory]
    [InlineData("ExpoPushToken[xxxxxxxxxxxxxxxxxxxxxx]", true)]
    [InlineData("ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]", true)]
    [InlineData("ExpoPushToken[abc123]", true)]
    [InlineData("ExponentPushToken[abc123]", true)]
    [InlineData("F5741A13-BCDA-434B-A316-5DC0E6FFA94F", true)] // UUID uppercase
    [InlineData("f5741a13-bcda-434b-a316-5dc0e6ffa94f", true)] // UUID lowercase
    [InlineData("f5741A13-bcDA-434B-a316-5dC0e6ffA94f", true)] // UUID mixed case
    public void IsExpoPushToken_ValidTokens_ReturnsTrue(string token, bool expected)
    {
        Expo.IsExpoPushToken(token).Should().Be(expected);
    }

    [Theory]
    [InlineData("dOKpuo4qbsM:APA91bHkSmF84ROx7Y-2eMpSSbgwuC3zycL", false)] // FCM token
    [InlineData("5fa729c6e535eb568g18fdabd35785fc60f41c161d9d7cf4b0bbb0d92437fda0", false)] // APNs token
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("invalid-token", false)]
    [InlineData("ExpoPushToken", false)] // Missing brackets
    [InlineData("ExpoPushToken[]", true)] // Empty brackets - still valid format
    [InlineData("ExpoPushTokenxxxxxxxxxxxxxxxxxxxxxx]", false)] // Missing opening bracket
    [InlineData("ExpoPushToken[xxxxxxxxxxxxxxxxxxxxxx", false)] // Missing closing bracket
    [InlineData("NotExpoPushToken[xxxxxxxxxxxxxxxxxxxxxx]", false)]
    [InlineData("expo-push-token[xxxxxxxxxxxxxxxxxxxxxx]", false)] // Wrong case
    public void IsExpoPushToken_InvalidTokens_ReturnsFalse(string token, bool expected)
    {
        Expo.IsExpoPushToken(token).Should().Be(expected);
    }

    [Fact]
    public void IsExpoPushToken_NullToken_ReturnsFalse()
    {
        Expo.IsExpoPushToken(null).Should().BeFalse();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", true)] // All zeros UUID
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff", true)] // All f's UUID
    [InlineData("12345678-1234-1234-1234-123456789012", true)] // Numeric UUID
    [InlineData("12345678-1234-1234-1234-12345678901", false)] // Too short
    [InlineData("12345678-1234-1234-1234-1234567890123", false)] // Too long
    [InlineData("12345678-1234-1234-1234", false)] // Missing segment
    [InlineData("12345678123412341234123456789012", false)] // Missing dashes
    public void IsExpoPushToken_UuidFormats_ValidatesCorrectly(string token, bool expected)
    {
        Expo.IsExpoPushToken(token).Should().Be(expected);
    }
}
