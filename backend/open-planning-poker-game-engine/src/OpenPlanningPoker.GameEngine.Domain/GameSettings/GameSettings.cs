namespace OpenPlanningPoker.GameEngine.Domain.GameSettings;

public sealed class GameSettings : Entity<Guid>
{
    internal GameSettings(Guid gameId, string deckSetup)
    {
        GameId = gameId;
        DeckSetup = deckSetup;
    }

    public Guid GameId { get; private set; }
    public string DeckSetup { get; private set; }

    /// <summary>Reveal the cards as soon as every player (not observers) has voted.</summary>
    public bool AutoReveal { get; private set; }

    /// <summary>
    /// While set and in the future, players vote on the tickets on their own before the meeting.
    /// Votes stay hidden until the host reveals each ticket.
    /// </summary>
    public DateTimeOffset? AsyncVotingEndsAt { get; private set; }

    /// <summary>After a wide spread, the lowest and highest voters are asked to explain (OPP-122).</summary>
    public bool OutlierNotes { get; private set; } = true;

    /// <summary>Minutes of discussion after the reveal before the room is asked to move on; 0 is off (OPP-123).</summary>
    public int DiscussionMinutes { get; private set; }

    /// <summary>Players rate their confidence in the final estimate (OPP-124).</summary>
    public bool ConfidenceCheck { get; private set; }

    /// <summary>Players can send short emoji reactions (OPP-125).</summary>
    public bool Reactions { get; private set; } = true;

    /// <summary>The room sorts tickets into card columns instead of voting on each (OPP-142).</summary>
    public bool SortingOpen { get; private set; }

    public void SetSortingOpen(bool open) => SortingOpen = open;

    /// <summary>A short confetti moment when everyone voted the same card (OPP-172).</summary>
    public bool Celebrations { get; private set; } = true;

    /// <summary>Look of the table and the card backs, the same for everyone in the room (OPP-173).</summary>
    public string Theme { get; private set; } = DefaultTheme;

    /// <summary>Card backs follow the season (for example winter holidays) unless the host turns it off.</summary>
    public bool SeasonalCards { get; private set; } = true;

    public const string DefaultTheme = "classic";
    public static readonly string[] Themes = ["classic", "forest", "ocean", "sunset", "midnight", "paper"];

    public void UpdateLook(bool celebrations, string theme, bool seasonalCards)
    {
        Celebrations = celebrations;
        Theme = theme;
        SeasonalCards = seasonalCards;
    }

    public static readonly int[] DiscussionMinuteOptions = [0, 2, 3, 5];

    public Game Game { get; set; } = null!;

    public static GameSettings Create(Guid gameId, string deckSetup)
    {
        var gameSettings = new GameSettings(gameId, deckSetup);
        gameSettings.RaiseDomainEvent(new CreateGameSettingsDomainEvent(gameSettings.Id));
        return gameSettings;
    }

    public void Update(Guid gameId, string deckSetup)
    {
        GameId = gameId;
        DeckSetup = deckSetup;
        RaiseDomainEvent(new CreateGameSettingsDomainEvent(Id));
    }

    public void UpdateOptions(bool autoReveal, DateTimeOffset? asyncVotingEndsAt)
    {
        AutoReveal = autoReveal;
        AsyncVotingEndsAt = asyncVotingEndsAt;
    }

    public void UpdateFacilitation(bool outlierNotes, int discussionMinutes, bool confidenceCheck, bool reactions)
    {
        OutlierNotes = outlierNotes;
        DiscussionMinutes = discussionMinutes;
        ConfidenceCheck = confidenceCheck;
        Reactions = reactions;
    }
}
