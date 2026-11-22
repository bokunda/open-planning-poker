using System.Net;
using OpenPlanningPoker.GameEngine.Application.Features.Teams;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.PublicApi;

public class WebhookSigningTests
{
    private const string Secret = "whsec_test";

    [Fact]
    public void Signature_Verifies_AndRejectsChangesAndOldRequests()
    {
        var now = DateTimeOffset.UtcNow;
        var body = """{"type":"ticket.estimated"}""";
        var header = WebhookSigning.Sign(Secret, now.ToUnixTimeSeconds(), body);

        header.ShouldStartWith($"t={now.ToUnixTimeSeconds()},v1=");
        WebhookSigning.Verify(Secret, header, body, now, TimeSpan.FromMinutes(5)).ShouldBeTrue();
        WebhookSigning.Verify(Secret, header, body + " ", now, TimeSpan.FromMinutes(5)).ShouldBeFalse();
        WebhookSigning.Verify("whsec_other", header, body, now, TimeSpan.FromMinutes(5)).ShouldBeFalse();
        WebhookSigning.Verify(Secret, header, body, now.AddMinutes(10), TimeSpan.FromMinutes(5)).ShouldBeFalse();
    }

    [Fact]
    public void Body_HasTheEnvelope()
    {
        var id = Guid.NewGuid();
        var body = WebhookSigning.Body("session.finished", id, Guid.Empty, new { points = 13 });
        body.ShouldContain($"\"id\":\"{id}\"");
        body.ShouldContain("\"type\":\"session.finished\"");
        body.ShouldContain("\"data\":{\"points\":13}");
    }
}

public class WebhookUrlTests
{
    [Theory]
    [InlineData("https://hooks.zapier.com/hooks/catch/1/abc", true)]
    [InlineData("https://n8n.example.com/webhook/opp", true)]
    [InlineData("http://example.com/hook", false)]
    [InlineData("https://localhost/hook", false)]
    [InlineData("https://127.0.0.1/hook", false)]
    [InlineData("https://10.0.0.3/hook", false)]
    [InlineData("https://192.168.1.10/hook", false)]
    [InlineData("https://169.254.169.254/latest/meta-data", false)]
    [InlineData("https://[::1]/hook", false)]
    [InlineData("https://user:pass@example.com/hook", false)]
    [InlineData("https://db.internal/hook", false)]
    [InlineData("not a url", false)]
    public void OnlyPublicHttps(string url, bool allowed) => TeamApiService.IsAllowedUrl(url).ShouldBe(allowed);

    [Theory]
    [InlineData("8.8.8.8", true)]
    [InlineData("100.64.0.1", false)]
    [InlineData("172.20.0.5", false)]
    [InlineData("::ffff:10.0.0.1", false)]
    [InlineData("fd00::1", false)]
    public void PublicAddresses(string ip, bool expected) => TeamApiService.IsPublic(IPAddress.Parse(ip)).ShouldBe(expected);

    [Fact]
    public void Tokens_AreStoredAsSha256Hex()
    {
        var hash = TeamApiService.Hash("opp_x");
        hash.Length.ShouldBe(64);
        hash.ShouldBe(hash.ToLowerInvariant());
        TeamApiService.Hash("opp_y").ShouldNotBe(hash);
    }
}
