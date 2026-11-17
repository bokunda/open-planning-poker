using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;
using OpenPlanningPoker.GameEngine.Domain.Abstractions;
using OpenPlanningPoker.GameEngine.Domain.Integrations;
using OpenPlanningPoker.Shared.Errors;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Integrations;

/// <summary>Jira integration (OPP-10, OPP-11, OPP-12) against a fake Atlassian API.</summary>
public class JiraServiceTests
{
    private static readonly Guid Player = Guid.NewGuid();
    private static readonly Guid GameId = Guid.NewGuid();

    private readonly FakeJiraApi _api = new();
    private readonly FakeConnections _connections = new();
    private readonly FakeTickets _tickets = new();
    private readonly FakeVotes _votes = new();

    private JiraService Service(bool configured = true) => new(
        Options.Create(new JiraOptions { ClientId = configured ? "client" : "", ClientSecret = configured ? "secret" : "" }),
        _api, new FakeProtector(), _connections, _tickets, _votes, new FakeUnitOfWork());

    private async Task ConnectAsync(JiraService service, DateTimeOffset? expires = null)
    {
        _api.Tokens = new JiraTokens("access-1", "refresh-1", expires ?? DateTimeOffset.UtcNow.AddHours(1));
        var url = service.CreateAuthorizeUrl(Player, "/game/abc").Value!;
        var state = Uri.UnescapeDataString(url.Split("state=")[1].Split('&')[0]);
        (await service.CompleteAsync(Player, "code", state, default)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task WithoutAnApp_TheIntegrationIsOff()
    {
        var service = Service(configured: false);
        (await service.GetStatusAsync(Player, default)).Enabled.ShouldBeFalse();
        service.CreateAuthorizeUrl(Player, "/").Error!.Code.ShouldBe("JiraNotConfigured");
    }

    [Fact]
    public async Task Connecting_StoresEncryptedTokens_AndPicksTheOnlySite()
    {
        var service = Service();
        var url = service.CreateAuthorizeUrl(Player, "/game/abc").Value!;
        url.ShouldStartWith("https://auth.atlassian.com/authorize?");
        url.ShouldContain("offline_access");

        await ConnectAsync(service);

        var connection = _connections.Items.Single();
        connection.ProtectedAccessToken.ShouldBe("enc:access-1");
        connection.CloudId.ShouldBe("cloud-1");
        var status = await service.GetStatusAsync(Player, default);
        status.Connected.ShouldBeTrue();
        status.SiteName.ShouldBe("Acme");
    }

    [Fact]
    public async Task TheConsentOfAnotherPlayer_IsRejected()
    {
        var service = Service();
        var url = service.CreateAuthorizeUrl(Guid.NewGuid(), "/").Value!;
        var state = Uri.UnescapeDataString(url.Split("state=")[1].Split('&')[0]);
        (await service.CompleteAsync(Player, "code", state, default)).Error!.Code.ShouldBe("JiraInvalidState");
        _connections.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnPath_StaysInsideTheApp()
    {
        var service = Service();
        _api.Tokens = new JiraTokens("a", "r", DateTimeOffset.UtcNow.AddHours(1));
        var url = service.CreateAuthorizeUrl(Player, "https://evil.example/").Value!;
        var state = Uri.UnescapeDataString(url.Split("state=")[1].Split('&')[0]);
        (await service.CompleteAsync(Player, "code", state, default)).Value!.ReturnPath.ShouldBe("/");
    }

    [Fact]
    public async Task Import_AddsLinkedTickets_AndSkipsIssuesAlreadyInTheGame()
    {
        var service = Service();
        await ConnectAsync(service);
        _api.Issues = [Issue("OPP-1", "Login page"), Issue("OPP-2", "Export CSV")];

        var first = await service.ImportAsync(Player, GameId, ["OPP-1", "opp-2"], default);
        first.Value!.Imported.ShouldBe(2);
        _tickets.Items.Select(t => t.Name).ShouldBe(["OPP-1 Login page", "OPP-2 Export CSV"]);
        _tickets.Items[0].ExternalUrl.ShouldBe("https://acme.atlassian.net/browse/OPP-1");
        _tickets.Items[1].Position.ShouldBe(1);

        var again = await service.ImportAsync(Player, GameId, ["OPP-1", "OPP-2"], default);
        again.Value!.Imported.ShouldBe(0);
        again.Value.Skipped.ShouldBe(2);
        _tickets.Items.Count.ShouldBe(2);

        var preview = await service.PreviewAsync(Player, GameId, null, 7, null, default);
        preview.Value!.Issues.ShouldAllBe(issue => issue.AlreadyInGame);
        _api.LastJql.ShouldBe("sprint = 7 ORDER BY Rank ASC");
    }

    [Fact]
    public async Task SaveEstimate_WritesTheFinalEstimateToTheEditableStoryPointsField()
    {
        var service = Service();
        await ConnectAsync(service);
        var ticket = Linked("OPP-9");
        ticket.RevealVotes();
        ticket.SetFinalEstimate("8");
        _votes.Items.AddRange([Vote.Create(Guid.NewGuid(), ticket.Id, "5"), Vote.Create(Guid.NewGuid(), ticket.Id, "8")]);
        _api.Editable = ["customfield_200"];

        var result = await service.SaveEstimateAsync(Player, ticket.Id, addComment: true, default);

        result.Value!.Value.ShouldBe("8");
        _api.Saved.ShouldBe(("OPP-9", "customfield_200", 8d));
        _api.Comment!.ShouldContain("Estimated with Open Planning Poker: 8");
    }

    [Fact]
    public async Task SaveEstimate_UsesTheMostCommonCard_WhenNoFinalEstimateWasChosen()
    {
        var service = Service();
        await ConnectAsync(service);
        var ticket = Linked("OPP-3");
        ticket.RevealVotes();
        _votes.Items.AddRange([Vote.Create(Guid.NewGuid(), ticket.Id, "3"), Vote.Create(Guid.NewGuid(), ticket.Id, "3"), Vote.Create(Guid.NewGuid(), ticket.Id, "5")]);

        (await service.SaveEstimateAsync(Player, ticket.Id, false, default)).Value!.Value.ShouldBe("3");
        _api.Comment.ShouldBeNull();
    }

    [Theory]
    [InlineData("M", "JiraNotNumeric")]
    [InlineData(null, "JiraNoEstimate")]
    public async Task SaveEstimate_SavesNothing_WithoutANumericEstimate(string? estimate, string code)
    {
        var service = Service();
        await ConnectAsync(service);
        var ticket = Linked("OPP-4");
        ticket.RevealVotes();
        ticket.SetFinalEstimate(estimate);

        (await service.SaveEstimateAsync(Player, ticket.Id, false, default)).Error!.Code.ShouldBe(code);
        _api.Saved.ShouldBeNull();
    }

    [Fact]
    public async Task SaveEstimate_ExplainsAMissingField()
    {
        var service = Service();
        await ConnectAsync(service);
        var ticket = Linked("OPP-5");
        ticket.RevealVotes();
        ticket.SetFinalEstimate("2");
        _api.Editable = ["summary"];

        (await service.SaveEstimateAsync(Player, ticket.Id, false, default)).Error!.Code.ShouldBe("JiraFieldMissing");
    }

    [Fact]
    public async Task AnExpiredToken_IsRefreshed_AndARejectedRefreshAsksToReconnect()
    {
        var service = Service();
        await ConnectAsync(service, expires: DateTimeOffset.UtcNow.AddMinutes(-5));
        _api.RefreshedTokens = new JiraTokens("access-2", "refresh-2", DateTimeOffset.UtcNow.AddHours(1));

        (await service.GetBoardsAsync(Player, default)).IsSuccess.ShouldBeTrue();
        _api.LastAccessToken.ShouldBe("access-2");
        _connections.Items.Single().ProtectedRefreshToken.ShouldBe("enc:refresh-2");

        _api.FailNextWith = 401;
        _api.RefreshFailsWith = 400;
        (await service.GetBoardsAsync(Player, default)).Error!.Code.ShouldBe("JiraReconnect");
        (await service.GetStatusAsync(Player, default)).NeedsReconnect.ShouldBeTrue();
    }

    [Fact]
    public async Task Disconnecting_DeletesTheTokens()
    {
        var service = Service();
        await ConnectAsync(service);
        (await service.DisconnectAsync(Player, default)).ShouldBeTrue();
        _connections.Items.ShouldBeEmpty();
        (await service.GetBoardsAsync(Player, default)).Error!.Code.ShouldBe("JiraNotConnected");
    }

    [Fact]
    public void AtlassianDocument_BecomesPlainText()
    {
        var json = """
            {"type":"doc","version":1,"content":[
              {"type":"paragraph","content":[{"type":"text","text":"Keep the cart"},{"type":"hardBreak"},{"type":"text","text":"for 30 days."}]},
              {"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","content":[{"type":"text","text":"signed-in users"}]}]}]}
            ]}
            """;
        AtlassianDocument.ToText(JsonDocument.Parse(json).RootElement).ShouldBe("Keep the cart\nfor 30 days.\n- signed-in users");
    }

    private Ticket Linked(string key)
    {
        var ticket = Ticket.Create(GameId, key + " Something", "", 0);
        ticket.LinkTo(key, $"https://acme.atlassian.net/browse/{key}");
        _tickets.Items.Add(ticket);
        return ticket;
    }

    private static JiraIssue Issue(string key, string summary) =>
        new(key, summary, "", null, $"https://acme.atlassian.net/browse/{key}");

    private sealed class FakeJiraApi : IJiraApi
    {
        public JiraTokens Tokens = new("a", "r", DateTimeOffset.UtcNow.AddHours(1));
        public JiraTokens? RefreshedTokens;
        public int? RefreshFailsWith;
        public int? FailNextWith;
        public List<JiraIssue> Issues = [];
        public HashSet<string> Editable = ["customfield_100"];
        public string? LastJql;
        public string? LastAccessToken;
        public (string, string, double)? Saved;
        public string? Comment;

        public Task<JiraTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(Tokens);

        public Task<JiraTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
            RefreshFailsWith is { } status ? throw new JiraApiException(status, "refresh") : Task.FromResult(RefreshedTokens ?? Tokens);

        public Task<IReadOnlyList<JiraSite>> GetSitesAsync(string accessToken, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JiraSite>>([new JiraSite("cloud-1", "Acme", "https://acme.atlassian.net")]);

        public Task<IReadOnlyList<JiraBoard>> GetBoardsAsync(string accessToken, string cloudId, CancellationToken cancellationToken)
        {
            LastAccessToken = accessToken;
            if (FailNextWith is { } status)
            {
                FailNextWith = null;
                throw new JiraApiException(status, "boards");
            }
            return Task.FromResult<IReadOnlyList<JiraBoard>>([new JiraBoard(1, "Team board", "scrum")]);
        }

        public Task<IReadOnlyList<JiraSprint>> GetSprintsAsync(string accessToken, string cloudId, long boardId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JiraSprint>>([]);

        public Task<IReadOnlyList<JiraField>> GetStoryPointFieldsAsync(string accessToken, string cloudId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<JiraField>>([new JiraField("customfield_100", "Story Points"), new JiraField("customfield_200", "Story point estimate")]);

        public Task<JiraIssuePage> SearchAsync(string accessToken, string cloudId, string siteUrl, string jql, IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken)
        {
            LastJql = jql;
            return Task.FromResult(new JiraIssuePage(Issues, false));
        }

        public Task<JiraIssuePage> GetBoardIssuesAsync(string accessToken, string cloudId, string siteUrl, long boardId, IReadOnlyList<JiraField> storyPointFields, int max, CancellationToken cancellationToken) =>
            Task.FromResult(new JiraIssuePage(Issues, false));

        public Task<IReadOnlySet<string>> GetEditableFieldsAsync(string accessToken, string cloudId, string issueKey, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(Editable);

        public Task SetFieldAsync(string accessToken, string cloudId, string issueKey, string fieldId, double value, CancellationToken cancellationToken)
        {
            Saved = (issueKey, fieldId, value);
            return Task.CompletedTask;
        }

        public Task AddCommentAsync(string accessToken, string cloudId, string issueKey, string text, CancellationToken cancellationToken)
        {
            Comment = text;
            return Task.CompletedTask;
        }
    }

    /// <summary>Readable "encryption" so the tests can see what is stored; the state is checked as the real one is.</summary>
    private sealed class FakeProtector : ISecretProtector
    {
        public string Protect(string value) => "enc:" + value;
        public string Unprotect(string value) => value["enc:".Length..];
        public string ProtectState(string value, TimeSpan lifetime) => "state:" + value;
        public string? UnprotectState(string value) => value.StartsWith("state:") ? value["state:".Length..] : null;
    }

    private sealed class FakeConnections : IJiraConnectionRepository
    {
        public readonly List<JiraConnection> Items = [];
        public Task<JiraConnection?> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(item => item.PlayerId == playerId));
        public void Add(JiraConnection connection) => Items.Add(connection);
        public void Remove(JiraConnection connection) => Items.Remove(connection);
    }

    private sealed class FakeTickets : ITicketRepository
    {
        public readonly List<Ticket> Items = [];
        public Task<IEnumerable<Ticket>> GetByGame(Guid gameId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(t => t.GameId == gameId));
        public Task<IEnumerable<Ticket>> GetByGames(IReadOnlyCollection<Guid> gameIds, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(t => gameIds.Contains(t.GameId)));
        public Task<IEnumerable<Ticket>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Ticket>>(Items);
        public Task<IEnumerable<Ticket>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(t => ids.Contains(t.Id)));
        public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(t => t.Id == id));
        public void Add(Ticket entity) => Items.Add(entity);
        public void AddRange(IEnumerable<Ticket> entities) => Items.AddRange(entities);
        public void Update(Ticket entity) { }
        public void Delete(Ticket entity) => Items.Remove(entity);
        public void DeleteRange(IEnumerable<Ticket> entities) { foreach (var e in entities.ToList()) Items.Remove(e); }
    }

    private sealed class FakeVotes : IVoteRepository
    {
        public readonly List<Vote> Items = [];
        public Task<ICollection<Vote>> GetByTicket(Guid ticketId, Guid? playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ICollection<Vote>>([.. Items.Where(v => v.TicketId == ticketId)]);
        public Task<ICollection<Vote>> GetByGame(Guid gameId, Guid? playerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ICollection<Vote>>([.. Items]);
        public Task<IEnumerable<Vote>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Vote>>(Items);
        public Task<IEnumerable<Vote>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(v => ids.Contains(v.Id)));
        public Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(v => v.Id == id));
        public void Add(Vote entity) => Items.Add(entity);
        public void AddRange(IEnumerable<Vote> entities) => Items.AddRange(entities);
        public void Update(Vote entity) { }
        public void Delete(Vote entity) => Items.Remove(entity);
        public void DeleteRange(IEnumerable<Vote> entities) { }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
