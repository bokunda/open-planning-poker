using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>
/// Atlassian REST calls for the Jira integration: OAuth 2.0 (3LO) on auth.atlassian.com, and Jira Cloud through
/// api.atlassian.com/ex/jira/{cloudId} (platform API v3 and the Agile API for boards and sprints).
/// </summary>
public sealed class JiraApiClient(HttpClient http, IOptions<JiraOptions> options) : IJiraApi
{
    private const string AuthUrl = "https://auth.atlassian.com/oauth/token";
    private const string ApiUrl = "https://api.atlassian.com";
    private static readonly string[] StoryPointFieldNames = ["Story Points", "Story point estimate"];
    private readonly JiraOptions _options = options.Value;

    public Task<JiraTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken) =>
        TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        }, cancellationToken);

    public Task<JiraTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        TokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refreshToken,
        }, cancellationToken);

    public async Task<IReadOnlyList<JiraSite>> GetSitesAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var doc = await GetAsync(accessToken, $"{ApiUrl}/oauth/token/accessible-resources", cancellationToken);
        return [.. doc.RootElement.EnumerateArray()
            .Select(site => new JiraSite(Str(site, "id"), Str(site, "name"), Str(site, "url").TrimEnd('/')))];
    }

    public async Task<IReadOnlyList<JiraBoard>> GetBoardsAsync(string accessToken, string cloudId, CancellationToken cancellationToken)
    {
        using var doc = await GetAsync(accessToken, $"{Jira(cloudId)}/rest/agile/1.0/board?maxResults=100", cancellationToken);
        return [.. doc.RootElement.GetProperty("values").EnumerateArray()
            .Select(board => new JiraBoard(board.GetProperty("id").GetInt64(), Str(board, "name"), Str(board, "type")))];
    }

    public async Task<IReadOnlyList<JiraSprint>> GetSprintsAsync(string accessToken, string cloudId, long boardId, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await GetAsync(accessToken,
                $"{Jira(cloudId)}/rest/agile/1.0/board/{boardId}/sprint?state=active,future&maxResults=50", cancellationToken);
            return [.. doc.RootElement.GetProperty("values").EnumerateArray()
                .Select(sprint => new JiraSprint(sprint.GetProperty("id").GetInt64(), Str(sprint, "name"), Str(sprint, "state")))];
        }
        catch (JiraApiException ex) when (ex.Status == 400)
        {
            return []; // Kanban boards have no sprints.
        }
    }

    public async Task<IReadOnlyList<JiraField>> GetStoryPointFieldsAsync(string accessToken, string cloudId, CancellationToken cancellationToken)
    {
        using var doc = await GetAsync(accessToken, $"{Jira(cloudId)}/rest/api/3/field", cancellationToken);
        // Company-managed projects use "Story Points", team-managed ones "Story point estimate".
        return [.. doc.RootElement.EnumerateArray()
            .Select(field => new JiraField(Str(field, "id"), Str(field, "name")))
            .Where(field => StoryPointFieldNames.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(field => Array.FindIndex(StoryPointFieldNames, name => name.Equals(field.Name, StringComparison.OrdinalIgnoreCase)))];
    }

    public async Task<JiraIssuePage> SearchAsync(string accessToken, string cloudId, string siteUrl, string jql,
        IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken)
    {
        var body = new
        {
            jql,
            maxResults = max + 1,
            fields = new[] { "summary", "description" }.Concat(storyPointFields.Select(field => field.Id)).ToArray(),
        };
        using var doc = await SendAsync(accessToken, HttpMethod.Post, $"{Jira(cloudId)}/rest/api/3/search/jql", body, cancellationToken);
        return ToPage(doc.RootElement.GetProperty("issues"), siteUrl, storyPointFields, max, doc.RootElement.TryGetProperty("nextPageToken", out _));
    }

    public async Task<JiraIssuePage> GetBoardIssuesAsync(string accessToken, string cloudId, string siteUrl, long boardId,
        IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken)
    {
        var fields = string.Join(',', new[] { "summary", "description" }.Concat(storyPointFields.Select(field => field.Id)));
        var jql = Uri.EscapeDataString("statusCategory != Done ORDER BY Rank ASC");
        using var doc = await GetAsync(accessToken,
            $"{Jira(cloudId)}/rest/agile/1.0/board/{boardId}/issue?maxResults={max + 1}&fields={fields}&jql={jql}", cancellationToken);
        var total = doc.RootElement.TryGetProperty("total", out var t) ? t.GetInt32() : 0;
        return ToPage(doc.RootElement.GetProperty("issues"), siteUrl, storyPointFields, max, total > max);
    }

    public async Task<IReadOnlySet<string>> GetEditableFieldsAsync(string accessToken, string cloudId, string issueKey, CancellationToken cancellationToken)
    {
        using var doc = await GetAsync(accessToken, $"{Jira(cloudId)}/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/editmeta", cancellationToken);
        return doc.RootElement.GetProperty("fields").EnumerateObject().Select(field => field.Name).ToHashSet();
    }

    public async Task SetFieldAsync(string accessToken, string cloudId, string issueKey, string fieldId, double value, CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object> { ["fields"] = new Dictionary<string, object> { [fieldId] = value } };
        using var _ = await SendAsync(accessToken, HttpMethod.Put, $"{Jira(cloudId)}/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}", body, cancellationToken);
    }

    public async Task<(DateTimeOffset? Started, DateTimeOffset? Done)> GetIssueTimesAsync(string accessToken, string cloudId, string issueKey, CancellationToken cancellationToken)
    {
        using var doc = await SendAsync(accessToken, HttpMethod.Get,
            $"{Jira(cloudId)}/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}?fields=resolutiondate&expand=changelog", null, cancellationToken);
        var root = doc.RootElement;
        DateTimeOffset? done = root.TryGetProperty("fields", out var fields) && fields.TryGetProperty("resolutiondate", out var r)
            && r.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(r.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var resolved)
            ? resolved : null;
        DateTimeOffset? started = null;
        if (root.TryGetProperty("changelog", out var changelog) && changelog.TryGetProperty("histories", out var histories))
        {
            foreach (var history in histories.EnumerateArray())
            {
                var isStatus = history.TryGetProperty("items", out var items) && items.EnumerateArray()
                    .Any(item => item.TryGetProperty("field", out var f) && f.GetString() == "status");
                if (!isStatus || !history.TryGetProperty("created", out var c)) continue;
                if (DateTimeOffset.TryParse(c.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) && (started is null || at < started))
                {
                    started = at;
                }
            }
        }
        return (started, done);
    }

    public async Task AddCommentAsync(string accessToken, string cloudId, string issueKey, string text, CancellationToken cancellationToken)
    {
        using var _ = await SendAsync(accessToken, HttpMethod.Post,
            $"{Jira(cloudId)}/rest/api/3/issue/{Uri.EscapeDataString(issueKey)}/comment",
            new { body = AtlassianDocument.FromText(text) }, cancellationToken);
    }

    private static JiraIssuePage ToPage(JsonElement issues, string siteUrl, IReadOnlyList<JiraField> storyPointFields, int max, bool more)
    {
        var all = issues.EnumerateArray().Select(issue =>
        {
            var key = Str(issue, "key");
            var fields = issue.GetProperty("fields");
            double? points = null;
            foreach (var field in storyPointFields)
            {
                if (fields.TryGetProperty(field.Id, out var value) && value.ValueKind == JsonValueKind.Number)
                {
                    points = value.GetDouble();
                    break;
                }
            }
            var description = fields.TryGetProperty("description", out var d) ? AtlassianDocument.ToText(d) : string.Empty;
            return new JiraIssue(key, Str(fields, "summary"), description, points, $"{siteUrl}/browse/{key}");
        }).ToList();
        return new JiraIssuePage([.. all.Take(max)], more || all.Count > max);
    }

    private async Task<JiraTokens> TokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(AuthUrl, form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new JiraApiException((int)response.StatusCode, "Atlassian token request failed");
        }
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = doc.RootElement;
        var expiresIn = root.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 3600;
        return new JiraTokens(Str(root, "access_token"), Str(root, "refresh_token"), DateTimeOffset.UtcNow.AddSeconds(expiresIn));
    }

    private Task<JsonDocument> GetAsync(string accessToken, string url, CancellationToken cancellationToken) =>
        SendAsync(accessToken, HttpMethod.Get, url, null, cancellationToken);

    private async Task<JsonDocument> SendAsync(string accessToken, HttpMethod method, string url, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null) request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new JiraApiException((int)response.StatusCode, $"Jira answered {(int)response.StatusCode} for {method} {new Uri(url).AbsolutePath}");
        }
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
    }

    private static string Jira(string cloudId) => $"{ApiUrl}/ex/jira/{Uri.EscapeDataString(cloudId)}";

    private static string Str(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : element.TryGetProperty(name, out value) && value.ValueKind == JsonValueKind.Number
                ? value.GetRawText()
                : string.Empty;
}
