using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Trackers;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>Shared HTTP helpers for the tracker clients.</summary>
public abstract class TrackerClientBase(HttpClient http)
{
    protected HttpClient Http { get; } = http;

    protected static string Query(IDictionary<string, string> values) =>
        string.Join('&', values.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));

    protected async Task<JsonElement> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await Http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new TrackerApiException((int)response.StatusCode, $"{request.RequestUri?.Host} answered {(int)response.StatusCode}");
        }

        return string.IsNullOrWhiteSpace(body) ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    protected static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? json = null, string? mediaType = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (json is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(json), Encoding.UTF8, mediaType ?? "application/json");
            if (mediaType is not null) request.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }
        return request;
    }

    protected async Task<TrackerTokens> TokenAsync(string url, Dictionary<string, string> form, string? accountName, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var json = await SendAsync(request, cancellationToken);
        if (!json.TryGetProperty("access_token", out var access) || access.GetString() is not { Length: > 0 } token)
        {
            throw new TrackerApiException(401, "no access token");
        }

        var refresh = json.TryGetProperty("refresh_token", out var r) ? r.GetString() : null;
        DateTimeOffset? expires = json.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? DateTimeOffset.UtcNow.AddSeconds(seconds) : null;
        return new TrackerTokens(token, refresh, expires, accountName);
    }

    protected static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    protected static double? Number(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    protected static string Trim(string text, int max) => text.Length <= max ? text : text[..max];
}

/// <summary>GitHub Issues (OPP-131): repositories the user can see; the estimate is an "estimate: N" label.</summary>
public sealed partial class GitHubTrackerClient(HttpClient http, IOptions<GitHubOptions> options) : TrackerClientBase(http), ITrackerClient
{
    private const string Api = "https://api.github.com";
    public const string LabelPrefix = "estimate: ";
    private readonly GitHubOptions _options = options.Value;

    [GeneratedRegex(@"^https://github\.com/([^/]+)/([^/]+)/issues/(\d+)$")]
    private static partial Regex IssueUrl();

    public string Provider => "github";
    public string DisplayName => "GitHub";
    public bool Enabled => _options.Enabled;
    public string QueryHint => "Labels, comma separated (optional)";

    public string AuthorizeUrl(string state) => "https://github.com/login/oauth/authorize?" + Query(new Dictionary<string, string>
    {
        ["client_id"] = _options.ClientId,
        ["redirect_uri"] = _options.RedirectUri,
        ["scope"] = "repo",
        ["state"] = state,
    });

    public async Task<TrackerTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var tokens = await TokenAsync("https://github.com/login/oauth/access_token", new()
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        }, null, cancellationToken);
        var user = await SendAsync(GitHub(HttpMethod.Get, $"{Api}/user", tokens.AccessToken), cancellationToken);
        return tokens with { AccountName = Text(user, "login") };
    }

    public async Task<TrackerTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        await TokenAsync("https://github.com/login/oauth/access_token", new()
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        }, null, cancellationToken);

    public async Task<IReadOnlyList<TrackerSource>> GetSourcesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var repos = await SendAsync(GitHub(HttpMethod.Get, $"{Api}/user/repos?per_page=100&sort=updated", accessToken), cancellationToken);
        return [.. repos.EnumerateArray().Where(r => r.TryGetProperty("has_issues", out var h) && h.GetBoolean())
            .Select(r => new TrackerSource(Text(r, "full_name"), Text(r, "full_name")))];
    }

    public async Task<IReadOnlyList<TrackerIssue>> GetIssuesAsync(string accessToken, string sourceId, string? query, int max, CancellationToken cancellationToken)
    {
        var labels = string.Join(',', (query ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        var url = $"{Api}/repos/{sourceId}/issues?state=open&per_page={Math.Min(max, 100)}" + (labels.Length > 0 ? $"&labels={Uri.EscapeDataString(labels)}" : string.Empty);
        var items = await SendAsync(GitHub(HttpMethod.Get, url, accessToken), cancellationToken);
        var repo = sourceId.Split('/').Last();
        return [.. items.EnumerateArray()
            .Where(i => !i.TryGetProperty("pull_request", out _))
            .Select(i => new TrackerIssue(
                Trim($"{repo}#{i.GetProperty("number").GetInt32()}", 64),
                Text(i, "title"),
                Text(i, "body"),
                Text(i, "html_url"),
                EstimateOf(i)))];
    }

    public string? CheckEstimate(string value) => null;

    public async Task SaveEstimateAsync(string accessToken, string issueKey, string issueUrl, string value, CancellationToken cancellationToken)
    {
        var match = IssueUrl().Match(issueUrl);
        if (!match.Success) throw new TrackerApiException(400, "not a GitHub issue link");
        var issue = $"{Api}/repos/{match.Groups[1].Value}/{match.Groups[2].Value}/issues/{match.Groups[3].Value}";
        var current = await SendAsync(GitHub(HttpMethod.Get, $"{issue}/labels", accessToken), cancellationToken);
        foreach (var label in current.EnumerateArray().Select(l => Text(l, "name")).Where(n => n.StartsWith(LabelPrefix.TrimEnd(), StringComparison.OrdinalIgnoreCase)))
        {
            await SendAsync(GitHub(HttpMethod.Delete, $"{issue}/labels/{Uri.EscapeDataString(label)}", accessToken), cancellationToken);
        }
        await SendAsync(GitHub(HttpMethod.Post, $"{issue}/labels", accessToken, new { labels = new[] { LabelPrefix + value } }), cancellationToken);
    }

    private static double? EstimateOf(JsonElement issue)
    {
        if (!issue.TryGetProperty("labels", out var labels)) return null;
        foreach (var name in labels.EnumerateArray().Select(l => Text(l, "name")))
        {
            if (name.StartsWith(LabelPrefix.TrimEnd(), StringComparison.OrdinalIgnoreCase)
                && double.TryParse(name[(name.IndexOf(':') + 1)..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            {
                return n;
            }
        }
        return null;
    }

    private static HttpRequestMessage GitHub(HttpMethod method, string url, string token, object? json = null)
    {
        var request = Request(method, url, token, json);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("OpenPlanningPoker", "1.0"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        return request;
    }
}

/// <summary>Linear (OPP-133): a team's active cycle (or open issues); the estimate is the issue's estimate field.</summary>
public sealed class LinearTrackerClient(HttpClient http, IOptions<LinearOptions> options) : TrackerClientBase(http), ITrackerClient
{
    private const string GraphQL = "https://api.linear.app/graphql";
    private const string IssueFields = "nodes { identifier title description url estimate }";
    private readonly LinearOptions _options = options.Value;

    public string Provider => "linear";
    public string DisplayName => "Linear";
    public bool Enabled => _options.Enabled;
    public string QueryHint => "Empty: the active cycle. \"open\": all open issues.";

    public string AuthorizeUrl(string state) => "https://linear.app/oauth/authorize?" + Query(new Dictionary<string, string>
    {
        ["client_id"] = _options.ClientId,
        ["redirect_uri"] = _options.RedirectUri,
        ["response_type"] = "code",
        ["scope"] = "read,write",
        ["state"] = state,
        ["actor"] = "user",
    });

    public async Task<TrackerTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var tokens = await TokenAsync("https://api.linear.app/oauth/token", new()
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
        }, null, cancellationToken);
        var viewer = await RunAsync(tokens.AccessToken, "query { viewer { name } }", null, cancellationToken);
        return tokens with { AccountName = Text(viewer.GetProperty("viewer"), "name") };
    }

    public async Task<TrackerTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        await TokenAsync("https://api.linear.app/oauth/token", new()
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refreshToken,
        }, null, cancellationToken);

    public async Task<IReadOnlyList<TrackerSource>> GetSourcesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var data = await RunAsync(accessToken, "query { teams(first: 100) { nodes { id name key } } }", null, cancellationToken);
        return [.. data.GetProperty("teams").GetProperty("nodes").EnumerateArray()
            .Select(t => new TrackerSource(Text(t, "id"), $"{Text(t, "name")} ({Text(t, "key")})"))];
    }

    public async Task<IReadOnlyList<TrackerIssue>> GetIssuesAsync(string accessToken, string sourceId, string? query, int max, CancellationToken cancellationToken)
    {
        var first = Math.Min(max, 100);
        JsonElement nodes;
        if (!string.Equals(query?.Trim(), "open", StringComparison.OrdinalIgnoreCase))
        {
            var cycle = await RunAsync(accessToken,
                $"query($id: String!) {{ team(id: $id) {{ activeCycle {{ issues(first: {first}) {{ {IssueFields} }} }} }} }}", new { id = sourceId }, cancellationToken);
            var active = cycle.GetProperty("team").GetProperty("activeCycle");
            if (active.ValueKind == JsonValueKind.Object) return Map(active.GetProperty("issues").GetProperty("nodes"));
        }

        var open = await RunAsync(accessToken,
            $"query($id: ID!) {{ issues(first: {first}, filter: {{ team: {{ id: {{ eq: $id }} }}, state: {{ type: {{ nin: [\"completed\", \"canceled\"] }} }} }}) {{ {IssueFields} }} }}",
            new { id = sourceId }, cancellationToken);
        nodes = open.GetProperty("issues").GetProperty("nodes");
        return Map(nodes);
    }

    /// <summary>Linear estimates are whole points.</summary>
    public string? CheckEstimate(string value) =>
        TrackerService.TryNumber(value, out var n) && n == Math.Floor(n) && n >= 0 ? null : "Linear estimates are whole numbers.";

    public async Task SaveEstimateAsync(string accessToken, string issueKey, string issueUrl, string value, CancellationToken cancellationToken)
    {
        TrackerService.TryNumber(value, out var n);
        var data = await RunAsync(accessToken, "mutation($id: String!, $estimate: Int!) { issueUpdate(id: $id, input: { estimate: $estimate }) { success } }",
            new { id = issueKey, estimate = (int)n }, cancellationToken);
        if (!data.GetProperty("issueUpdate").GetProperty("success").GetBoolean()) throw new TrackerApiException(400, "Linear did not save the estimate");
    }

    private static List<TrackerIssue> Map(JsonElement nodes) =>
        [.. nodes.EnumerateArray().Select(i => new TrackerIssue(Trim(Text(i, "identifier"), 64), Text(i, "title"), Text(i, "description"), Text(i, "url"), Number(i, "estimate")))];

    private async Task<JsonElement> RunAsync(string token, string query, object? variables, CancellationToken cancellationToken)
    {
        var json = await SendAsync(Request(HttpMethod.Post, GraphQL, token, new { query, variables }), cancellationToken);
        if (json.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
        {
            var authentication = errors.EnumerateArray().Any(e => e.TryGetProperty("extensions", out var x) && Text(x, "code") == "AUTHENTICATION_ERROR");
            throw new TrackerApiException(authentication ? 401 : 400, "Linear answered with errors");
        }
        return json.GetProperty("data");
    }
}

/// <summary>
/// Azure DevOps Boards (OPP-132): projects of every organization the user belongs to; work items from a WIQL query
/// or an iteration path; the estimate goes to Story Points, Effort or Size, whichever the work item type has.
/// </summary>
public sealed partial class AzureDevOpsTrackerClient(HttpClient http, IOptions<AzureDevOpsOptions> options) : TrackerClientBase(http), ITrackerClient
{
    private const string Scope = "499b84ac-1321-427f-aa17-267ca6975798/user_impersonation offline_access";
    private const string Version = "api-version=7.1";
    private static readonly string[] EstimateFields = ["Microsoft.VSTS.Scheduling.StoryPoints", "Microsoft.VSTS.Scheduling.Effort", "Microsoft.VSTS.Scheduling.Size"];
    private readonly AzureDevOpsOptions _options = options.Value;

    [GeneratedRegex(@"^https://dev\.azure\.com/([^/]+)/([^/]+)/_workitems/edit/(\d+)$")]
    private static partial Regex WorkItemUrl();

    public string Provider => "azure-devops";
    public string DisplayName => "Azure DevOps";
    public bool Enabled => _options.Enabled;
    public string QueryHint => "Iteration path (Project\\Sprint 4) or a WIQL query (optional)";

    public string AuthorizeUrl(string state) => "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?" + Query(new Dictionary<string, string>
    {
        ["client_id"] = _options.ClientId,
        ["response_type"] = "code",
        ["redirect_uri"] = _options.RedirectUri,
        ["response_mode"] = "query",
        ["scope"] = Scope,
        ["state"] = state,
    });

    public async Task<TrackerTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var tokens = await TokenAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", new()
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = Scope,
        }, null, cancellationToken);
        var me = await SendAsync(Request(HttpMethod.Get, $"https://app.vssps.visualstudio.com/_apis/profile/profiles/me?{Version}", tokens.AccessToken), cancellationToken);
        return tokens with { AccountName = Text(me, "displayName") };
    }

    public async Task<TrackerTokens?> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        await TokenAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", new()
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = Scope,
        }, null, cancellationToken);

    public async Task<IReadOnlyList<TrackerSource>> GetSourcesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var me = await SendAsync(Request(HttpMethod.Get, $"https://app.vssps.visualstudio.com/_apis/profile/profiles/me?{Version}", accessToken), cancellationToken);
        var accounts = await SendAsync(Request(HttpMethod.Get,
            $"https://app.vssps.visualstudio.com/_apis/accounts?memberId={Text(me, "id")}&{Version}", accessToken), cancellationToken);
        var sources = new List<TrackerSource>();
        foreach (var org in accounts.GetProperty("value").EnumerateArray().Select(a => Text(a, "accountName")).Take(10))
        {
            var projects = await SendAsync(Request(HttpMethod.Get, $"https://dev.azure.com/{org}/_apis/projects?{Version}", accessToken), cancellationToken);
            sources.AddRange(projects.GetProperty("value").EnumerateArray().Select(p => new TrackerSource($"{org}/{Text(p, "name")}", $"{org} / {Text(p, "name")}")));
        }
        return sources;
    }

    public async Task<IReadOnlyList<TrackerIssue>> GetIssuesAsync(string accessToken, string sourceId, string? query, int max, CancellationToken cancellationToken)
    {
        var (org, project) = Split(sourceId);
        var wiql = BuildWiql(query);
        var result = await SendAsync(Request(HttpMethod.Post,
            $"https://dev.azure.com/{org}/{Uri.EscapeDataString(project)}/_apis/wit/wiql?$top={Math.Min(max, 200)}&{Version}", accessToken, new { query = wiql }), cancellationToken);
        var ids = result.GetProperty("workItems").EnumerateArray().Select(w => w.GetProperty("id").GetInt32()).Take(max).ToList();
        if (ids.Count == 0) return [];

        var fields = string.Join(',', new[] { "System.Title", "System.Description" }.Concat(EstimateFields));
        var items = await SendAsync(Request(HttpMethod.Get,
            $"https://dev.azure.com/{org}/{Uri.EscapeDataString(project)}/_apis/wit/workitems?ids={string.Join(',', ids)}&fields={fields}&{Version}", accessToken), cancellationToken);
        return [.. items.GetProperty("value").EnumerateArray().Select(w =>
        {
            var id = w.GetProperty("id").GetInt32();
            var f = w.GetProperty("fields");
            var estimate = EstimateFields.Select(name => Number(f, name)).FirstOrDefault(n => n is not null);
            var url = $"https://dev.azure.com/{org}/{Uri.EscapeDataString(project)}/_workitems/edit/{id}";
            return new TrackerIssue($"#{id}", Text(f, "System.Title"), StripHtml(Text(f, "System.Description")), url, estimate);
        })];
    }

    public string? CheckEstimate(string value) =>
        TrackerService.TryNumber(value, out _) ? null : "Only numeric estimates can be saved in Azure DevOps.";

    public async Task SaveEstimateAsync(string accessToken, string issueKey, string issueUrl, string value, CancellationToken cancellationToken)
    {
        var match = WorkItemUrl().Match(issueUrl);
        if (!match.Success) throw new TrackerApiException(400, "not an Azure DevOps work item link");
        var (org, project, id) = (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
        var item = await SendAsync(Request(HttpMethod.Get, $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{id}?fields=System.WorkItemType&{Version}", accessToken), cancellationToken);
        var type = Text(item.GetProperty("fields"), "System.WorkItemType");
        var typeFields = await SendAsync(Request(HttpMethod.Get,
            $"https://dev.azure.com/{org}/{project}/_apis/wit/workitemtypes/{Uri.EscapeDataString(type)}/fields?{Version}", accessToken), cancellationToken);
        var available = typeFields.GetProperty("value").EnumerateArray().Select(f => Text(f, "referenceName")).ToHashSet();
        var field = EstimateFields.FirstOrDefault(available.Contains) ?? throw new TrackerApiException(400, "no estimate field");
        TrackerService.TryNumber(value, out var n);
        await SendAsync(Request(HttpMethod.Patch, $"https://dev.azure.com/{org}/{project}/_apis/wit/workitems/{id}?{Version}", accessToken,
            new[] { new { op = "add", path = $"/fields/{field}", value = n } }, "application/json-patch+json"), cancellationToken);
    }

    /// <summary>A WIQL query as is, an iteration path, or the project's open backlog items.</summary>
    public static string BuildWiql(string? query)
    {
        var q = query?.Trim() ?? string.Empty;
        if (q.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) return q;
        const string open = "SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = @project " +
            "AND [System.State] NOT IN ('Done', 'Closed', 'Removed') " +
            "AND [System.WorkItemType] IN ('User Story', 'Product Backlog Item', 'Requirement', 'Bug')";
        return q.Length == 0
            ? $"{open} ORDER BY [System.ChangedDate] DESC"
            : $"{open} AND [System.IterationPath] UNDER '{q.Replace("'", "''")}' ORDER BY [Microsoft.VSTS.Common.BacklogPriority]";
    }

    private static (string Org, string Project) Split(string sourceId)
    {
        var slash = sourceId.IndexOf('/');
        if (slash <= 0) throw new TrackerApiException(400, "source must be organization/project");
        return (sourceId[..slash], sourceId[(slash + 1)..]);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();

    private static string StripHtml(string html) => System.Net.WebUtility.HtmlDecode(Tags().Replace(html.Replace("<br>", "\n").Replace("</p>", "\n"), string.Empty)).Trim();
}
