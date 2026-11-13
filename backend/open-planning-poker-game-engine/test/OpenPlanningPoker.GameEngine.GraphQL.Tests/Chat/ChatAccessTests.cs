using System.Text;
using HotChocolate;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenPlanningPoker.GameEngine.Application.Abstractions.Authorization;
using OpenPlanningPoker.GameEngine.GraphQL.Extensions;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;
using OpenPlanningPoker.Shared.Errors;

namespace OpenPlanningPoker.GameEngine.GraphQL.Tests.Chat;

/// <summary>OPP-26: only players of a game can read its chat, over HTTP and over the WebSocket.</summary>
public class ChatAccessTests
{
    private static readonly Guid GameId = Guid.NewGuid();

    [Theory]
    [InlineData("NotMember")]
    [InlineData("Unauthenticated")]
    public async Task ChatMessages_ForNonPlayers_IsEmpty(string errorCode)
    {
        // Redis is never touched for non-players, so none is passed.
        var messages = await new ChatQueries().GetChatMessagesAsync(new FakeGameAccess(new ApplicationError(errorCode)), null!, GameId);

        Assert.Empty(messages);
    }

    [Theory]
    [InlineData("NotMember")]
    [InlineData("Unauthenticated")]
    public async Task OnChatMessage_ForNonPlayers_DoesNotStart(string errorCode)
    {
        var receiver = new FakeReceiver();

        var error = await Assert.ThrowsAsync<GraphQLException>(() =>
            new ChatSubscriptions().SubscribeToChatAsync(GameId, new FakeGameAccess(new ApplicationError(errorCode)), receiver, default).AsTask());

        Assert.Equal(errorCode, error.Errors[0].Code);
        Assert.Null(receiver.Topic);
    }

    [Fact]
    public async Task OnChatMessage_ForPlayers_SubscribesToTheGameTopic()
    {
        var receiver = new FakeReceiver();

        await new ChatSubscriptions().SubscribeToChatAsync(GameId, new FakeGameAccess(null), receiver, default);

        Assert.Equal(ChatTopics.Messages(GameId), receiver.Topic);
    }

    [Fact]
    public async Task WebSocketToken_Valid_BecomesTheUser()
    {
        var userId = Guid.NewGuid();
        var options = BearerOptions("a-test-secret-that-is-long-enough-1234567890");
        var token = CreateToken("a-test-secret-that-is-long-enough-1234567890", userId);

        var user = await WebSocketAuthInterceptor.ValidateTokenAsync(options, token);

        Assert.NotNull(user);
        Assert.Equal(userId.ToString(), user!.FindFirst("id")?.Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WebSocketToken_ForgedOrGarbage_StaysAnonymous(bool forged)
    {
        var options = BearerOptions("a-test-secret-that-is-long-enough-1234567890");
        var token = forged ? CreateToken("another-secret-that-is-also-long-enough-0987", Guid.NewGuid()) : "not-a-token";

        Assert.Null(await WebSocketAuthInterceptor.ValidateTokenAsync(options, token));
    }

    private static JwtBearerOptions BearerOptions(string secret) => new()
    {
        TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidIssuer = "open-planning-poker",
            ValidAudience = "open-planning-poker",
        }
    };

    private static string CreateToken(string secret, Guid userId) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
    {
        Issuer = "open-planning-poker",
        Audience = "open-planning-poker",
        Expires = DateTime.UtcNow.AddHours(1),
        Claims = new Dictionary<string, object> { ["id"] = userId.ToString() },
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256),
    });

    private sealed class FakeReceiver : ITopicEventReceiver
    {
        public string? Topic { get; private set; }

        public ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(string topicName, CancellationToken cancellationToken = default)
        {
            Topic = topicName;
            return ValueTask.FromResult<ISourceStream<TMessage>>(null!);
        }

        public ValueTask<ISourceStream<TMessage>> SubscribeAsync<TMessage>(string topicName, int? bufferCapacity, TopicBufferFullMode? bufferFullMode, CancellationToken cancellationToken = default) =>
            SubscribeAsync<TMessage>(topicName, cancellationToken);
    }

    /// <summary>Answers every membership check with the given error (null = access granted).</summary>
    private sealed class FakeGameAccess(ApplicationError? memberError) : IGameAccessService
    {
        public Guid CurrentUserId => Guid.Empty;
        public ApplicationError? EnsureAuthenticated() => memberError;
        public Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult<ApplicationError?>(null);
        public Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult(memberError);
        public Task<ApplicationError?> EnsureOwnerAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult(memberError);
        public Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default) => Task.FromResult(memberError);
        public Task<(Guid GameId, ApplicationError? Error)> EnsureHostOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult((GameId, memberError));
        public Task<(Guid GameId, ApplicationError? Error)> EnsureMemberOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult((GameId, memberError));
        public Task<(Guid GameId, ApplicationError? Error)> EnsureVoterOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult((GameId, memberError));
        public Task<ApplicationError?> EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(memberError);
        public Task<ApplicationError?> EnsureTeamOwnerAsync(Guid teamId, CancellationToken cancellationToken = default) => Task.FromResult(memberError);
    }
}
