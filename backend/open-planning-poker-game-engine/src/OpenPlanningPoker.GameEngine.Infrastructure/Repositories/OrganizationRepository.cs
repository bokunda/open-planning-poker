namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class OrganizationRepository(OpenPlanningPokerGameEngineDbContext dbContext) : IOrganizationRepository
{
    public Task<Organization?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<Organization>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(Organization organization) => dbContext.Set<Organization>().Add(organization);

    public void Remove(Organization organization) => dbContext.Set<Organization>().Remove(organization);

    public async Task<IReadOnlyList<OrganizationMember>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<OrganizationMember>().Where(m => m.OrganizationId == organizationId).OrderBy(m => m.CreatedOn).ToListAsync(cancellationToken);

    public Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid playerId, CancellationToken cancellationToken = default) =>
        dbContext.Set<OrganizationMember>().FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.PlayerId == playerId, cancellationToken);

    public async Task<IReadOnlyList<Organization>> GetByMemberAsync(Guid playerId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Organization>()
            .Where(o => dbContext.Set<OrganizationMember>().Any(m => m.OrganizationId == o.Id && m.PlayerId == playerId))
            .OrderBy(o => o.Name).ToListAsync(cancellationToken);

    public void AddMember(OrganizationMember member) => dbContext.Set<OrganizationMember>().Add(member);

    public void RemoveMember(OrganizationMember member) => dbContext.Set<OrganizationMember>().Remove(member);

    public async Task<IReadOnlyList<Team>> GetTeamsAsync(Guid organizationId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Team>().Where(t => t.OrganizationId == organizationId).OrderBy(t => t.Name).ToListAsync(cancellationToken);
}
