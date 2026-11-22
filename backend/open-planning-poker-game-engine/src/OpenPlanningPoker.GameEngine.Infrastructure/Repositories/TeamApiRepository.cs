namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class TeamApiRepository(OpenPlanningPokerGameEngineDbContext dbContext) : ITeamApiRepository
{
    public Task<TeamApiToken?> GetTokenByHashAsync(string hash, CancellationToken cancellationToken = default) =>
        dbContext.Set<TeamApiToken>().FirstOrDefaultAsync(t => t.Hash == hash, cancellationToken);

    public Task<TeamApiToken?> GetTokenAsync(Guid tokenId, CancellationToken cancellationToken = default) =>
        dbContext.Set<TeamApiToken>().FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken);

    public async Task<IReadOnlyList<TeamApiToken>> GetTokensAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TeamApiToken>().Where(t => t.TeamId == teamId).ToListAsync(cancellationToken);

    public void AddToken(TeamApiToken token) => dbContext.Set<TeamApiToken>().Add(token);

    public async Task<IReadOnlyList<TeamWebhook>> GetWebhooksAsync(Guid teamId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TeamWebhook>().Where(w => w.TeamId == teamId).OrderBy(w => w.CreatedOn).ToListAsync(cancellationToken);

    public Task<TeamWebhook?> GetWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default) =>
        dbContext.Set<TeamWebhook>().FirstOrDefaultAsync(w => w.Id == webhookId, cancellationToken);

    public void AddWebhook(TeamWebhook webhook) => dbContext.Set<TeamWebhook>().Add(webhook);

    public void RemoveWebhook(TeamWebhook webhook) => dbContext.Set<TeamWebhook>().Remove(webhook);

    public async Task<IReadOnlyList<TeamWebhookDelivery>> GetDeliveriesAsync(Guid webhookId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<TeamWebhookDelivery>().Where(d => d.WebhookId == webhookId).OrderByDescending(d => d.CreatedOn).ToListAsync(cancellationToken);

    public void AddDelivery(TeamWebhookDelivery delivery) => dbContext.Set<TeamWebhookDelivery>().Add(delivery);

    public void RemoveDeliveries(IEnumerable<TeamWebhookDelivery> deliveries) => dbContext.Set<TeamWebhookDelivery>().RemoveRange(deliveries);
}
