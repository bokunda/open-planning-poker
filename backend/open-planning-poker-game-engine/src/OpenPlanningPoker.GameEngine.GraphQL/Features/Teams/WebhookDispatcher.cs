using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Abstractions;
using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>In-memory queue of webhook events (OPP-160). A restart drops what is waiting; the deliveries list shows what was sent.</summary>
public sealed class WebhookQueue : IWebhookQueue
{
    private readonly Channel<WebhookJob> _channel = Channel.CreateBounded<WebhookJob>(new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
    });

    public void Enqueue(WebhookJob job) => _channel.Writer.TryWrite(job);

    public ChannelReader<WebhookJob> Reader => _channel.Reader;
}

/// <summary>
/// Sends queued events to the teams' webhooks: signed, with a 10 s timeout, retried after 1, 5 and 30 minutes, and
/// only to public addresses (checked when the connection is opened, so DNS cannot point it at an internal host).
/// </summary>
public sealed class WebhookDispatcher(WebhookQueue queue, IServiceScopeFactory scopes, IHttpClientFactory httpClients, ILogger<WebhookDispatcher> logger)
    : BackgroundService
{
    public const string HttpClientName = "team-webhooks";
    public static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(job, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Webhook {WebhookId} delivery failed unexpectedly", job.WebhookId);
            }
        }
    }

    private async Task SendAsync(WebhookJob job, CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<ITeamApiRepository>();
        var protector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var webhook = await api.GetWebhookAsync(job.WebhookId, cancellationToken);
        if (webhook is null || !webhook.Enabled) return;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
        {
            Content = new StringContent(job.Body, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation(WebhookSigning.SignatureHeader, WebhookSigning.Sign(protector.Unprotect(webhook.ProtectedSecret), timestamp, job.Body));
        request.Headers.TryAddWithoutValidation(WebhookSigning.EventHeader, job.Event);
        request.Headers.TryAddWithoutValidation(WebhookSigning.DeliveryHeader, job.EventId.ToString());
        request.Headers.UserAgent.ParseAdd("OpenPlanningPoker-Webhooks/1.0");

        int? status = null;
        string? error = null;
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await httpClients.CreateClient(HttpClientName).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode) error = $"HTTP {status}";
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or BlockedAddressException)
        {
            error = exception is TaskCanceledException ? "Timed out after 10 s" : exception.GetBaseException().Message;
        }

        var success = error is null;
        api.AddDelivery(TeamWebhookDelivery.Create(webhook.Id, job.Event, job.EventId, job.Attempt, status, success, error, watch.Elapsed.TotalMilliseconds));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var old = (await api.GetDeliveriesAsync(webhook.Id, cancellationToken)).Skip(TeamWebhookDelivery.Kept).ToList();
        if (old.Count > 0)
        {
            api.RemoveDeliveries(old);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 4xx other than 408 and 429 will not get better by trying again.
        var retry = !success && job.Attempt <= Backoff.Length && status is null or >= 500 or 408 or 429;
        if (retry)
        {
            var delay = Backoff[job.Attempt - 1];
            _ = Task.Delay(delay, cancellationToken).ContinueWith(_ => queue.Enqueue(job with { Attempt = job.Attempt + 1 }),
                cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }

    /// <summary>The handler for webhook requests: connects only to public addresses and does not follow redirects.</summary>
    public static SocketsHttpHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        ConnectCallback = async (context, cancellationToken) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
            var target = addresses.FirstOrDefault(TeamApiService.IsPublic)
                ?? throw new BlockedAddressException(context.DnsEndPoint.Host);
            var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(target, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}

/// <summary>The webhook host resolves only to internal addresses.</summary>
public sealed class BlockedAddressException(string host) : Exception($"{host} does not resolve to a public address");
