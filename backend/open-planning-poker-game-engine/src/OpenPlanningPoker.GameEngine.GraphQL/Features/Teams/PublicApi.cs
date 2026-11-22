namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Teams;

/// <summary>An API token of the team; the token itself is never shown again after it was created (OPP-158).</summary>
public sealed class TeamApiTokenInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>The first characters of the token, to tell tokens apart.</summary>
    public string Prefix { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastUsedOn { get; set; }
    public bool Active { get; set; }

    public static TeamApiTokenInfo From(ApiTokenInfo t) => new()
    {
        Id = t.Id, Name = t.Name, Prefix = t.Prefix, CreatedOn = t.CreatedOn, LastUsedOn = t.LastUsedOn, Active = t.Active,
    };
}

public sealed class CreatedTeamApiToken
{
    public TeamApiTokenInfo Info { get; set; } = new();

    /// <summary>Shown once: copy it now.</summary>
    public string Token { get; set; } = string.Empty;
}

public sealed class TeamWebhookInfo
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = [];
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    public static TeamWebhookInfo From(WebhookInfo w) => new() { Id = w.Id, Url = w.Url, Events = [.. w.Events], Enabled = w.Enabled, CreatedOn = w.CreatedOn };
}

public sealed class CreatedTeamWebhook
{
    public TeamWebhookInfo Info { get; set; } = new();

    /// <summary>The signing secret, shown once: store it with the receiver.</summary>
    public string Secret { get; set; } = string.Empty;
}

public sealed class TeamWebhookDeliveryInfo
{
    public Guid Id { get; set; }
    public string Event { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public int Attempt { get; set; }
    public int? StatusCode { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public double DurationMs { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}

/// <summary>The last deliveries of a webhook, newest first.</summary>
public sealed class TeamWebhookDeliveries
{
    public List<TeamWebhookDeliveryInfo> Items { get; set; } = [];
}

[ExtendObjectType(typeof(Team))]
public sealed class TeamPublicApiFields
{
    /// <summary>API tokens of the team, newest first. Owner only; empty for others.</summary>
    public async Task<List<TeamApiTokenInfo>> GetApiTokensAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] TeamApiService api, CancellationToken cancellationToken) =>
        await gameAccess.EnsureTeamOwnerAsync(team.Id, cancellationToken) is null
            ? [.. (await api.TokensAsync(team.Id, cancellationToken)).Select(TeamApiTokenInfo.From)]
            : [];

    /// <summary>Outgoing webhooks of the team. Owner only; empty for others.</summary>
    public async Task<List<TeamWebhookInfo>> GetWebhooksAsync(
        [Parent] Team team, [Service] IGameAccessService gameAccess, [Service] TeamApiService api, CancellationToken cancellationToken) =>
        await gameAccess.EnsureTeamOwnerAsync(team.Id, cancellationToken) is null
            ? [.. (await api.WebhooksAsync(team.Id, cancellationToken)).Select(TeamWebhookInfo.From)]
            : [];
}

[QueryType]
public class PublicApiQueries
{
    /// <summary>The last 20 deliveries of a webhook, newest first (OPP-160). Team owner.</summary>
    public async Task<FieldResult<TeamWebhookDeliveries, ApplicationError>> GetTeamWebhookDeliveriesAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] Guid webhookId, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.DeliveriesAsync(teamId, webhookId, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        return new TeamWebhookDeliveries
        {
            Items = [.. result.Value!.Select(d => new TeamWebhookDeliveryInfo
            {
                Id = d.Id, Event = d.Event, EventId = d.EventId, Attempt = d.Attempt, StatusCode = d.StatusCode,
                Success = d.Success, Error = d.Error, DurationMs = d.DurationMs, CreatedOn = d.CreatedOn,
            })],
        };
    }
}

[MutationType]
public class PublicApiMutations
{
    /// <summary>Creates an API token for the team; the token is in the answer only (OPP-158). Team owner.</summary>
    public async Task<FieldResult<CreatedTeamApiToken, ApplicationError>> CreateTeamApiTokenAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] string name, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.CreateTokenAsync(teamId, name, cancellationToken);
        return result.IsSuccess
            ? new CreatedTeamApiToken { Info = TeamApiTokenInfo.From(result.Value!.Info), Token = result.Value.Token }
            : result.Error!;
    }

    /// <summary>Revokes an API token at once. Team owner.</summary>
    public async Task<FieldResult<bool, ApplicationError>> RevokeTeamApiTokenAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] Guid tokenId, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.RevokeTokenAsync(teamId, tokenId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }

    /// <summary>Adds an outgoing webhook; the signing secret is in the answer only (OPP-160). Team owner.</summary>
    public async Task<FieldResult<CreatedTeamWebhook, ApplicationError>> CreateTeamWebhookAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] string url, [Required] List<string> events, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.CreateWebhookAsync(teamId, url, events, cancellationToken);
        return result.IsSuccess
            ? new CreatedTeamWebhook { Info = TeamWebhookInfo.From(result.Value!.Info), Secret = result.Value.Secret }
            : result.Error!;
    }

    /// <summary>Changes the address, the events or pauses a webhook. Team owner.</summary>
    public async Task<FieldResult<TeamWebhookInfo, ApplicationError>> UpdateTeamWebhookAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] Guid webhookId, [Required] string url, [Required] List<string> events, [Required] bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.UpdateWebhookAsync(teamId, webhookId, url, events, enabled, cancellationToken);
        return result.IsSuccess ? TeamWebhookInfo.From(result.Value!) : result.Error!;
    }

    /// <summary>Makes a new signing secret; the old one stops working at once. Team owner.</summary>
    public async Task<FieldResult<CreatedTeamWebhook, ApplicationError>> RotateTeamWebhookSecretAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] Guid webhookId, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.RotateSecretAsync(teamId, webhookId, cancellationToken);
        return result.IsSuccess
            ? new CreatedTeamWebhook { Info = TeamWebhookInfo.From(result.Value!.Info), Secret = result.Value.Secret }
            : result.Error!;
    }

    public async Task<FieldResult<bool, ApplicationError>> DeleteTeamWebhookAsync(
        [Service] IGameAccessService gameAccess, [Service] TeamApiService api,
        [Required] Guid teamId, [Required] Guid webhookId, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureTeamOwnerAsync(teamId, cancellationToken) is { } accessError) return accessError;
        var result = await api.DeleteWebhookAsync(teamId, webhookId, cancellationToken);
        return result.IsSuccess ? true : result.Error!;
    }
}
