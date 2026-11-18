namespace OpenPlanningPoker.GameEngine.Domain.Games;

public sealed class Game : Entity<Guid>
{
    private Game(string name, string description, Guid hostId, Guid? teamId)
    {
        Name = name;
        Description = description;
        HostId = hostId;
        TeamId = teamId;
    }

    public string Name { get; }
    public string Description { get; }

    /// <summary>
    /// The player who runs the game. Starts as the creator and can be handed over to another player.
    /// </summary>
    public Guid HostId { get; private set; }

    /// <summary>Team room the game belongs to (one sprint of the team), or null for a standalone game.</summary>
    public Guid? TeamId { get; private set; }

    public GameSettings.GameSettings GameSettings { get; set; } = null!;
    public ICollection<GamePlayer.GamePlayer>? GamePlayers { get; set; }
    public ICollection<Ticket>? Tickets { get; set; }

    public static Game Create(string name, string description, Guid hostId = default, Guid? teamId = null)
    {
        var game = new Game(name, description, hostId, teamId);
        game.RaiseDomainEvent(new CreateGameDomainEvent(game.Id));
        return game;
    }

    public void TransferHost(Guid playerId) => HostId = playerId;

    public const int DefaultSprintDays = 10;
    public const int MaxSprintDays = 30;

    /// <summary>Working days of the sprint planned in this session (OPP-146); null until the team sets it.</summary>
    public int? SprintDays { get; private set; }

    public void SetSprintDays(int? days) => SprintDays = days;

    /// <summary>
    /// SHA-256 of the one-time link that makes its opener the host, for games started from Slack or Teams
    /// (OPP-22, OPP-23), which have no host until then. Null otherwise.
    /// </summary>
    public string? HostClaimHash { get; private set; }
    public DateTimeOffset? HostClaimExpiresOn { get; private set; }

    /// <summary>A game started from a chat app: no host yet, the claim link decides who it is.</summary>
    public static Game CreateUnclaimed(string name, string description, string hostClaimHash, DateTimeOffset expiresOn)
    {
        var game = Create(name, description);
        game.HostClaimHash = hostClaimHash;
        game.HostClaimExpiresOn = expiresOn;
        return game;
    }

    /// <summary>The first valid claim makes the player the host; the link stops working after that.</summary>
    public bool TryClaimHost(string hostClaimHash, Guid playerId, DateTimeOffset now)
    {
        if (HostId != Guid.Empty || HostClaimHash is null || HostClaimExpiresOn < now) return false;
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(HostClaimHash), System.Text.Encoding.ASCII.GetBytes(hostClaimHash)))
        {
            return false;
        }
        HostId = playerId;
        HostClaimHash = null;
        HostClaimExpiresOn = null;
        return true;
    }
}
