using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ExpoPushNotifications.Extensions;
using ExpoPushNotifications.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpoPushNotifications.Tests;

/// <summary>
/// Expo gzips large responses when the client advertises gzip support.
/// These tests run a local HTTP server that does the same, so the real
/// HTTP handler pipeline is exercised (not a mocked handler).
/// </summary>
public sealed class ResponseDecompressionTests : IDisposable
{
    private const int MessageCount = 30;

    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverLoop;
    private int _gzippedResponses;

    public ResponseDecompressionTests()
    {
        BaseUrl = $"http://localhost:{GetFreePort()}";
        _listener.Prefixes.Add(BaseUrl + "/");
        _listener.Start();
        _serverLoop = Task.Run(ServeAsync);
    }

    private string BaseUrl { get; }

    [Fact]
    public async Task Expo_DefaultConstructor_ReadsGzipCompressedResponse()
    {
        using var expo = new Expo(new ExpoClientOptions { BaseUrl = BaseUrl });

        var tickets = await expo.SendPushNotificationsAsync(CreateMessages());

        tickets.Should().HaveCount(MessageCount);
        _gzippedResponses.Should().Be(1);
    }

    [Fact]
    public async Task AddExpoClient_ReadsGzipCompressedResponse()
    {
        var services = new ServiceCollection();
        services.AddExpoClient(options => options.BaseUrl = BaseUrl);
        using var serviceProvider = services.BuildServiceProvider();
        var expo = serviceProvider.GetRequiredService<IExpoClient>();

        var tickets = await expo.SendPushNotificationsAsync(CreateMessages());

        tickets.Should().HaveCount(MessageCount);
        _gzippedResponses.Should().Be(1);
    }

    [Fact]
    public async Task AddExpoClient_WithHttpClientBuilder_ReadsGzipCompressedResponse()
    {
        var services = new ServiceCollection();
        services.AddExpoClient(options => options.BaseUrl = BaseUrl, _ => { });
        using var serviceProvider = services.BuildServiceProvider();
        var expo = serviceProvider.GetRequiredService<IExpoClient>();

        var tickets = await expo.SendPushNotificationsAsync(CreateMessages());

        tickets.Should().HaveCount(MessageCount);
        _gzippedResponses.Should().Be(1);
    }

    private static List<ExpoPushMessage> CreateMessages() =>
        Enumerable.Range(0, MessageCount)
            .Select(i => ExpoPushMessage.Create($"ExpoPushToken[token-{i}]"))
            .ToList();

    private async Task ServeAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (_cts.IsCancellationRequested)
            {
                return;
            }

            var tickets = Enumerable.Range(0, MessageCount)
                .Select(i => $$"""{"status":"ok","id":"receipt-{{i}}"}""");
            var body = Encoding.UTF8.GetBytes($$"""{"data":[{{string.Join(",", tickets)}}]}""");

            // Like Expo, only compress when the client says it can decompress.
            var acceptEncoding = context.Request.Headers["Accept-Encoding"] ?? string.Empty;
            if (acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                using var compressed = new MemoryStream();
                using (var gzip = new GZipStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzip.Write(body);
                }

                body = compressed.ToArray();
                context.Response.AddHeader("Content-Encoding", "gzip");
                Interlocked.Increment(ref _gzippedResponses);
            }

            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
            context.Response.Close();
        }
    }

    private static int GetFreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
        try
        {
            _serverLoop.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Listener shutdown can fault the loop; nothing to clean up.
        }

        _cts.Dispose();
    }
}
