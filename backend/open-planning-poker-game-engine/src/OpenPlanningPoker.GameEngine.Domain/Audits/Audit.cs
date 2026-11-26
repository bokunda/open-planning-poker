using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Audits;

/// <summary>
/// One entry of the activity log (audit log, GitHub #20): who (<see cref="Entity{TEntityId}.CreatedBy"/>) did what
/// (<see cref="Action"/>, a stable code like <c>votes.revealed</c>) where (<see cref="Scope"/> and <see cref="ScopeId"/>)
/// and when (<see cref="Entity{TEntityId}.CreatedOn"/>). Texts are not stored; the app translates the action code.
/// <see cref="Detail"/> names the subject so the entry stays readable later (a ticket or token name) and
/// <see cref="Value"/> holds the new value (an estimate, a role, a deck); never secrets or votes.
/// </summary>
public sealed class Audit : Entity<Guid>
{
    public const int MaxActionLength = 64;
    public const int MaxDetailLength = 512;
    public const int MaxValueLength = 256;

    private Audit(Guid id, AuditScope scope, Guid scopeId, string action, AuditType type, Guid? ticketId, Guid? targetId, string? detail, string? value)
        : base(id)
    {
        Scope = scope;
        ScopeId = scopeId;
        Action = action;
        Type = type;
        TicketId = ticketId;
        TargetId = targetId;
        Detail = detail;
        Value = value;
    }

    public AuditScope Scope { get; private set; }
    public Guid ScopeId { get; private set; }
    public string Action { get; private set; }
    public AuditType Type { get; private set; }

    /// <summary>The ticket the action was about, if any (it may be deleted since).</summary>
    public Guid? TicketId { get; private set; }

    /// <summary>The player (or team) the action was about, if any.</summary>
    public Guid? TargetId { get; private set; }

    public string? Detail { get; private set; }

    public string? Value { get; private set; }

    public static Audit Create(AuditScope scope, Guid scopeId, string action, AuditType type, Guid? ticketId = null, Guid? targetId = null, string? detail = null, string? value = null)
    {
        if (scopeId == Guid.Empty)
        {
            throw new ArgumentException("An audit entry needs the id of its game, team, organization or user.", nameof(scopeId));
        }

        if (string.IsNullOrWhiteSpace(action) || action.Length > MaxActionLength)
        {
            throw new ArgumentException($"The action code must have 1 to {MaxActionLength} characters.", nameof(action));
        }

        return new Audit(SequentialGuidGenerator.Generate(), scope, scopeId, action, type, ticketId, targetId,
            Shorten(detail, MaxDetailLength), Shorten(value, MaxValueLength));
    }

    private static string? Shorten(string? text, int max)
    {
        var trimmed = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return trimmed is { Length: var length } && length > max ? trimmed[..(max - 1)] + "…" : trimmed;
    }
}

public interface IAuditRepository
{
    void Add(Audit audit);

    /// <summary>Forgets an added entry that could not be saved.</summary>
    void Discard(Audit audit);

    /// <summary>Entries of one game, team, organization or user, newest first, older than <paramref name="before"/>.</summary>
    Task<IReadOnlyList<Audit>> GetAsync(AuditScope scope, Guid scopeId, DateTimeOffset? before, int take, CancellationToken cancellationToken = default);

    /// <summary>Deletes the entries created before <paramref name="cutoff"/>; returns how many.</summary>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
