using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.GamePlayer;

/// <summary>What a person in the room does: votes, or only watches.</summary>
public enum PlayerRole
{
    Player = 0,
    Observer = 1,
}

public sealed class GamePlayer : Entity<Guid>
{
    private GamePlayer(Guid Id, Guid gameId, Guid playerId, PlayerRole role)
    {
        GameId = gameId;
        PlayerId = playerId;
        Role = role;
    }

    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }

    /// <summary>Observers see the table but cannot vote and are not counted for auto-reveal.</summary>
    public PlayerRole Role { get; private set; }

    /// <summary>Co-hosts can run the game (tickets, reveal, timer) next to the host.</summary>
    public bool IsCoHost { get; private set; }

    public Game Game { get; set; } = null!;

    public static GamePlayer Create(Guid gameId, Guid playerId, PlayerRole role = PlayerRole.Player) =>
        new(SequentialGuidGenerator.Generate(), gameId, playerId, role);

    public void SetRole(PlayerRole role) => Role = role;

    public void SetCoHost(bool isCoHost) => IsCoHost = isCoHost;
}
