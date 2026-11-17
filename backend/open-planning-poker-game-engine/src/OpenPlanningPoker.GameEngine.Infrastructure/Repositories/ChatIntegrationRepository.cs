using OpenPlanningPoker.GameEngine.Domain.Integrations;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Repositories;

public sealed class ChatIntegrationRepository(OpenPlanningPokerGameEngineDbContext dbContext) : IChatIntegrationRepository
{
    public Task<SlackInstallation?> GetSlackInstallationAsync(string workspaceId, CancellationToken cancellationToken = default) =>
        dbContext.Set<SlackInstallation>().FirstOrDefaultAsync(item => item.WorkspaceId == workspaceId, cancellationToken);

    public void AddSlackInstallation(SlackInstallation installation) => dbContext.Set<SlackInstallation>().Add(installation);

    public Task<ChatPost?> GetPostAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        dbContext.Set<ChatPost>().FirstOrDefaultAsync(item => item.GameId == gameId, cancellationToken);

    public void AddPost(ChatPost post) => dbContext.Set<ChatPost>().Add(post);
}
