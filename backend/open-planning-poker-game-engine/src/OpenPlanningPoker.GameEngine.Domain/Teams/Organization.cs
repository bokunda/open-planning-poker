using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Teams;

public enum OrganizationRole
{
    Member,
    Admin,
}

/// <summary>
/// A company with several team rooms (OPP-154). People join with the invite link; admins see every team of the
/// organization and its numbers. Team owners keep their rights over their own team.
/// </summary>
public sealed class Organization : Entity<Guid>
{
    public const int MaxNameLength = 120;

    private Organization(Guid id, string name, string inviteToken) : base(id)
    {
        Name = name;
        InviteToken = inviteToken;
    }

    public string Name { get; private set; }

    /// <summary>Secret part of the invite link; a new one stops old links from working.</summary>
    public string InviteToken { get; private set; }

    public static Organization Create(string name, string inviteToken) => new(SequentialGuidGenerator.Generate(), name.Trim(), inviteToken);

    public void Rename(string name) => Name = name.Trim();

    public void SetInviteToken(string token) => InviteToken = token;
}

public sealed class OrganizationMember : Entity<Guid>
{
    private OrganizationMember(Guid id, Guid organizationId, Guid playerId, OrganizationRole role) : base(id)
    {
        OrganizationId = organizationId;
        PlayerId = playerId;
        Role = role;
    }

    public Guid OrganizationId { get; private set; }
    public Guid PlayerId { get; private set; }
    public OrganizationRole Role { get; private set; }

    public static OrganizationMember Create(Guid organizationId, Guid playerId, OrganizationRole role) =>
        new(SequentialGuidGenerator.Generate(), organizationId, playerId, role);

    public void SetRole(OrganizationRole role) => Role = role;
}

public interface IOrganizationRepository
{
    Task<Organization?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Organization organization);
    void Remove(Organization organization);

    Task<IReadOnlyList<OrganizationMember>> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMember?> GetMemberAsync(Guid organizationId, Guid playerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Organization>> GetByMemberAsync(Guid playerId, CancellationToken cancellationToken = default);
    void AddMember(OrganizationMember member);
    void RemoveMember(OrganizationMember member);

    Task<IReadOnlyList<Team>> GetTeamsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
