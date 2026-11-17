namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TeamRepository(OpenPlanningPokerGameEngineDbContext dbContext)
    : Repository<Team, Guid>(dbContext), ITeamRepository
{
    public async Task<IReadOnlyList<TeamMember>> GetMembersAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<TeamMember>()
            .Where(member => member.TeamId == teamId)
            .OrderBy(member => member.CreatedOn)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Team>> GetByMemberAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<Team>()
            .Where(team => DbContext.Set<TeamMember>().Any(member => member.TeamId == team.Id && member.PlayerId == playerId))
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TeamAnchor>> GetAnchorsAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await DbContext.Set<TeamAnchor>()
            .Where(anchor => anchor.TeamId == teamId)
            .OrderBy(anchor => anchor.CreatedOn)
            .ToListAsync(cancellationToken);

    public void AddAnchor(TeamAnchor anchor) => DbContext.Set<TeamAnchor>().Add(anchor);

    public void RemoveAnchor(TeamAnchor anchor) => DbContext.Set<TeamAnchor>().Remove(anchor);

    public void AddMember(TeamMember member) => DbContext.Set<TeamMember>().Add(member);

    public void RemoveMember(TeamMember member) => DbContext.Set<TeamMember>().Remove(member);
}
