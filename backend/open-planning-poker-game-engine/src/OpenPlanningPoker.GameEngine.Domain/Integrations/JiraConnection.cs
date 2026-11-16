using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Integrations;

/// <summary>
/// A player's connection to a Jira Cloud site (OPP-10). The tokens are stored encrypted (the application layer
/// protects them before they reach the entity); the browser never sees them.
/// </summary>
public sealed class JiraConnection : Entity<Guid>
{
    private JiraConnection(Guid id, Guid playerId) : base(id) => PlayerId = playerId;

    public Guid PlayerId { get; private set; }

    /// <summary>The chosen site; null until the player picks one after consent.</summary>
    public string? CloudId { get; private set; }
    public string? SiteName { get; private set; }
    public string? SiteUrl { get; private set; }

    public string ProtectedAccessToken { get; private set; } = string.Empty;
    public string ProtectedRefreshToken { get; private set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresOn { get; private set; }

    /// <summary>Set when Jira rejected the refresh token (consent revoked): the player has to connect again.</summary>
    public bool NeedsReconnect { get; private set; }

    public static JiraConnection Create(Guid playerId) => new(SequentialGuidGenerator.Generate(), playerId);

    public void SetTokens(string protectedAccessToken, string protectedRefreshToken, DateTimeOffset expiresOn)
    {
        ProtectedAccessToken = protectedAccessToken;
        ProtectedRefreshToken = protectedRefreshToken;
        AccessTokenExpiresOn = expiresOn;
        NeedsReconnect = false;
    }

    public void SelectSite(string cloudId, string siteName, string siteUrl)
    {
        CloudId = cloudId;
        SiteName = siteName;
        SiteUrl = siteUrl;
    }

    public void MarkNeedsReconnect() => NeedsReconnect = true;
}

public interface IJiraConnectionRepository
{
    Task<JiraConnection?> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
    void Add(JiraConnection connection);
    void Remove(JiraConnection connection);
}
