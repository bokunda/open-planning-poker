using OpenPlanningPoker.GameEngine.Application.Info.GetInfo;
using GameType = OpenPlanningPoker.GameEngine.GraphQL.Features.Games.Game;
using GamePlayerType = OpenPlanningPoker.GameEngine.GraphQL.Features.GamePlayer.GamePlayer;
using InfoType = OpenPlanningPoker.GameEngine.GraphQL.Features.Info.Info;
using SettingsType = OpenPlanningPoker.GameEngine.GraphQL.Features.Settings.Settings;
using TicketType = OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets.Ticket;
using VoteType = OpenPlanningPoker.GameEngine.GraphQL.Features.Votes.Vote;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features;

/// <summary>
/// Explicit Application response → GraphQL type mappings (replaces AutoMapper).
/// </summary>
public static class GraphQLMappings
{
    public static ApiCollection<TDestination> ToGraphQL<TSource, TDestination>(
        this Application.ApiCollection<TSource> collection,
        Func<TSource, TDestination> map) =>
        new([.. collection.Items.Select(map)], collection.TotalCount);

    public static GameType ToGraphQL(this GetGameResponse game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Description = game.Description,
        HostId = game.CreatedBy
    };

    public static GameType ToGraphQL(this CreateGameResponse game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        Description = game.Description,
        HostId = game.CreatedBy
    };

    public static GamePlayerType ToGraphQL(this ListPlayersItem player) => new() { Id = player.Id };

    public static InfoType ToGraphQL(this GetInfoResponse info) => new()
    {
        Version = info.Version,
        Author = info.Author,
        Contact = info.Contact
    };

    public static SettingsType ToGraphQL(this GetGameSettingsResponse settings) => ToSettings(settings.Id, settings.GameId, settings.DeckSetup);
    public static SettingsType ToGraphQL(this CreateGameSettingsResponse settings) => ToSettings(settings.Id, settings.GameId, settings.DeckSetup);
    public static SettingsType ToGraphQL(this UpdateGameSettingsResponse settings) => ToSettings(settings.Id, settings.GameId, settings.DeckSetup);

    public static TicketType ToGraphQL(this GetTicketResponse ticket) => ToTicket(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);
    public static TicketType ToGraphQL(this CreateTicketResponse ticket) => ToTicket(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);
    public static TicketType ToGraphQL(this UpdateTicketResponse ticket) => ToTicket(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);
    public static TicketType ToGraphQL(this GetTicketsItem ticket) => ToTicket(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);

    public static VoteType ToGraphQL(this GetVotesItem vote) => new() { Id = vote.Id, PlayerId = vote.PlayerId, Value = vote.Value };
    public static VoteType ToGraphQL(this CreateOrUpdateVoteResponse vote) => new() { Id = vote.Id, PlayerId = vote.PlayerId, Value = vote.Value };

    private static SettingsType ToSettings(Guid id, Guid gameId, string deckSetup) => new()
    {
        Id = id,
        GameId = gameId,
        DeckSetup = deckSetup
    };

    private static TicketType ToTicket(Guid id, Guid gameId, string name, string description, bool votesRevealed) => new()
    {
        Id = id,
        GameId = gameId,
        Name = name,
        Description = description,
        VotesRevealed = votesRevealed
    };
}
