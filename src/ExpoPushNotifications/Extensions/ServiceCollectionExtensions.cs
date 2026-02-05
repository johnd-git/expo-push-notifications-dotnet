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
        .AddStandardResilienceHandler(options =>
        {
            // Configure retry policy for rate limiting
            options.Retry.MaxRetryAttempts = Constants.DefaultMaxRetryAttempts;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromMilliseconds(Constants.DefaultRetryMinTimeoutMs);
            options.Retry.ShouldHandle = args => ValueTask.FromResult(
                args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests);

            // Disable circuit breaker for this client (we handle retries ourselves)
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(1);
            options.CircuitBreaker.FailureRatio = 1.0; // Never open
            options.CircuitBreaker.MinimumThroughput = int.MaxValue;

            // Disable timeout (we handle this ourselves)
            options.TotalRequestTimeout.Timeout = Timeout.InfiniteTimeSpan;
            options.AttemptTimeout.Timeout = Timeout.InfiniteTimeSpan;
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
}
