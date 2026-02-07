using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ExpoPushNotifications.Extensions;

/// <summary>
/// Extension methods for configuring Expo push notification services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly string SdkVersion = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

    /// <summary>
    /// Adds the Expo push notification client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for client options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs or Startup.cs
    /// builder.Services.AddExpoClient(options =>
    /// {
    ///     options.AccessToken = builder.Configuration["Expo:AccessToken"];
    ///     options.MaxConcurrentRequests = 6;
    /// });
    ///
    /// // In your service
    /// public class NotificationService
    /// {
    ///     private readonly IExpoClient _expo;
    ///
    ///     public NotificationService(IExpoClient expo) => _expo = expo;
    ///
    ///     public async Task SendAsync(string token, string message)
    ///     {
    ///         var tickets = await _expo.SendPushNotificationsAsync(new[]
    ///         {
    ///             new ExpoPushMessage { To = new[] { token }, Body = message }
    ///         });
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddExpoClient(
        this IServiceCollection services,
        Action<ExpoClientOptions>? configure = null)
    {
        var configuredOptions = new ExpoClientOptions();
        configure?.Invoke(configuredOptions);

        // Configure options
        var optionsBuilder = services.AddOptions<ExpoClientOptions>();
        if (configure != null)
        {
            optionsBuilder.Configure(configure);
        }

        // Add HttpClient with resilience
        services.AddHttpClient<IExpoClient, Expo>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExpoClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"expo-push-notifications-dotnet/{SdkVersion}");

            if (!string.IsNullOrEmpty(options.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
            }
        })
        .AddStandardResilienceHandler(resilienceOptions =>
        {
            // Configure retry policy for rate limiting
            resilienceOptions.Retry.MaxRetryAttempts = configuredOptions.MaxRetryAttempts;
            resilienceOptions.Retry.BackoffType = DelayBackoffType.Exponential;
            resilienceOptions.Retry.Delay = configuredOptions.RetryMinTimeout;
            resilienceOptions.Retry.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests);

            // Keep circuit breaker effectively disabled without violating resilience validation rules.
            resilienceOptions.CircuitBreaker.SamplingDuration = GetCircuitBreakerSamplingDuration(configuredOptions.AttemptTimeout);
            resilienceOptions.CircuitBreaker.FailureRatio = 1.0; // Never open
            resilienceOptions.CircuitBreaker.MinimumThroughput = int.MaxValue;

            resilienceOptions.AttemptTimeout.Timeout = configuredOptions.AttemptTimeout;
            resilienceOptions.TotalRequestTimeout.Timeout = configuredOptions.TotalRequestTimeout;
        });

        return services;
    }

    /// <summary>
    /// Adds the Expo push notification client to the service collection with a custom HTTP client configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Configuration action for client options.</param>
    /// <param name="configureHttpClient">Configuration action for the HTTP client builder.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExpoClient(
        this IServiceCollection services,
        Action<ExpoClientOptions> configureOptions,
        Action<IHttpClientBuilder> configureHttpClient)
    {
        services.AddOptions<ExpoClientOptions>().Configure(configureOptions);

        var builder = services.AddHttpClient<IExpoClient, Expo>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ExpoClientOptions>>().Value;

            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"expo-push-notifications-dotnet/{SdkVersion}");

            if (!string.IsNullOrEmpty(options.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
            }
        });

        configureHttpClient(builder);

        return services;
    }

    private static TimeSpan GetCircuitBreakerSamplingDuration(TimeSpan attemptTimeout)
    {
        var minimumSamplingDuration = TimeSpan.FromSeconds(20);
        var requiredSamplingDuration = TimeSpan.FromTicks(attemptTimeout.Ticks * 2);

        return requiredSamplingDuration > minimumSamplingDuration
            ? requiredSamplingDuration
            : minimumSamplingDuration;
    }
}
