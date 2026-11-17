using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Integrations;

/// <summary>
/// A player's connection to an issue tracker other than Jira: GitHub, Linear or Azure DevOps (OPP-116). One per
/// player and tracker. Tokens are stored encrypted (the application layer protects them) and never reach the browser.
/// </summary>
public sealed class TrackerConnection : Entity<Guid>
{
    private TrackerConnection(Guid id, Guid playerId, string provider) : base(id)
    {
        PlayerId = playerId;
        Provider = provider;
    }

    public Guid PlayerId { get; private set; }

    /// <summary>"github", "linear" or "azure-devops".</summary>
    public string Provider { get; private set; }

    public string ProtectedAccessToken { get; private set; } = string.Empty;
    public string? ProtectedRefreshToken { get; private set; }

    /// <summary>Null when the token does not expire (GitHub OAuth apps).</summary>
    public DateTimeOffset? AccessTokenExpiresOn { get; private set; }

    /// <summary>Who connected, as the tracker shows it (login or name), for "Connected as …".</summary>
    public string? AccountName { get; private set; }

    public bool NeedsReconnect { get; private set; }

    public static TrackerConnection Create(Guid playerId, string provider) => new(SequentialGuidGenerator.Generate(), playerId, provider);

    public void SetTokens(string protectedAccessToken, string? protectedRefreshToken, DateTimeOffset? expiresOn, string? accountName)
    {
        ProtectedAccessToken = protectedAccessToken;
        ProtectedRefreshToken = protectedRefreshToken;
        AccessTokenExpiresOn = expiresOn;
        AccountName = accountName ?? AccountName;
        NeedsReconnect = false;
    }

    public void MarkNeedsReconnect() => NeedsReconnect = true;
}

public interface ITrackerConnectionRepository
{
    Task<TrackerConnection?> GetAsync(Guid playerId, string provider, CancellationToken cancellationToken = default);
    void Add(TrackerConnection connection);
    void Remove(TrackerConnection connection);
}
