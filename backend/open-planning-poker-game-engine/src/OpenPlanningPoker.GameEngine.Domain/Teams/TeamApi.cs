using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Teams;

/// <summary>
/// A token for the public API of one team (OPP-158). Only a SHA-256 hash of the token is stored; the token itself is
/// shown once when it is created. <see cref="Prefix"/> is the start of the token, so the owner can tell tokens apart.
/// </summary>
public sealed class TeamApiToken : Entity<Guid>
{
    public const int MaxNameLength = 60;
    public const int MaxPerTeam = 10;

    private TeamApiToken(Guid id, Guid teamId, string name, string hash, string prefix) : base(id)
    {
        TeamId = teamId;
        Name = name;
        Hash = hash;
        Prefix = prefix;
    }

    public Guid TeamId { get; private set; }
    public string Name { get; private set; }
    public string Hash { get; private set; }
    public string Prefix { get; private set; }
    public DateTimeOffset? LastUsedOn { get; private set; }
    public DateTimeOffset? RevokedOn { get; private set; }

    public bool IsActive => RevokedOn is null;

    public static TeamApiToken Create(Guid teamId, string name, string hash, string prefix) =>
        new(SequentialGuidGenerator.Generate(), teamId, name.Trim(), hash, prefix);

    public void Revoke() => RevokedOn ??= DateTimeOffset.UtcNow;

    /// <summary>Marks a use; returns whether it changed enough to save (once a minute is precise enough).</summary>
    public bool Used(DateTimeOffset now)
    {
        if (LastUsedOn is { } last && now - last < TimeSpan.FromMinutes(1)) return false;
        LastUsedOn = now;
        return true;
    }
}

/// <summary>
/// An outgoing webhook of a team (OPP-160). Every request carries an HMAC-SHA256 signature made with the secret, which
/// is stored encrypted (the application layer protects it) because the server needs it to sign.
/// </summary>
public sealed class TeamWebhook : Entity<Guid>
{
    public const int MaxUrlLength = 512;
    public const int MaxPerTeam = 5;

    public static readonly IReadOnlyList<string> Events = ["ticket.estimated", "session.finished"];

    private TeamWebhook(Guid id, Guid teamId, string url, string protectedSecret, string eventList) : base(id)
    {
        TeamId = teamId;
        Url = url;
        ProtectedSecret = protectedSecret;
        EventList = eventList;
    }

    public Guid TeamId { get; private set; }
    public string Url { get; private set; }
    public string ProtectedSecret { get; private set; }

    /// <summary>Comma-separated event names.</summary>
    public string EventList { get; private set; }
    public bool Enabled { get; private set; } = true;

    public IReadOnlyList<string> SubscribedEvents => EventList.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public static TeamWebhook Create(Guid teamId, string url, string protectedSecret, IEnumerable<string> events) =>
        new(SequentialGuidGenerator.Generate(), teamId, url.Trim(), protectedSecret, string.Join(',', events.Distinct()));

    public void Update(string url, IEnumerable<string> events, bool enabled)
    {
        Url = url.Trim();
        EventList = string.Join(',', events.Distinct());
        Enabled = enabled;
    }

    public void SetSecret(string protectedSecret) => ProtectedSecret = protectedSecret;
}

/// <summary>One attempt to deliver an event to a webhook; the newest 20 per webhook are kept.</summary>
public sealed class TeamWebhookDelivery : Entity<Guid>
{
    public const int Kept = 20;
    public const int MaxErrorLength = 300;

    private TeamWebhookDelivery(Guid id, Guid webhookId, string @event, Guid eventId, int attempt) : base(id)
    {
        WebhookId = webhookId;
        Event = @event;
        EventId = eventId;
        Attempt = attempt;
    }

    public Guid WebhookId { get; private set; }
    public string Event { get; private set; }

    /// <summary>The same for every attempt of one event, so receivers can drop duplicates.</summary>
    public Guid EventId { get; private set; }
    public int Attempt { get; private set; }
    public int? StatusCode { get; private set; }
    public bool Success { get; private set; }
    public string? Error { get; private set; }
    public double DurationMs { get; private set; }

    public static TeamWebhookDelivery Create(Guid webhookId, string eventName, Guid eventId, int attempt, int? statusCode, bool success, string? error, double durationMs)
    {
        var delivery = new TeamWebhookDelivery(SequentialGuidGenerator.Generate(), webhookId, eventName, eventId, attempt)
        {
            StatusCode = statusCode,
            Success = success,
            Error = error is null ? null : error.Length > MaxErrorLength ? error[..MaxErrorLength] : error,
            DurationMs = Math.Round(durationMs),
        };
        return delivery;
    }
}

public interface ITeamApiRepository
{
    Task<TeamApiToken?> GetTokenByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task<TeamApiToken?> GetTokenAsync(Guid tokenId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamApiToken>> GetTokensAsync(Guid teamId, CancellationToken cancellationToken = default);
    void AddToken(TeamApiToken token);

    Task<IReadOnlyList<TeamWebhook>> GetWebhooksAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamWebhook?> GetWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);
    void AddWebhook(TeamWebhook webhook);
    void RemoveWebhook(TeamWebhook webhook);

    Task<IReadOnlyList<TeamWebhookDelivery>> GetDeliveriesAsync(Guid webhookId, CancellationToken cancellationToken = default);
    void AddDelivery(TeamWebhookDelivery delivery);
    void RemoveDeliveries(IEnumerable<TeamWebhookDelivery> deliveries);
}
