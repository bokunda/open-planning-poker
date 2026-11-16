using System.Text.Json;

namespace OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;

/// <summary>
/// Atlassian OAuth 2.0 (3LO) app (OPP-10). Without a client id and secret the integration is off and the app
/// hides it. The redirect URI must match the app's callback URL in the Atlassian developer console.
/// </summary>
public sealed class JiraOptions
{
    public const string Section = "Jira";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "https://app.openplanningpoker.com/integrations/jira/callback";
    public string Scopes { get; set; } = "read:jira-work write:jira-work read:jira-user offline_access";
    public bool Enabled => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed record JiraTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresOn);
public sealed record JiraSite(string CloudId, string Name, string Url);
public sealed record JiraBoard(long Id, string Name, string Type);
public sealed record JiraSprint(long Id, string Name, string State);

/// <summary>One issue as the import preview shows it. StoryPoints is the current estimate, if any.</summary>
public sealed record JiraIssue(string Key, string Summary, string Description, double? StoryPoints, string Url);

public sealed record JiraIssuePage(IReadOnlyList<JiraIssue> Issues, bool Truncated);

/// <summary>A Jira field that can hold story points on a site.</summary>
public sealed record JiraField(string Id, string Name);

/// <summary>Jira answered with an error status. Unauthorized means the access token is no longer valid.</summary>
public sealed class JiraApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
    public bool Unauthorized => Status == 401;
    public bool Forbidden => Status == 403;
}

/// <summary>Calls to Atlassian (auth.atlassian.com and api.atlassian.com).</summary>
public interface IJiraApi
{
    Task<JiraTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<JiraTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<JiraSite>> GetSitesAsync(string accessToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<JiraBoard>> GetBoardsAsync(string accessToken, string cloudId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JiraSprint>> GetSprintsAsync(string accessToken, string cloudId, long boardId, CancellationToken cancellationToken);
    Task<IReadOnlyList<JiraField>> GetStoryPointFieldsAsync(string accessToken, string cloudId, CancellationToken cancellationToken);
    Task<JiraIssuePage> SearchAsync(string accessToken, string cloudId, string siteUrl, string jql, IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken);
    Task<JiraIssuePage> GetBoardIssuesAsync(string accessToken, string cloudId, string siteUrl, long boardId, IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> GetEditableFieldsAsync(string accessToken, string cloudId, string issueKey, CancellationToken cancellationToken);
    Task SetFieldAsync(string accessToken, string cloudId, string issueKey, string fieldId, double value, CancellationToken cancellationToken);
    Task AddCommentAsync(string accessToken, string cloudId, string issueKey, string text, CancellationToken cancellationToken);
}

/// <summary>Encrypts tokens at rest and signs the short-lived OAuth state.</summary>
public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
    string ProtectState(string value, TimeSpan lifetime);

    /// <summary>The original value, or null when the state is forged or expired.</summary>
    string? UnprotectState(string value);
}

/// <summary>Plain text from Atlassian Document Format (descriptions), with paragraphs on their own lines.</summary>
public static class AtlassianDocument
{
    private static readonly HashSet<string> BlockTypes = ["paragraph", "heading", "listItem", "codeBlock", "blockquote", "rule", "tableRow"];

    public static string ToText(JsonElement document)
    {
        if (document.ValueKind == JsonValueKind.String) return document.GetString() ?? string.Empty;
        if (document.ValueKind != JsonValueKind.Object) return string.Empty;
        var text = new System.Text.StringBuilder();
        Append(document, text);
        return string.Join('\n', text.ToString().Split('\n').Select(line => line.TrimEnd())).Trim();
    }

    private static void Append(JsonElement node, System.Text.StringBuilder text)
    {
        var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;
        if (type == "text" && node.TryGetProperty("text", out var value)) text.Append(value.GetString());
        else if (type == "hardBreak") text.Append('\n');
        else if (type == "mention" && node.TryGetProperty("attrs", out var attrs) && attrs.TryGetProperty("text", out var mention)) text.Append(mention.GetString());
        if (type == "listItem") text.Append("- ");
        if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray()) Append(child, text);
        }
        if (type is not null && BlockTypes.Contains(type) && (text.Length == 0 || text[^1] != '\n')) text.Append('\n');
    }

    /// <summary>A comment body in Atlassian Document Format with one paragraph per line.</summary>
    public static object FromText(string text) => new
    {
        type = "doc",
        version = 1,
        content = text.Split('\n').Select(line => new
        {
            type = "paragraph",
            content = new object[] { new { type = "text", text = line } }
        }).ToArray()
    };
}
