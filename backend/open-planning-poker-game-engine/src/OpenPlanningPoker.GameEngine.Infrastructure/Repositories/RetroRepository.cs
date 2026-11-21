namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class RetroRepository(OpenPlanningPokerGameEngineDbContext dbContext) : IRetroRepository
{
    public Task<Retro?> GetAsync(Guid retroId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Retro>().FirstOrDefaultAsync(r => r.Id == retroId, cancellationToken);

    public Task<Retro?> GetByGameAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        dbContext.Set<Retro>().FirstOrDefaultAsync(r => r.GameId == gameId, cancellationToken);

    public async Task<IReadOnlyList<Retro>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<Retro>().Where(r => r.TeamId == teamId).OrderBy(r => r.CreatedOn).ToListAsync(cancellationToken);

    public void Add(Retro retro) => dbContext.Set<Retro>().Add(retro);

    public async Task<IReadOnlyList<RetroCard>> GetCardsAsync(Guid retroId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<RetroCard>().Where(c => c.RetroId == retroId).OrderBy(c => c.CreatedOn).ToListAsync(cancellationToken);

    public Task<RetroCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken = default) =>
        dbContext.Set<RetroCard>().FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);

    public void AddCard(RetroCard card) => dbContext.Set<RetroCard>().Add(card);

    public void RemoveCard(RetroCard card) => dbContext.Set<RetroCard>().Remove(card);

    public async Task<IReadOnlyList<RetroVote>> GetVotesAsync(Guid retroId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<RetroVote>().Where(v => v.RetroId == retroId).ToListAsync(cancellationToken);

    public void AddVote(RetroVote vote) => dbContext.Set<RetroVote>().Add(vote);

    public void RemoveVote(RetroVote vote) => dbContext.Set<RetroVote>().Remove(vote);

    public async Task<IReadOnlyList<RetroActionItem>> GetActionItemsAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<RetroActionItem>().Where(i => i.TeamId == teamId).OrderBy(i => i.CreatedOn).ToListAsync(cancellationToken);

    public Task<RetroActionItem?> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken = default) =>
        dbContext.Set<RetroActionItem>().FirstOrDefaultAsync(i => i.Id == actionItemId, cancellationToken);

    public void AddActionItem(RetroActionItem item) => dbContext.Set<RetroActionItem>().Add(item);

    public void RemoveActionItem(RetroActionItem item) => dbContext.Set<RetroActionItem>().Remove(item);
}
