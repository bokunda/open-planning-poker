using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Teams;

/// <summary>
/// A persistent room for one team. Each planning session is a game that belongs to the team (a sprint),
/// so the team keeps its history and velocity across sprints. The owner is <see cref="Entity{TEntityId}.CreatedBy"/>.
/// </summary>
public sealed class Team : Entity<Guid>
{
    private Team(Guid id, string name, string description) : base(id)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }

    public ICollection<TeamMember>? Members { get; set; }

    public const int MaxWebhookLength = 512;

    /// <summary>Incoming webhook (Slack or Microsoft Teams) that gets the session summary (OPP-136); null when none.</summary>
    public string? SummaryWebhookUrl { get; private set; }

    public void SetSummaryWebhook(string? url) => SummaryWebhookUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    public static Team Create(string name, string description) =>
        new(SequentialGuidGenerator.Generate(), name, description);

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

public sealed class TeamMember : Entity<Guid>
{
    private TeamMember(Guid id, Guid teamId, Guid playerId) : base(id)
    {
        TeamId = teamId;
        PlayerId = playerId;
    }

    public Guid TeamId { get; private set; }
    public Guid PlayerId { get; private set; }

    public Team Team { get; set; } = null!;

    public static TeamMember Create(Guid teamId, Guid playerId) =>
        new(SequentialGuidGenerator.Generate(), teamId, playerId);
}

/// <summary>
/// A reference ticket of a team (OPP-127): "this is what a 3 looks like for us". It points to an estimated ticket of
/// one of the team's sprints and keeps the card value it had when it was pinned.
/// </summary>
public sealed class TeamAnchor : Entity<Guid>
{
    public const int MaxNoteLength = 200;

    /// <summary>At most this many anchors per card value, so the reference stays short.</summary>
    public const int MaxPerValue = 2;

    private TeamAnchor(Guid id, Guid teamId, Guid ticketId, string value, string? note) : base(id)
    {
        TeamId = teamId;
        TicketId = ticketId;
        Value = value;
        Note = note;
    }

    public Guid TeamId { get; private set; }
    public Guid TicketId { get; private set; }
    public string Value { get; private set; }
    public string? Note { get; private set; }

    public static TeamAnchor Create(Guid teamId, Guid ticketId, string value, string? note) =>
        new(SequentialGuidGenerator.Generate(), teamId, ticketId, value, string.IsNullOrWhiteSpace(note) ? null : note.Trim());
}

/// <summary>
/// Days one member is available in the sprint of a planning session (OPP-146). Members without a row are available for
/// the whole sprint.
/// </summary>
public sealed class SprintCapacity : Entity<Guid>
{
    private SprintCapacity(Guid id, Guid gameId, Guid playerId, double days) : base(id)
    {
        GameId = gameId;
        PlayerId = playerId;
        Days = days;
    }

    public Guid GameId { get; private set; }
    public Guid PlayerId { get; private set; }
    public double Days { get; private set; }

    public static SprintCapacity Create(Guid gameId, Guid playerId, double days) =>
        new(SequentialGuidGenerator.Generate(), gameId, playerId, days);

    public void SetDays(double days) => Days = days;
}

public interface ITeamRepository : IRepository<Team, Guid>
{
    Task<IReadOnlyList<SprintCapacity>> GetCapacitiesAsync(IReadOnlyCollection<Guid> gameIds, CancellationToken cancellationToken = default);
    void AddCapacity(SprintCapacity capacity);
    Task<IReadOnlyList<TeamAnchor>> GetAnchorsAsync(Guid teamId, CancellationToken cancellationToken = default);
    void AddAnchor(TeamAnchor anchor);
    void RemoveAnchor(TeamAnchor anchor);
    Task<IReadOnlyList<TeamMember>> GetMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByMemberAsync(Guid playerId, CancellationToken cancellationToken = default);
    void AddMember(TeamMember member);
    void RemoveMember(TeamMember member);
}
