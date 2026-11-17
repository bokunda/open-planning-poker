using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Integrations;

public static class ChatProviders
{
    public const string Slack = "slack";
    public const string Teams = "teams";
}

/// <summary>
/// The Open Planning Poker app installed in a Slack workspace (OPP-22). Only the workspace's bot token is
/// stored (encrypted); nothing is stored for people who only join a game.
/// </summary>
public sealed class SlackInstallation : Entity<Guid>
{
    private SlackInstallation(Guid id, string workspaceId) : base(id) => WorkspaceId = workspaceId;

    public string WorkspaceId { get; private set; }
    public string WorkspaceName { get; private set; } = string.Empty;
    public string ProtectedBotToken { get; private set; } = string.Empty;

    public static SlackInstallation Create(string workspaceId) => new(SequentialGuidGenerator.Generate(), workspaceId);

    public void Update(string workspaceName, string protectedBotToken)
    {
        WorkspaceName = workspaceName;
        ProtectedBotToken = protectedBotToken;
    }
}

/// <summary>
/// The chat message a game was announced in (Slack channel message, Teams conversation), so the result of
/// every reveal can be posted back to it (OPP-22, OPP-23).
/// </summary>
public sealed class ChatPost : Entity<Guid>
{
    private ChatPost(Guid id, Guid gameId, string provider, string workspaceId, string channelId) : base(id)
    {
        GameId = gameId;
        Provider = provider;
        WorkspaceId = workspaceId;
        ChannelId = channelId;
    }

    public Guid GameId { get; private set; }
    public string Provider { get; private set; }

    /// <summary>Slack workspace id, or the Teams tenant id.</summary>
    public string WorkspaceId { get; private set; }

    /// <summary>Slack channel id, or the Teams conversation id.</summary>
    public string ChannelId { get; private set; }

    /// <summary>Slack: the message ts the results are threaded under. Teams: unused.</summary>
    public string? MessageId { get; private set; }

    /// <summary>Teams: the Bot Connector service URL of the conversation.</summary>
    public string? ServiceUrl { get; private set; }

    public static ChatPost Create(Guid gameId, string provider, string workspaceId, string channelId, string? messageId, string? serviceUrl)
    {
        var post = new ChatPost(SequentialGuidGenerator.Generate(), gameId, provider, workspaceId, channelId);
        post.MessageId = messageId;
        post.ServiceUrl = serviceUrl;
        return post;
    }
}

public interface IChatIntegrationRepository
{
    Task<SlackInstallation?> GetSlackInstallationAsync(string workspaceId, CancellationToken cancellationToken = default);
    void AddSlackInstallation(SlackInstallation installation);
    Task<ChatPost?> GetPostAsync(Guid gameId, CancellationToken cancellationToken = default);
    void AddPost(ChatPost post);
}
