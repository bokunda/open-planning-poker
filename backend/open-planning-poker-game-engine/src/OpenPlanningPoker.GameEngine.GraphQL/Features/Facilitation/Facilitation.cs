using HotChocolate;
using HotChocolate.Execution;
using OpenPlanningPoker.GameEngine.Application.Features.Facilitation;
using OpenPlanningPoker.GameEngine.GraphQL.Features.Games;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Facilitation;

/// <summary>Room options for running the discussion (OPP-114).</summary>
public sealed class FacilitationSettings
{
    /// <summary>After a wide spread, the lowest and highest voters are asked to explain their card.</summary>
    public bool OutlierNotes { get; set; } = true;

    /// <summary>Minutes of discussion after the reveal (0 = off, 2, 3 or 5).</summary>
    public int DiscussionMinutes { get; set; }

    /// <summary>Players rate their confidence (1–5) in the final estimate.</summary>
    public bool ConfidenceCheck { get; set; }

    /// <summary>Players can send emoji reactions.</summary>
    public bool Reactions { get; set; } = true;

    /// <summary>A short confetti moment when everyone voted the same card (OPP-172).</summary>
    public bool Celebrations { get; set; } = true;

    /// <summary>Look of the table and the card backs: classic, forest, ocean, sunset, midnight or paper (OPP-173).</summary>
    public string Theme { get; set; } = Domain.GameSettings.GameSettings.DefaultTheme;

    /// <summary>Card backs follow the season.</summary>
    public bool SeasonalCards { get; set; } = true;

    public static FacilitationSettings From(FacilitationOptions o) => new()
    {
        OutlierNotes = o.OutlierNotes,
        DiscussionMinutes = o.DiscussionMinutes,
        ConfidenceCheck = o.ConfidenceCheck,
        Reactions = o.Reactions,
        Celebrations = o.Celebrations,
        Theme = o.Theme,
        SeasonalCards = o.SeasonalCards,
    };
}

public sealed class VoteNote
{
    public Guid PlayerId { get; set; }
    public string Note { get; set; } = string.Empty;
}

/// <summary>Discussion data of a ticket: when the cards were turned over, notes, needs-info flag and confidence.</summary>
public sealed class TicketFacilitationInfo
{
    public Guid TicketId { get; set; }
    public DateTimeOffset? RevealedAt { get; set; }
    public DateTimeOffset? EstimatedAt { get; set; }

    /// <summary>Times the cards were turned over (1 = agreed on the first vote).</summary>
    public int VoteRounds { get; set; }
    public bool NeedsInfo { get; set; }
    public List<VoteNote> Notes { get; set; } = [];

    /// <summary>Average confidence (1–5) of the players who rated; null when nobody did.</summary>
    public decimal? ConfidenceAverage { get; set; }
    public int ConfidenceCount { get; set; }

    /// <summary>The current user's own rating.</summary>
    public int? MyConfidence { get; set; }

    public static TicketFacilitationInfo From(TicketFacilitationResponse r) => new()
    {
        TicketId = r.TicketId,
        RevealedAt = r.RevealedAt,
        EstimatedAt = r.EstimatedAt,
        VoteRounds = r.VoteRounds,
        NeedsInfo = r.NeedsInfo,
        Notes = [.. r.Notes.Select(n => new VoteNote { PlayerId = n.PlayerId, Note = n.Note })],
        ConfidenceAverage = r.ConfidenceAverage,
        ConfidenceCount = r.ConfidenceCount,
        MyConfidence = r.MyConfidence,
    };
}

/// <summary>A short emoji reaction; broadcast to the room, never stored (OPP-125).</summary>
public sealed class Reaction
{
    public Guid GameId { get; set; }
    public Guid PlayerId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}

public static class Reactions
{
    /// <summary>The reactions players can send.</summary>
    public static readonly string[] Allowed = ["👍", "🤔", "☕", "🎉", "😂", "👀"];

    public static string Topic(Guid gameId) => $"Reaction_{gameId}";
}

[QueryType]
public class FacilitationQueries
{
    /// <summary>Discussion data of a ticket. Players of the game only.</summary>
    public async Task<FieldResult<TicketFacilitationInfo, ApplicationError>> GetTicketFacilitationAsync(
        [Service] ISender sender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        if ((await gameAccess.EnsureMemberOfTicketAsync(ticketId, cancellationToken)).Error is { } accessError)
        {
            return accessError;
        }

        var result = await sender.Send(new GetTicketFacilitationQuery(ticketId), cancellationToken);
        return result.IsSuccess ? TicketFacilitationInfo.From(result.Value!) : result.Error!;
    }
}

[MutationType]
public class FacilitationMutations
{
    /// <summary>Explains the current user's card after the reveal (up to 200 characters; empty clears it).</summary>
    public async Task<FieldResult<TicketFacilitationInfo, ApplicationError>> SetVoteNoteAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureVoterOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.SendChatMessage, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        return await Publish(eventSender, gameId, ticketId, await sender.Send(new SetVoteNoteCommand(ticketId, note), cancellationToken), cancellationToken);
    }

    /// <summary>Rates the current user's confidence (1–5) in the final estimate; null clears it.</summary>
    public async Task<FieldResult<TicketFacilitationInfo, ApplicationError>> SetConfidenceAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        int? value,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureVoterOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        return await Publish(eventSender, gameId, ticketId, await sender.Send(new SetConfidenceCommand(ticketId, value), cancellationToken), cancellationToken);
    }

    /// <summary>Marks a ticket as "needs more information" (or clears it). Host and co-hosts.</summary>
    public async Task<FieldResult<TicketFacilitationInfo, ApplicationError>> SetTicketNeedsInfoAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid ticketId,
        [Required] bool needsInfo,
        CancellationToken cancellationToken = default)
    {
        var (gameId, accessError) = await gameAccess.EnsureHostOfTicketAsync(ticketId, cancellationToken);
        if (accessError is not null)
        {
            return accessError;
        }

        return await Publish(eventSender, gameId, ticketId, await sender.Send(new SetNeedsInfoCommand(ticketId, needsInfo), cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Discussion and look options of the room. Host and co-hosts. Celebrations, theme and seasonal cards keep
    /// their current value when left out.
    /// </summary>
    public async Task<FieldResult<FacilitationSettings, ApplicationError>> UpdateFacilitationOptionsAsync(
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] bool outlierNotes,
        [Required] int discussionMinutes,
        [Required] bool confidenceCheck,
        [Required] bool reactions,
        bool? celebrations = null,
        string? theme = null,
        bool? seasonalCards = null,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureHostAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        var current = await sender.Send(new GetFacilitationOptionsQuery(gameId), cancellationToken);
        var result = await sender.Send(new UpdateFacilitationOptionsCommand(gameId,
            new FacilitationOptions(outlierNotes, discussionMinutes, confidenceCheck, reactions,
                celebrations ?? current.Celebrations, theme ?? current.Theme, seasonalCards ?? current.SeasonalCards)), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Game, cancellationToken: cancellationToken);
        return FacilitationSettings.From(result.Value!);
    }

    /// <summary>Sends an emoji reaction to everyone in the room. Not stored.</summary>
    public async Task<FieldResult<bool, ApplicationError>> SendReactionAsync(
        [Service] ActionRateLimiter rateLimiter,
        [Service] ISender sender,
        [Service] ITopicEventSender eventSender,
        [Service] IGameAccessService gameAccess,
        [Required] Guid gameId,
        [Required] string emoji,
        CancellationToken cancellationToken = default)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } accessError)
        {
            return accessError;
        }

        if (!Reactions.Allowed.Contains(emoji))
        {
            return new ApplicationError("InvalidReaction", "Unknown reaction.");
        }

        if (!(await sender.Send(new GetFacilitationOptionsQuery(gameId), cancellationToken)).Reactions)
        {
            return new ApplicationError("ReactionsOff", "Reactions are turned off in this room.");
        }

        if (rateLimiter.TryAcquire(ActionRateLimiter.Reaction, gameAccess.CurrentUserId) is { } rateLimitError)
        {
            return rateLimitError;
        }

        await eventSender.SendAsync(Reactions.Topic(gameId),
            new Reaction { GameId = gameId, PlayerId = gameAccess.CurrentUserId, Emoji = emoji }, cancellationToken);
        return true;
    }

    private static async Task<FieldResult<TicketFacilitationInfo, ApplicationError>> Publish(
        ITopicEventSender eventSender, Guid gameId, Guid ticketId, OpenPlanningPoker.Shared.Results.Result<TicketFacilitationResponse, ApplicationError> result, CancellationToken cancellationToken)
    {
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        await eventSender.PublishAsync(gameId, GameChangeKind.Tickets, ticketId: ticketId, cancellationToken: cancellationToken);
        return TicketFacilitationInfo.From(result.Value!);
    }
}

[SubscriptionType]
public class FacilitationSubscriptions
{
    /// <summary>Emoji reactions in a game. Only players of the game can subscribe.</summary>
    [Subscribe(With = nameof(SubscribeToReactionsAsync))]
    public Reaction OnReaction(Guid gameId, [EventMessage] Reaction reaction) => reaction;

    public async ValueTask<ISourceStream<Reaction>> SubscribeToReactionsAsync(
        Guid gameId,
        [Service] IGameAccessService gameAccess,
        [Service] ITopicEventReceiver receiver,
        CancellationToken cancellationToken)
    {
        if (await gameAccess.EnsureMemberAsync(gameId, cancellationToken) is { } error)
        {
            throw new GraphQLException(ErrorBuilder.New().SetMessage(error.Message ?? error.Code).SetCode(error.Code).Build());
        }

        return await receiver.SubscribeAsync<Reaction>(Reactions.Topic(gameId), cancellationToken);
    }
}
