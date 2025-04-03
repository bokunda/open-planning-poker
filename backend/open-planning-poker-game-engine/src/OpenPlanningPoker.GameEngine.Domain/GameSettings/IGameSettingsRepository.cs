namespace OpenPlanningPoker.GameEngine.Domain.GameSettings;

public interface IGameSettingsRepository : IRepository<GameSettings, Guid>
{
    Task<GameSettings> GetByGame(Guid gameId, CancellationToken cancellationToken = default);
}