namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Timer;

/// <summary>
/// Voting timer of a game, shared by all players. The host starts and stops it.
/// Clients count down to <see cref="EndsAt"/> and use <see cref="ServerTime"/> to correct their clock.
/// </summary>
public class GameTimer
{
    public Guid GameId { get; set; }
    public int DurationSeconds { get; set; }
    public bool Running { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime ServerTime { get; set; }
}
