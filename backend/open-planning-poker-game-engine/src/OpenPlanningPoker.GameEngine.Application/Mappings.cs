using OpenPlanningPoker.GameEngine.Application.Features.Games;
using OpenPlanningPoker.GameEngine.Application.Features.GameSettings;
using OpenPlanningPoker.GameEngine.Application.Features.Tickets;
using OpenPlanningPoker.GameEngine.Application.Features.Votes;

namespace OpenPlanningPoker.GameEngine.Application;

/// <summary>
/// Explicit domain → response mappings (replaces AutoMapper). Plain code: checked by the
/// compiler, no reflection, and a renamed property cannot be silently left unmapped.
/// </summary>
public static class Mappings
{
    public static CreateGameResponse ToCreateGameResponse(this Game game) =>
        new(game.Id, game.Name, game.Description, game.CreatedBy);

    public static GetGameResponse ToGetGameResponse(this Game game) =>
        new(game.Id, game.Name, game.Description, game.CreatedBy);

    public static CreateGameSettingsResponse ToCreateGameSettingsResponse(this Domain.GameSettings.GameSettings settings) =>
        new(settings.Id, settings.GameId, settings.DeckSetup);

    public static GetGameSettingsResponse ToGetGameSettingsResponse(this Domain.GameSettings.GameSettings settings) =>
        new(settings.Id, settings.GameId, settings.DeckSetup);

    public static UpdateGameSettingsResponse ToUpdateGameSettingsResponse(this Domain.GameSettings.GameSettings settings) =>
        new(settings.Id, settings.GameId, settings.DeckSetup);

    public static CreateTicketResponse ToCreateTicketResponse(this Ticket ticket) =>
        new(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);

    public static GetTicketResponse ToGetTicketResponse(this Ticket ticket) =>
        new(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);

    public static GetTicketsItem ToGetTicketsItem(this Ticket ticket) =>
        new(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);

    public static UpdateTicketResponse ToUpdateTicketResponse(this Ticket ticket) =>
        new(ticket.Id, ticket.GameId, ticket.Name, ticket.Description, ticket.VotesRevealed);

    public static ImportTicketItemResponse ToImportTicketItemResponse(this Ticket ticket) =>
        new(ticket.Name, ticket.Description);

    public static CreateOrUpdateVoteResponse ToCreateOrUpdateVoteResponse(this Vote vote) =>
        new(vote.Id, vote.PlayerId, vote.TicketId, vote.Value);

    public static GetVotesItem ToGetVotesItem(this Vote vote) =>
        new(vote.Id, vote.PlayerId, vote.Value);
}
