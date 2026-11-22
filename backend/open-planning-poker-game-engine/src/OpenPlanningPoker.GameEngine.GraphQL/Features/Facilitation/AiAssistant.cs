using System.Text.Json;
using OpenPlanningPoker.GameEngine.Application.Features.Facilitation;
using OpenPlanningPoker.GameEngine.Domain.Abstractions;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Chat;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;
using OpenPlanningPoker.GameEngine.Domain.GamePlayer;
using OpenPlanningPoker.GameEngine.Domain.GameSettings;
using OpenPlanningPoker.GameEngine.Domain.Tickets;
using OpenPlanningPoker.GameEngine.Domain.Votes;
using TicketType = OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets.Ticket;
using DomainTicket = OpenPlanningPoker.GameEngine.Domain.Tickets.Ticket;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Facilitation;

/// <summary>Whether the AI assistant can be used in a room (OPP-139).</summary>
public sealed class AiAssistantStatus
{
    /// <summary>The server has an API key.</summary>
    public bool Available { get; set; }

    /// <summary>The host turned it on for this room.</summary>
    public bool Enabled { get; set; }
}

/// <summary>What the assistant said about a ticket: the clarity points (everyone sees them) and the host's summary.</summary>
public sealed class TicketAiInfo
{
    public List<string> Clarity { get; set; } = [];
    public string? Summary { get; set; }
}

public sealed class TicketClarity
{
    public List<string> Points { get; set; } = [];
}

public sealed class TicketSplitIdea
{
    public string Title { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
}

public sealed class TicketSplitIdeas
{
    public List<TicketSplitIdea> Items { get; set; } = [];
}

public sealed class TicketDiscussionSummary
{
    public string Summary { get; set; } = string.Empty;
}

[ExtendObjectType(typeof(TicketType))]
public sealed class TicketAiFields
{
    public async Task<TicketAiInfo> GetAiAsync([Parent] TicketType ticket, [Service] ITicketRepository tickets, CancellationToken cancellationToken)
    {
        var entity = await tickets.GetByIdAsync(ticket.Id, cancellationToken);
        return new TicketAiInfo
        {
            Clarity = entity?.ClarityNotes?.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList() ?? [],
            Summary = entity?.DiscussionSummary,
        };
    }
}

[QueryType]
public class AiAssistantQueries
{
    /// <summary>Is the assistant available on this server and on in this room? Players of the game.</summary>
    public async Task<FieldResult<AiAssistantStatus, ApplicationError>> GetAiAssistantAsync(
        [Service] IGameAccessService gameAccess, [Service] IGameSettingsRepository settings, [Service] AiAssistantService ai,
        [Required] Guid gameId, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } accessError) return accessError;
        var room = await settings.GetByGame(gameId, cancellationToken);
        return new AiAssistantStatus { Available = ai.Available, Enabled = ai.Available && room.AiAssistant };
    }
}

[MutationType]
public class AiAssistantMutations
{
    /// <summary>Turns the assistant on or off for the room. Host and co-hosts.</summary>
    public async Task<FieldResult<AiAssistantStatus, ApplicationError>> SetAiAssistantAsync(
        [Service] IGameAccessService gameAccess, [Service] IGameSettingsRepository settings, [Service] AiAssistantService ai,
        [Service] IUnitOfWork unitOfWork, [Service] ITopicEventSender eventSender,
        [Required] Guid gameId, [Required] bool enabled, CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError) return accessError;
        if (!ai.Available) return AiErrors.NotAvailable;
        var room = await settings.GetByGame(gameId, cancellationToken);
        room.SetAiAssistant(enabled);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        return new AiAssistantStatus { Available = true, Enabled = enabled };
    }

    /// <summary>Up to three points that are unclear in the ticket, shown to everyone in the room (OPP-150). Host and co-hosts.</summary>
    public async Task<FieldResult<TicketClarity, ApplicationError>> CheckTicketClarityAsync(
        [Service] IGameAccessService gameAccess, [Service] ActionRateLimiter rateLimiter, [Service] ITicketRepository tickets,
        [Service] IGameSettingsRepository settings, [Service] AiAssistantService ai, [Service] IUnitOfWork unitOfWork,
        [Service] ITopicEventSender eventSender, [Required] Guid ticketId, CancellationToken cancellationToken = default)
    {
        var (ticket, error) = await ReadyAsync(gameAccess, rateLimiter, tickets, settings, ai, ticketId, cancellationToken);
        if (error is not null) return error;
        var result = await ai.ClarityAsync(ticket!, cancellationToken);
        if (!result.IsSuccess) return result.Error!;
        ticket!.SetClarityNotes(result.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await eventSender.PublishAsync(ticket.GameId, GameChangeKind.Tickets, ticketId: ticket.Id, cancellationToken: cancellationToken);
        return new TicketClarity { Points = [.. result.Value!] };
    }

    /// <summary>Clears the clarity points of a ticket for everyone. Host and co-hosts.</summary>
    public async Task<FieldResult<bool, ApplicationError>> DismissTicketClarityAsync(
        [Service] IGameAccessService gameAccess, [Service] ITicketRepository tickets, [Service] IUnitOfWork unitOfWork,
        [Service] ITopicEventSender eventSender, [Required] Guid ticketId, CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null) return accessError;
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null) return GameAccessErrors.TicketNotFound;
        ticket.SetClarityNotes(null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: ticketId, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Two to four smaller tickets for one that is too big (OPP-151). Only the host sees them. Host and co-hosts.</summary>
    public async Task<FieldResult<TicketSplitIdeas, ApplicationError>> SuggestTicketSplitAsync(
        [Service] IGameAccessService gameAccess, [Service] ActionRateLimiter rateLimiter, [Service] ITicketRepository tickets,
        [Service] IGameSettingsRepository settings, [Service] AiAssistantService ai, [Required] Guid ticketId, CancellationToken cancellationToken = default)
    {
        var (ticket, error) = await ReadyAsync(gameAccess, rateLimiter, tickets, settings, ai, ticketId, cancellationToken);
        if (error is not null) return error;
        var result = await ai.SplitAsync(ticket!, cancellationToken);
        return result.IsSuccess
            ? new TicketSplitIdeas { Items = [.. result.Value!.Select(i => new TicketSplitIdea { Title = i.Title, Line = i.Line })] }
            : result.Error!;
    }

    /// <summary>
    /// A one-sentence summary of the voters' notes and the chat since the reveal (OPP-152), for the host to edit and keep.
    /// Player names are replaced before anything is sent. Host and co-hosts.
    /// </summary>
    public async Task<FieldResult<TicketDiscussionSummary, ApplicationError>> SummarizeTicketDiscussionAsync(
        [Service] IGameAccessService gameAccess, [Service] ActionRateLimiter rateLimiter, [Service] ITicketRepository tickets,
        [Service] IGameSettingsRepository settings, [Service] AiAssistantService ai, [Service] IVoteRepository votes,
        [Service] IGamePlayerRepository players, [Service] IUserService users, [Service] IConnectionMultiplexer redis,
        [Required] Guid ticketId, CancellationToken cancellationToken = default)
    {
        var (ticket, error) = await ReadyAsync(gameAccess, rateLimiter, tickets, settings, ai, ticketId, cancellationToken);
        if (error is not null) return error;

        var notes = (await votes.GetByTicket(ticket!.Id, null, cancellationToken)).Where(v => v.Note is not null).Select(v => v.Note!).ToList();
        var names = new List<string>();
        foreach (var player in await players.GetByGame(ticket.GameId, cancellationToken))
        {
            if ((await users.GetAsync($"{player.PlayerId}", cancellationToken))?.UserName is { Length: > 0 } name) names.Add(name);
        }

        // Chat since the reveal: the talk about this ticket.
        var since = ticket.RevealedAt ?? DateTimeOffset.MinValue;
        var chat = new List<string>();
        foreach (var value in await redis.GetDatabase().ListRangeAsync($"chat:{ticket.GameId}"))
        {
            try
            {
                var message = JsonSerializer.Deserialize<ChatMessage>((string)value!);
                if (message is not null && DateTimeOffset.TryParse(message.Timestamp, out var at) && at >= since) chat.Add(message.Content);
            }
            catch (JsonException) { /* skip malformed */ }
        }
        chat.Reverse();

        var result = await ai.SummaryAsync(ticket, notes, chat, names, cancellationToken);
        return result.IsSuccess ? new TicketDiscussionSummary { Summary = result.Value! } : result.Error!;
    }

    /// <summary>Keeps (or clears, with an empty text) the summary of a ticket; it goes into the tracker comment and the report. Host and co-hosts.</summary>
    public async Task<FieldResult<bool, ApplicationError>> SetTicketDiscussionSummaryAsync(
        [Service] IGameAccessService gameAccess, [Service] ITicketRepository tickets, [Service] IUnitOfWork unitOfWork,
        [Service] ITopicEventSender eventSender, [Required] Guid ticketId, string? summary, CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null) return accessError;
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null) return GameAccessErrors.TicketNotFound;
        ticket.SetDiscussionSummary(summary);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: ticketId, cancellationToken: cancellationToken);
        return true;
    }

    /// <summary>Host of the ticket's game, the assistant on in that room, and the caller within the AI rate limit.</summary>
    private static async Task<(DomainTicket? Ticket, ApplicationError? Error)> ReadyAsync(
        IGameAccessService gameAccess, ActionRateLimiter rateLimiter, ITicketRepository tickets, IGameSettingsRepository settings,
        AiAssistantService ai, Guid ticketId, CancellationToken cancellationToken)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null) return (null, accessError);
        if (!ai.Available) return (null, AiErrors.NotAvailable);
        if (!(await settings.GetByGame(gameId, cancellationToken)).AiAssistant) return (null, AiErrors.Off);
        if (rateLimiter.TryAcquire(ActionRateLimiter.AiAssist, gameAccess.CurrentUserId) is { } rateLimitError) return (null, rateLimitError);
        var ticket = await tickets.GetByIdAsync(ticketId, cancellationToken);
        return ticket is null ? (null, GameAccessErrors.TicketNotFound) : (ticket, null);
    }
}
