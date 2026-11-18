namespace OpenPlanningPoker.GameEngine.Domain.Games;

public interface IGameRepository : IRepository<Game, Guid>
{
    /// <summary>Games (sprints) of a team room, newest first.</summary>
    Task<IReadOnlyList<Game>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);

    /// <summary>The game whose recap is shared with this token (OPP-170), or null.</summary>
    Task<Game?> GetByRecapTokenAsync(string token, CancellationToken cancellationToken = default);
}