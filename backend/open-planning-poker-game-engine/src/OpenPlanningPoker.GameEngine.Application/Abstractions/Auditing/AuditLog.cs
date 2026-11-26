using OpenPlanningPoker.GameEngine.Domain.Audits;

namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;

/// <summary>Writes the activity log (GitHub #20). Writing is best effort: it never fails the action it records.</summary>
public interface IAuditLog
{
    /// <summary>
    /// Completes an entry before the action runs: a ticket entry gets its game and the ticket name, which are gone
    /// once the ticket is deleted. Returns null when the entry cannot be placed (e.g. the ticket does not exist).
    /// </summary>
    Task<AuditEntry?> ResolveAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Saves a resolved entry.</summary>
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Resolves and saves; for services that are not MediatR commands, after a successful change.</summary>
    async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (await ResolveAsync(entry, cancellationToken) is { } resolved)
        {
            await WriteAsync(resolved, cancellationToken);
        }
    }
}

public sealed class AuditLog(IAuditRepository audits, ITicketRepository tickets, IUnitOfWork unitOfWork, ILogger<AuditLog> logger) : IAuditLog
{
    public async Task<AuditEntry?> ResolveAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var needsTicket = entry.TicketId is not null && (entry.ScopeId == Guid.Empty || entry.Detail is null);
            if (!needsTicket)
            {
                return entry.ScopeId == Guid.Empty ? null : entry;
            }

            var ticket = await tickets.GetByIdAsync(entry.TicketId!.Value, cancellationToken);
            if (ticket is null)
            {
                return entry.ScopeId == Guid.Empty ? null : entry;
            }

            return entry with
            {
                ScopeId = entry.ScopeId == Guid.Empty ? ticket.GameId : entry.ScopeId,
                Detail = entry.Detail ?? ticket.Name
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not prepare the audit entry {Action}", entry.Action);
            return null;
        }
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        Audit? audit = null;
        try
        {
            audit = Audit.Create(entry.Scope, entry.ScopeId, entry.Action, entry.Type, entry.TicketId, entry.TargetId, entry.Detail, entry.Value);
            audits.Add(audit);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The action itself is already saved; a missing log line must not turn it into an error, and the
            // unsaved entry must not break the next save of this request.
            if (audit is not null)
            {
                audits.Discard(audit);
            }

            logger.LogWarning(exception, "Could not write the audit entry {Action} for {Scope} {ScopeId}", entry.Action, entry.Scope, entry.ScopeId);
        }
    }
}
