using OpenPlanningPoker.GameEngine.Domain.Shared;

namespace OpenPlanningPoker.GameEngine.Domain.Teams;

/// <summary>Steps of a retro (OPP-164, OPP-165). Each step only moves forward, started by the host.</summary>
public enum RetroPhase
{
    /// <summary>Everyone writes; each person sees only their own cards and a count per column.</summary>
    Writing,

    /// <summary>All cards are visible; the team merges similar ones into groups.</summary>
    Grouping,

    /// <summary>Each member places their dots; the dots of others stay hidden.</summary>
    Voting,

    /// <summary>The votes are shown, cards sort by votes and the team agrees on action items.</summary>
    Discussing,
}

/// <summary>
/// The retrospective of one sprint (OPP-162): it belongs to the team's planning session of that sprint, so the players,
/// the guest links, the host and the timer of that game are reused.
/// </summary>
public sealed class Retro : Entity<Guid>
{
    public const int MaxTemplateLength = 32;
    public const int MinDots = 1;
    public const int MaxDots = 10;

    /// <summary>Column titles are translated in the app; the template name picks them.</summary>
    public static readonly IReadOnlyDictionary<string, int> Templates = new Dictionary<string, int>
    {
        ["went-well"] = 3,          // Went well / To improve / Ideas
        ["start-stop-continue"] = 3,
        ["mad-sad-glad"] = 3,
        ["4ls"] = 4,                // Liked / Learned / Lacked / Longed for
    };

    private Retro(Guid id, Guid teamId, Guid gameId, string template) : base(id)
    {
        TeamId = teamId;
        GameId = gameId;
        Template = template;
    }

    public Guid TeamId { get; private set; }
    public Guid GameId { get; private set; }
    public string Template { get; private set; }
    public RetroPhase Phase { get; private set; } = RetroPhase.Writing;

    /// <summary>Authors are hidden unless the host turns this on before the cards are revealed.</summary>
    public bool ShowAuthors { get; private set; }
    public int DotsPerPerson { get; private set; } = 3;

    public int Columns => Templates[Template];

    public static Retro Create(Guid teamId, Guid gameId, string template) =>
        new(SequentialGuidGenerator.Generate(), teamId, gameId, template);

    /// <summary>Settings can change only while writing: names and dots are part of the rules everyone agreed to.</summary>
    public bool UpdateSettings(bool showAuthors, int dotsPerPerson)
    {
        if (Phase != RetroPhase.Writing) return false;
        ShowAuthors = showAuthors;
        DotsPerPerson = Math.Clamp(dotsPerPerson, MinDots, MaxDots);
        return true;
    }

    /// <summary>Moves to the next step; false when it is already past <paramref name="phase"/>.</summary>
    public bool MoveTo(RetroPhase phase)
    {
        if (phase <= Phase) return false;
        Phase = phase;
        return true;
    }
}

/// <summary>A note on the retro board. <see cref="Entity{TEntityId}.CreatedBy"/> is the author.</summary>
public sealed class RetroCard : Entity<Guid>
{
    public const int MaxTextLength = 500;
    public const int MaxGroupTitleLength = 100;

    private RetroCard(Guid id, Guid retroId, int column, string text) : base(id)
    {
        RetroId = retroId;
        Column = column;
        Text = text;
    }

    public Guid RetroId { get; private set; }
    public int Column { get; private set; }
    public string Text { get; private set; }

    /// <summary>The card this one was merged into; null for a card on its own or the head of a group.</summary>
    public Guid? GroupId { get; private set; }

    /// <summary>Title of the group this card heads; null when it heads none or has no title yet.</summary>
    public string? GroupTitle { get; private set; }

    public static RetroCard Create(Guid retroId, int column, string text) =>
        new(SequentialGuidGenerator.Generate(), retroId, column, text.Trim());

    public void Edit(string text) => Text = text.Trim();

    public void JoinGroup(Guid? headId, int column)
    {
        GroupId = headId;
        Column = column;
        if (headId is not null) GroupTitle = null;
    }

    public void SetGroupTitle(string? title) => GroupTitle = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
}

/// <summary>One dot of a member on a card or group head; several dots of one member may sit on the same card.</summary>
public sealed class RetroVote : Entity<Guid>
{
    private RetroVote(Guid id, Guid retroId, Guid cardId, Guid playerId) : base(id)
    {
        RetroId = retroId;
        CardId = cardId;
        PlayerId = playerId;
    }

    public Guid RetroId { get; private set; }
    public Guid CardId { get; private set; }
    public Guid PlayerId { get; private set; }

    public static RetroVote Create(Guid retroId, Guid cardId, Guid playerId) =>
        new(SequentialGuidGenerator.Generate(), retroId, cardId, playerId);
}

/// <summary>
/// Something the team will do (OPP-166). It stays open across retros of the team until it is marked done, so the next
/// retro starts with it and the team room lists it.
/// </summary>
public sealed class RetroActionItem : Entity<Guid>
{
    public const int MaxTextLength = 300;

    private RetroActionItem(Guid id, Guid teamId, Guid retroId, Guid? cardId, string text, Guid? ownerId) : base(id)
    {
        TeamId = teamId;
        RetroId = retroId;
        CardId = cardId;
        Text = text;
        OwnerId = ownerId;
    }

    public Guid TeamId { get; private set; }

    /// <summary>The retro it was agreed in.</summary>
    public Guid RetroId { get; private set; }
    public Guid? CardId { get; private set; }
    public string Text { get; private set; }
    public Guid? OwnerId { get; private set; }
    public bool Done { get; private set; }
    public DateTimeOffset? DoneOn { get; private set; }

    public static RetroActionItem Create(Guid teamId, Guid retroId, Guid? cardId, string text, Guid? ownerId) =>
        new(SequentialGuidGenerator.Generate(), teamId, retroId, cardId, text.Trim(), ownerId);

    public void Update(string text, Guid? ownerId)
    {
        Text = text.Trim();
        OwnerId = ownerId;
    }

    public void SetDone(bool done)
    {
        Done = done;
        DoneOn = done ? DateTimeOffset.UtcNow : null;
    }
}

public interface IRetroRepository
{
    Task<Retro?> GetAsync(Guid retroId, CancellationToken cancellationToken = default);
    Task<Retro?> GetByGameAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Retro>> GetByTeamAsync(Guid teamId, CancellationToken cancellationToken = default);
    void Add(Retro retro);

    Task<IReadOnlyList<RetroCard>> GetCardsAsync(Guid retroId, CancellationToken cancellationToken = default);
    Task<RetroCard?> GetCardAsync(Guid cardId, CancellationToken cancellationToken = default);
    void AddCard(RetroCard card);
    void RemoveCard(RetroCard card);

    Task<IReadOnlyList<RetroVote>> GetVotesAsync(Guid retroId, CancellationToken cancellationToken = default);
    void AddVote(RetroVote vote);
    void RemoveVote(RetroVote vote);

    Task<IReadOnlyList<RetroActionItem>> GetActionItemsAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<RetroActionItem?> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken = default);
    void AddActionItem(RetroActionItem item);
    void RemoveActionItem(RetroActionItem item);
}
