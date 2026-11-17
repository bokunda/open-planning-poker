using System.Net.Http.Json;
using OpenPlanningPoker.GameEngine.Application.Features.Teams;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>Posts a plain text message to a Slack or Microsoft Teams incoming webhook (OPP-136).</summary>
public sealed class WebhookPoster(HttpClient http) : IWebhookPoster
{
    public async Task<bool> PostAsync(string url, string text, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(url, new { text }, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
