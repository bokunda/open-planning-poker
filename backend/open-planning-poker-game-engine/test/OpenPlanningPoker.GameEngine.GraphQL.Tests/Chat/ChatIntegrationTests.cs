using System.Security.Cryptography;
using System.Text;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Chat;
using OpenPlanningPoker.GameEngine.Domain.Games;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Integrations;
using Xunit;

namespace OpenPlanningPoker.GameEngine.GraphQL.Tests.Chat;

/// <summary>Slack request signing, host claims of chat games and the posted results (OPP-22, OPP-23).</summary>
public class ChatIntegrationTests
{
    private const string Secret = "8f742231b10e8888abcd99yyyzzz85a5";

    private static string Sign(string timestamp, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        return "v0=" + Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"v0:{timestamp}:{body}"))).ToLowerInvariant();
    }

    [Fact]
    public void SlackSignature_AcceptsASignedRecentRequest_AndRejectsTamperingAndReplays()
    {
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToUnixTimeSeconds().ToString();
        const string body = "team_id=T1&channel_id=C1&user_id=U1&text=Sprint+43";
        var signature = Sign(timestamp, body);

        Assert.True(SlackSignature.IsValid(Secret, timestamp, signature, body, now));
        Assert.False(SlackSignature.IsValid(Secret, timestamp, signature, body + "&x=1", now));
        Assert.False(SlackSignature.IsValid("other-secret", timestamp, signature, body, now));
        Assert.False(SlackSignature.IsValid(Secret, timestamp, signature, body, now.AddMinutes(6)));
        Assert.False(SlackSignature.IsValid(Secret, null, signature, body, now));
    }

    [Fact]
    public void HostClaim_WorksOnce_ForTheRightToken_BeforeItExpires()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("token")));
        var game = Game.CreateUnclaimed("Sprint 43", "", hash, DateTimeOffset.UtcNow.AddDays(7));
        var player = Guid.NewGuid();

        Assert.False(game.TryClaimHost(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("wrong"))), player, DateTimeOffset.UtcNow));
        Assert.True(game.TryClaimHost(hash, player, DateTimeOffset.UtcNow));
        Assert.Equal(player, game.HostId);
        Assert.False(game.TryClaimHost(hash, Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Equal(player, game.HostId);

        var expired = Game.CreateUnclaimed("Old", "", hash, DateTimeOffset.UtcNow.AddDays(-1));
        Assert.False(expired.TryClaimHost(hash, player, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ResultText_ShowsTheEstimateSpreadAndVotes()
    {
        Assert.Equal("*OPP-1 Login*: estimate *5* (close to consensus). Votes: 3, 5 ×2",
            ChatGameService.ResultText("OPP-1 Login", ["5", "3", "5"]));
        Assert.Equal("*Empty*: nobody voted.", ChatGameService.ResultText("Empty", []));
    }
}
