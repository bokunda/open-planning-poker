using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenPlanningPoker.GameEngine.Application.Features.Facilitation;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>Settings of the AI assistant (OPP-139). Without an API key the assistant is off and nothing is sent.</summary>
public sealed class AiOptions
{
    public const string Section = "Ai";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-opus-5-5";
    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>The Claude API through the official SDK, with JSON answers constrained to a schema.</summary>
public sealed class ClaudeAiModel(IOptions<AiOptions> options, ILogger<ClaudeAiModel> logger) : IAiModel
{
    private readonly AiOptions _options = options.Value;
    private AnthropicClient? _client;

    public bool Enabled => _options.Enabled;

    private AnthropicClient Client => _client ??= new AnthropicClient { ApiKey = _options.ApiKey };

    public async Task<string?> CompleteJsonAsync(string system, string prompt, IReadOnlyDictionary<string, JsonElement> schema, int maxTokens, CancellationToken cancellationToken)
    {
        if (!Enabled) return null;
        try
        {
            var response = await Client.Messages.Create(new MessageCreateParams
            {
                Model = _options.Model,
                MaxTokens = maxTokens,
                System = system,
                OutputConfig = new OutputConfig
                {
                    // Short answers to a clear question: low effort keeps them quick and cheap.
                    Effort = Effort.Low,
                    Format = new JsonOutputFormat { Schema = schema.ToDictionary(p => p.Key, p => p.Value) },
                },
                Messages = [new UserMessageParam { Content = prompt }],
            }, cancellationToken);

            if (response.StopReason == StopReason.Refusal)
            {
                logger.LogInformation("The AI assistant declined a request");
                return null;
            }

            return string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        }
        catch (AnthropicRateLimitException exception)
        {
            logger.LogWarning(exception, "The AI assistant hit the Claude API rate limit");
            return null;
        }
        catch (AnthropicApiException exception)
        {
            logger.LogWarning(exception, "The AI assistant request failed");
            return null;
        }
        catch (AnthropicIOException exception)
        {
            logger.LogWarning(exception, "The AI assistant could not reach the Claude API");
            return null;
        }
    }
}
