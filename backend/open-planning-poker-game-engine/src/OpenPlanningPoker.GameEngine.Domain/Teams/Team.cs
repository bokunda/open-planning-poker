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

public interface ITeamRepository : IRepository<Team, Guid>
{
    Task<IReadOnlyList<TeamMember>> GetMembersAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Team>> GetByMemberAsync(Guid playerId, CancellationToken cancellationToken = default);
    void AddMember(TeamMember member);
    void RemoveMember(TeamMember member);
}
