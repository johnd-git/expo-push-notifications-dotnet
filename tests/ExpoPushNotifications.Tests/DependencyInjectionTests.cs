using ExpoPushNotifications.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpoPushNotifications.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddExpoClient_ResolvingClient_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddExpoClient();

        using var serviceProvider = services.BuildServiceProvider();

        var act = () => serviceProvider.GetRequiredService<IExpoClient>();
        act.Should().NotThrow();
    }
}
