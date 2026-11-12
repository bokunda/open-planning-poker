using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenPlanningPoker.Shared.Services;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace OpenPlanningPoker.GameEngine.IntegrationTests;

/// <summary>
/// Hosts the Game Engine against real PostgreSQL and Redis containers (Testcontainers). Migrations run on
/// start like in production. One instance is shared by all tests in the collection; every test creates its
/// own users and games, so tests do not depend on each other.
/// </summary>
public sealed class GameEngineFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Secret = "integration-tests-only-secret-0123456789abcdef";
    private const string Issuer = "open-planning-poker";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:15.5").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_database.StartAsync(), _redis.StartAsync());

        // Program reads configuration while it builds the host, so it is passed as environment variables.
        Environment.SetEnvironmentVariable("ConnectionStrings__Database", _database.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__Cache", _redis.GetConnectionString());
        Environment.SetEnvironmentVariable("Authentication__Secret", Secret);
        Environment.SetEnvironmentVariable("Otel__Endpoint", "http://127.0.0.1:4317");
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_database.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    /// <summary>A signed-in user, the way User Management creates one: a cached profile and a JWT.</summary>
    public async Task<GraphQLClient> CreateUserAsync(string name)
    {
        var user = await Services.GetRequiredService<IUserService>().AddAsync(name);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Issuer,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object> { ["id"] = user.Id.ToString() },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256),
        });

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new GraphQLClient(client, user.Id);
    }

    public GraphQLClient CreateAnonymous() => new(CreateClient(), Guid.Empty);
}

[CollectionDefinition(Name)]
public sealed class GameEngineCollection : ICollectionFixture<GameEngineFactory>
{
    public const string Name = "Game Engine";
}

public sealed class GraphQLClient(HttpClient http, Guid userId)
{
    public Guid UserId { get; } = userId;

    /// <summary>Runs a GraphQL operation and returns <c>data</c>; fails the test on transport-level GraphQL errors.</summary>
    public async Task<JsonElement> RunAsync(string query, object? variables = null)
    {
        var response = await http.PostAsJsonAsync("/graphql", new { query, variables });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (body.TryGetProperty("errors", out var errors))
        {
            throw new InvalidOperationException($"GraphQL errors: {errors}");
        }

        return body.GetProperty("data");
    }

    // Small helpers for the flows most tests need.

    public async Task<Guid> CreateGameAsync(string name = "Sprint 42")
    {
        var data = await RunAsync(
            "mutation($input: CreateGameInput!) { createGame(input: $input) { game { id } errors { ... on Error { message } } } }",
            new { input = new { name, description = "Integration test" } });
        return data.GetProperty("createGame").GetProperty("game").GetProperty("id").GetGuid();
    }

    public async Task JoinAsync(Guid gameId)
    {
        var data = await RunAsync(
            "mutation($input: JoinGameInput!) { joinGame(input: $input) { boolean errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId } });
        ErrorCode(data.GetProperty("joinGame")).ShouldBeNull();
    }

    public async Task<JsonElement> CreateTicketAsync(Guid gameId, string name = "Login page") =>
        (await RunAsync(
            "mutation($input: CreateTicketInput!) { createTicket(input: $input) { ticket { id } errors { ... on ApplicationError { code } } } }",
            new { input = new { gameId, name, description = "" } })).GetProperty("createTicket");

    public async Task<JsonElement> VoteAsync(Guid ticketId, string value) =>
        (await RunAsync(
            "mutation($input: CreateOrUpdateVoteInput!) { createOrUpdateVote(input: $input) { vote { id } errors { ... on ApplicationError { code } } } }",
            new { input = new { ticketId, value } })).GetProperty("createOrUpdateVote");

    public async Task<JsonElement> RevealAsync(Guid ticketId) =>
        (await RunAsync(
            "mutation($input: RevealVotesInput!) { revealVotes(input: $input) { errors { ... on ApplicationError { code } } } }",
            new { input = new { ticketId } })).GetProperty("revealVotes");

    public async Task<Dictionary<Guid, string>> VotesAsync(Guid ticketId)
    {
        var data = await RunAsync(
            "query($ticketId: UUID!) { votes(ticketId: $ticketId) { ... on ApiCollectionOfVote { items { playerId value } } } }",
            new { ticketId });
        return data.GetProperty("votes").GetProperty("items").EnumerateArray()
            .ToDictionary(vote => vote.GetProperty("playerId").GetGuid(), vote => vote.GetProperty("value").GetString()!);
    }

    /// <summary>The first error code of a mutation payload, or null when it succeeded.</summary>
    public static string? ErrorCode(JsonElement payload) =>
        payload.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0
            ? errors[0].GetProperty("code").GetString()
            : null;
}
