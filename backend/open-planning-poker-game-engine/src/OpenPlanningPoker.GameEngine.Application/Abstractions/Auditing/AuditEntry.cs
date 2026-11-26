using OpenPlanningPoker.GameEngine.Domain.Audits;

namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;

/// <summary>
/// What a command or service tells the activity log (GitHub #20). <see cref="Detail"/> names the subject (a ticket,
/// a token); <see cref="Value"/> is the new value (an estimate, a role). A ticket entry may leave <see cref="ScopeId"/>
/// or <see cref="Detail"/> empty: <see cref="IAuditLog.ResolveAsync"/> fills in the ticket's game and name.
/// </summary>
public sealed record AuditEntry(
    string Action,
    AuditType Type,
    AuditScope Scope,
    Guid ScopeId,
    Guid? TicketId = null,
    Guid? TargetId = null,
    string? Detail = null,
    string? Value = null)
{
    public static AuditEntry Game(string action, AuditType type, Guid gameId, Guid? targetId = null, string? detail = null, string? value = null) =>
        new(action, type, AuditScope.Game, gameId, null, targetId, detail, value);

    /// <summary>An action on a ticket of a game; the game and the ticket name are looked up.</summary>
    public static AuditEntry Ticket(string action, AuditType type, Guid ticketId, string? value = null) =>
        new(action, type, AuditScope.Game, Guid.Empty, ticketId, null, null, value);

    public static AuditEntry Team(string action, AuditType type, Guid teamId, Guid? targetId = null, string? detail = null, Guid? ticketId = null) =>
        new(action, type, AuditScope.Team, teamId, ticketId, targetId, detail);

    public static AuditEntry Organization(string action, AuditType type, Guid organizationId, Guid? targetId = null, string? detail = null, string? value = null) =>
        new(action, type, AuditScope.Organization, organizationId, null, targetId, detail, value);

    public static AuditEntry User(string action, AuditType type, Guid userId, string? value = null) =>
        new(action, type, AuditScope.User, userId, null, null, null, value);
}

/// <summary>
/// A MediatR command that is written to the activity log when it succeeds (see <see cref="AuditBehavior{TRequest,TResponse}"/>).
/// Return null to skip the entry.
/// </summary>
public interface IAuditedRequest
{
    AuditEntry? ToAuditEntry();
}
