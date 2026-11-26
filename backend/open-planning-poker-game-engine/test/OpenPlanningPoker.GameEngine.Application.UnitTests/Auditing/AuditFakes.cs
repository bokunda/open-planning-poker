using OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;
using OpenPlanningPoker.GameEngine.Application.Abstractions.Authorization;
using OpenPlanningPoker.GameEngine.Domain.Abstractions;
using OpenPlanningPoker.GameEngine.Domain.Audits;
using OpenPlanningPoker.Shared.Errors;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Auditing;

/// <summary>An <see cref="IAuditLog"/> that keeps what it was asked to do, in order.</summary>
public sealed class RecordingAuditLog : IAuditLog
{
    public List<string> Calls { get; } = [];
    public List<AuditEntry> Written { get; } = [];

    /// <summary>The game a ticket entry resolves to; null makes the entry unplaceable.</summary>
    public Guid? TicketGame { get; set; } = Guid.NewGuid();

    public Task<AuditEntry?> ResolveAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        Calls.Add("resolve");
        if (entry.ScopeId != Guid.Empty) return Task.FromResult<AuditEntry?>(entry);
        return Task.FromResult(TicketGame is { } game ? entry with { ScopeId = game } : null);
    }

    public Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        Calls.Add("write");
        Written.Add(entry);
        return Task.CompletedTask;
    }
}

public sealed class FakeAuditRepository : IAuditRepository
{
    public List<Audit> Rows { get; } = [];
    public List<Audit> Discarded { get; } = [];

    public void Add(Audit audit) => Rows.Add(audit);

    public void Discard(Audit audit)
    {
        Rows.Remove(audit);
        Discarded.Add(audit);
    }

    public Task<IReadOnlyList<Audit>> GetAsync(AuditScope scope, Guid scopeId, DateTimeOffset? before, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Audit>>([.. Rows.Where(a => a.Scope == scope && a.ScopeId == scopeId).Take(take)]);

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) => Task.FromResult(0);
}

public sealed class FakeAuditUnitOfWork : IUnitOfWork
{
    public bool Fail { get; set; }
    public int Saves { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Fail) throw new InvalidOperationException("database is down");
        Saves++;
        return Task.FromResult(1);
    }
}

public sealed class FakeAuditTickets : ITicketRepository
{
    public Dictionary<Guid, Ticket> Tickets { get; } = [];

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Tickets.GetValueOrDefault(id));

    public Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult(Tickets.Values.Where(t => t.GameId == gameId));
    public Task<IEnumerable<Ticket>> GetByGames(IReadOnlyCollection<Guid> gameIds, CancellationToken cancellationToken = default) => Task.FromResult(Tickets.Values.Where(t => gameIds.Contains(t.GameId)));
    public Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Ticket>>(Tickets.Values);
    public Task<IEnumerable<Ticket>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult(Tickets.Values.Where(t => ids.Contains(t.Id)));
    public void Add(Ticket entity) => Tickets[entity.Id] = entity;
    public void AddRange(IEnumerable<Ticket> entities) { foreach (var t in entities) Add(t); }
    public void Update(Ticket entity) { }
    public void Delete(Ticket entity) => Tickets.Remove(entity.Id);
    public void DeleteRange(IEnumerable<Ticket> entities) { foreach (var t in entities) Delete(t); }
}

/// <summary>Grants or refuses each kind of check, and records which one was asked.</summary>
public sealed class FakeAuditAccess : IGameAccessService
{
    public Guid CurrentUserId { get; set; } = Guid.NewGuid();
    public ApplicationError? HostError { get; set; }
    public ApplicationError? TeamOwnerError { get; set; }
    public ApplicationError? OrganizationAdminError { get; set; }
    public List<string> Checks { get; } = [];

    public ApplicationError? EnsureAuthenticated() => CurrentUserId == Guid.Empty ? GameAccessErrors.Unauthenticated : null;
    public Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationError?>(null);

    public Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        Checks.Add("host");
        return Task.FromResult(HostError);
    }

    public Task<ApplicationError?> EnsureOwnerAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult(HostError);
    public Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationError?>(null);
    public Task<(Guid GameId, ApplicationError? Error)> EnsureHostOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult((Guid.Empty, HostError));
    public Task<(Guid GameId, ApplicationError? Error)> EnsureMemberOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult<(Guid, ApplicationError?)>((Guid.Empty, null));
    public Task<(Guid GameId, ApplicationError? Error)> EnsureVoterOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult<(Guid, ApplicationError?)>((Guid.Empty, null));
    public Task<ApplicationError?> EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationError?>(null);

    public Task<ApplicationError?> EnsureTeamOwnerAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        Checks.Add("teamOwner");
        return Task.FromResult(TeamOwnerError);
    }

    public Task<ApplicationError?> EnsureOrganizationAdminAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        Checks.Add("organizationAdmin");
        return Task.FromResult(OrganizationAdminError);
    }
}
