namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Timer;

[QueryType]
public class TimerQueries
{
    /// <summary>
    /// Current voting timer of a game, for players who join while it runs. Null when there is none
    /// or the caller is not a player of the game.
    /// </summary>
    public async Task<GameTimer?> GetGameTimerAsync(
        [Service] IGameAccessService gameAccess,
        [Service] IConnectionMultiplexer redis,
        [Required] Guid gameId,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is not null)
        {
            return null;
        }

        var timer = await TimerStore.GetAsync(redis, gameId);
        if (timer is null) return null;

        timer.ServerTime = DateTime.UtcNow;
        timer.Running = timer.Running && timer.EndsAt > timer.ServerTime;
        return timer;
    }
}
