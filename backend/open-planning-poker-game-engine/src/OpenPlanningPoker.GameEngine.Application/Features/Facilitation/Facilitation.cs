namespace OpenPlanningPoker.GameEngine.Application.Features.Facilitation;

/// <summary>Room options for running the discussion (OPP-114).</summary>
/// <param name="Celebrations">Confetti when everyone agrees (OPP-172).</param>
/// <param name="Theme">Table and card-back look of the room (OPP-173).</param>
/// <param name="SeasonalCards">Card backs follow the season.</param>
public sealed record FacilitationOptions(
    bool OutlierNotes = true,
    int DiscussionMinutes = 0,
    bool ConfidenceCheck = false,
    bool Reactions = true,
    bool Celebrations = true,
    string Theme = Domain.GameSettings.GameSettings.DefaultTheme,
    bool SeasonalCards = true)
{
    public static FacilitationOptions From(Domain.GameSettings.GameSettings settings) =>
        new(settings.OutlierNotes, settings.DiscussionMinutes, settings.ConfidenceCheck, settings.Reactions,
            settings.Celebrations, settings.Theme, settings.SeasonalCards);
}

public sealed record VoteNoteItem(Guid PlayerId, string Note);

/// <summary>What the room needs after the reveal: discussion clock, notes of the outliers and the confidence check.</summary>
public sealed record TicketFacilitationResponse(
    Guid TicketId,
    DateTimeOffset? RevealedAt,
    DateTimeOffset? EstimatedAt,
    int VoteRounds,
    bool NeedsInfo,
    IReadOnlyList<VoteNoteItem> Notes,
    decimal? ConfidenceAverage,
    int ConfidenceCount,
    int? MyConfidence);

public sealed record GetTicketFacilitationQuery(Guid TicketId) : IRequest<Result<TicketFacilitationResponse, ApplicationError>>;
public sealed record SetVoteNoteCommand(Guid TicketId, string? Note) : IRequest<Result<TicketFacilitationResponse, ApplicationError>>;
public sealed record SetConfidenceCommand(Guid TicketId, int? Value) : IRequest<Result<TicketFacilitationResponse, ApplicationError>>;
public sealed record SetNeedsInfoCommand(Guid TicketId, bool NeedsInfo) : IRequest<Result<TicketFacilitationResponse, ApplicationError>>;
public sealed record UpdateFacilitationOptionsCommand(Guid GameId, FacilitationOptions Options) : IRequest<Result<FacilitationOptions, ApplicationError>>, IAuditedRequest
{
    public AuditEntry? ToAuditEntry() => AuditEntry.Game(AuditActions.SettingsFacilitation, AuditType.Update, GameId);
}

/// <summary>Facilitation options of a game; defaults when the game has no settings yet.</summary>
public sealed record GetFacilitationOptionsQuery(Guid GameId) : IRequest<FacilitationOptions>;

public static class FacilitationErrors
{
    public static ApplicationError NoVote => new("NoVote", "Vote on this ticket first.");
    public static ApplicationError NoFinalEstimate => new("NoFinalEstimate", "Choose the final estimate first.");
    public static ApplicationError InvalidConfidence => new("InvalidConfidence", "Confidence must be between 1 and 5.");
    public static ApplicationError InvalidDiscussionTime => new("InvalidDiscussionTime", "Choose 0, 2, 3 or 5 minutes.");
    public static ApplicationError NotRevealed => new("CardsNotRevealed", "Reveal the cards first.");
    public static ApplicationError UnknownTheme => new("UnknownTheme", "Choose one of the room themes.");
}

public static class TicketFacilitation
{
    public const int MinConfidence = 1;
    public const int MaxConfidence = 5;

    /// <summary>Builds the response from a ticket and its votes; notes only after the reveal.</summary>
    public static TicketFacilitationResponse Build(Ticket ticket, IEnumerable<Vote> votes, Guid currentUserId)
    {
        var list = votes.ToList();
        var notes = ticket.VotesRevealed
            ? list.Where(v => v.Note is not null).Select(v => new VoteNoteItem(v.PlayerId, v.Note!)).ToList()
            : [];
        var ratings = list.Where(v => v.Confidence is not null).Select(v => v.Confidence!.Value).ToList();
        decimal? average = ratings.Count > 0 ? decimal.Round((decimal)ratings.Average(), 1) : null;
        var mine = list.FirstOrDefault(v => v.PlayerId == currentUserId)?.Confidence;
        return new TicketFacilitationResponse(ticket.Id, ticket.RevealedAt, ticket.EstimatedAt, ticket.VoteRounds, ticket.NeedsInfo,
            notes, average, ratings.Count, mine);
    }

    public sealed class GetHandler(ITicketRepository tickets, IVoteRepository votes, ICurrentUserProvider currentUser)
        : IRequestHandler<GetTicketFacilitationQuery, Result<TicketFacilitationResponse, ApplicationError>>
    {
        public async Task<Result<TicketFacilitationResponse, ApplicationError>> Handle(GetTicketFacilitationQuery request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            return Build(ticket, await votes.GetByTicket(ticket.Id, null, cancellationToken), currentUser.Id);
        }
    }

    public sealed class NoteValidator : AbstractValidator<SetVoteNoteCommand>
    {
        public NoteValidator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Note).MaximumLength(Vote.MaxNoteLength);
        }
    }

    public sealed class NoteHandler(ITicketRepository tickets, IVoteRepository votes, ICurrentUserProvider currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<SetVoteNoteCommand, Result<TicketFacilitationResponse, ApplicationError>>
    {
        public async Task<Result<TicketFacilitationResponse, ApplicationError>> Handle(SetVoteNoteCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            if (!ticket.VotesRevealed)
            {
                return FacilitationErrors.NotRevealed;
            }

            var all = await votes.GetByTicket(ticket.Id, null, cancellationToken);
            var mine = all.FirstOrDefault(v => v.PlayerId == currentUser.Id);
            if (mine is null)
            {
                return FacilitationErrors.NoVote;
            }

            mine.SetNote(request.Note);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Build(ticket, all, currentUser.Id);
        }
    }

    public sealed class ConfidenceHandler(ITicketRepository tickets, IVoteRepository votes, ICurrentUserProvider currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<SetConfidenceCommand, Result<TicketFacilitationResponse, ApplicationError>>
    {
        public async Task<Result<TicketFacilitationResponse, ApplicationError>> Handle(SetConfidenceCommand request, CancellationToken cancellationToken)
        {
            if (request.Value is < MinConfidence or > MaxConfidence)
            {
                return FacilitationErrors.InvalidConfidence;
            }

            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            if (ticket.FinalEstimate is null)
            {
                return FacilitationErrors.NoFinalEstimate;
            }

            var all = await votes.GetByTicket(ticket.Id, null, cancellationToken);
            var mine = all.FirstOrDefault(v => v.PlayerId == currentUser.Id);
            if (mine is null)
            {
                return FacilitationErrors.NoVote;
            }

            mine.SetConfidence(request.Value);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Build(ticket, all, currentUser.Id);
        }
    }

    public sealed class NeedsInfoHandler(ITicketRepository tickets, IVoteRepository votes, ICurrentUserProvider currentUser, IUnitOfWork unitOfWork)
        : IRequestHandler<SetNeedsInfoCommand, Result<TicketFacilitationResponse, ApplicationError>>
    {
        public async Task<Result<TicketFacilitationResponse, ApplicationError>> Handle(SetNeedsInfoCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            ticket.MarkNeedsInfo(request.NeedsInfo);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Build(ticket, await votes.GetByTicket(ticket.Id, null, cancellationToken), currentUser.Id);
        }
    }

    public sealed class OptionsHandler(IGameSettingsRepository settingsRepository, IUnitOfWork unitOfWork, AuditContext audit)
        : IRequestHandler<UpdateFacilitationOptionsCommand, Result<FacilitationOptions, ApplicationError>>
    {
        public async Task<Result<FacilitationOptions, ApplicationError>> Handle(UpdateFacilitationOptionsCommand request, CancellationToken cancellationToken)
        {
            var o = request.Options;
            if (!Domain.GameSettings.GameSettings.DiscussionMinuteOptions.Contains(o.DiscussionMinutes))
            {
                return FacilitationErrors.InvalidDiscussionTime;
            }

            if (!Domain.GameSettings.GameSettings.Themes.Contains(o.Theme))
            {
                return FacilitationErrors.UnknownTheme;
            }

            var settings = await settingsRepository.GetByGame(request.GameId, cancellationToken);
            if (settings is null)
            {
                return new ApplicationError("GameSettingsNotFound", "Game settings not found.");
            }

            if (FacilitationOptions.From(settings) == o)
            {
                audit.MarkUnchanged();
            }

            settings.UpdateFacilitation(o.OutlierNotes, o.DiscussionMinutes, o.ConfidenceCheck, o.Reactions);
            settings.UpdateLook(o.Celebrations, o.Theme, o.SeasonalCards);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return FacilitationOptions.From(settings);
        }
    }

    public sealed class GetOptionsHandler(IGameSettingsRepository settingsRepository) : IRequestHandler<GetFacilitationOptionsQuery, FacilitationOptions>
    {
        public async Task<FacilitationOptions> Handle(GetFacilitationOptionsQuery request, CancellationToken cancellationToken)
        {
            var settings = await settingsRepository.GetByGame(request.GameId, cancellationToken);
            return settings is null ? new FacilitationOptions() : FacilitationOptions.From(settings);
        }
    }
}
