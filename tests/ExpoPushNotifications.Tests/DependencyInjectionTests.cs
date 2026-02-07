using ExpoPushNotifications.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void AddExpoClient_WithCustomResilienceTimeouts_ResolvingClient_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddExpoClient(options =>
        {
            options.AttemptTimeout = TimeSpan.FromSeconds(15);
            options.TotalRequestTimeout = TimeSpan.FromSeconds(120);
            options.RetryMinTimeout = TimeSpan.FromMilliseconds(1500);
            options.MaxRetryAttempts = 3;
        });

        using var serviceProvider = services.BuildServiceProvider();

        var act = () => serviceProvider.GetRequiredService<IExpoClient>();
        act.Should().NotThrow();
    }

    [Fact]
    public void AddExpoClient_WithInvalidAttemptTimeout_ResolvingClient_ThrowsValidationException()
    {
        var services = new ServiceCollection();
        services.AddExpoClient(options =>
        {
            options.AttemptTimeout = TimeSpan.FromMilliseconds(1);
        });

        using var serviceProvider = services.BuildServiceProvider();

        var act = () => serviceProvider.GetRequiredService<IExpoClient>();
        act.Should().Throw<OptionsValidationException>();
    }
}
