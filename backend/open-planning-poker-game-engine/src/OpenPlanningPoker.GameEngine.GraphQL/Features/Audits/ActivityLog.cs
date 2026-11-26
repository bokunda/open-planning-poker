using OpenPlanningPoker.GameEngine.Application.Features.Audits;
using OpenPlanningPoker.GameEngine.Domain.Audits;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Audits;

/// <summary>A page of the activity log, newest first (GitHub #20).</summary>
public sealed class ActivityLog
{
    public List<ActivityEntry> Entries { get; set; } = [];

    /// <summary>True when older entries exist; ask again with <c>before</c> = the last entry's <c>createdOn</c>.</summary>
    public bool HasMore { get; set; }

    public static ActivityLog From(AuditLogPage page) => new()
    {
        Entries = [.. page.Entries.Select(ActivityEntry.From)],
        HasMore = page.HasMore
    };
}

/// <summary>
/// One action in a game, team or organization. <c>action</c> is a stable code (e.g. <c>votes.revealed</c>) that the
/// app translates; <c>detail</c> names the subject (a ticket, a token) and <c>value</c> is the new value (an estimate).
/// </summary>
public sealed class ActivityEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public AuditType Type { get; set; }

    /// <summary>Who did it; empty for changes made by Open Planning Poker itself (e.g. from Slack).</summary>
    public Guid ActorId { get; set; }

    public Guid? TicketId { get; set; }
    public Guid? TargetId { get; set; }
    public string? Detail { get; set; }
    public string? Value { get; set; }
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>The actor's current name; null for the system and for users that no longer exist.</summary>
    public Task<string?> GetActorNameAsync([Service] IUserService users, CancellationToken cancellationToken) =>
        NameAsync(users, ActorId, cancellationToken);

    /// <summary>The name of the player the action was about, if it is a user that still exists.</summary>
    public Task<string?> GetTargetNameAsync([Service] IUserService users, CancellationToken cancellationToken) =>
        TargetId is { } id ? NameAsync(users, id, cancellationToken) : Task.FromResult<string?>(null);

    private static async Task<string?> NameAsync(IUserService users, Guid id, CancellationToken cancellationToken) =>
        id == Guid.Empty ? null : (await users.GetAsync(id.ToString(), cancellationToken))?.UserName;

    public static ActivityEntry From(Audit audit) => new()
    {
        Id = audit.Id,
        Action = audit.Action,
        Type = audit.Type,
        ActorId = audit.CreatedBy,
        TicketId = audit.TicketId,
        TargetId = audit.TargetId,
        Detail = audit.Detail,
        Value = audit.Value,
        CreatedOn = audit.CreatedOn
    };
}

[QueryType]
public class ActivityLogQueries
{
    /// <summary>
    /// The activity log (audit log) of a game (host and co-hosts), a team (owner), an organization (admins) or of
    /// yourself (USER, your integrations), newest first. Pass the <c>createdOn</c> of the oldest entry shown as
    /// <c>before</c> for the next page; <c>take</c> is 1 to 100. Entries are kept for 90 days.
    /// </summary>
    public async Task<FieldResult<ActivityLog, ApplicationError>> GetActivityLogAsync(
        [Service] ISender sender,
        [Required] AuditScope scope,
        [Required] Guid scopeId,
        DateTimeOffset? before,
        int take = GetAuditLog.DefaultTake,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAuditLogQuery(scope, scopeId, before, take), cancellationToken);
        return result.IsSuccess ? ActivityLog.From(result.Value!) : result.Error!;
    }
}
