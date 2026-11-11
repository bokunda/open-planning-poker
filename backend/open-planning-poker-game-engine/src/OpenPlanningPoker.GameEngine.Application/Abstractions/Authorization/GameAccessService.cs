namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Authorization;

public static class GameAccessErrors
{
    public static ApplicationError Unauthenticated => new("Unauthenticated", "A valid user token is required.");
    public static ApplicationError GameNotFound => new("GameNotFound", "Game not found.");
    public static ApplicationError TicketNotFound => new("TicketNotFound", "Ticket not found.");
    public static ApplicationError NotHost => new("NotHost", "Only the game host can perform this action.");
    public static ApplicationError NotMember => new("NotMember", "Join the game before performing this action.");
}

/// <summary>
/// Server-side access checks for game actions. The host is the user who created the game
/// (<see cref="Entity{TEntityId}.CreatedBy"/>); members are users who joined it.
/// Every method returns <c>null</c> when access is granted, otherwise the error to return.
/// </summary>
public interface IGameAccessService
{
    Guid CurrentUserId { get; }
    ApplicationError? EnsureAuthenticated();
    Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<(Guid GameId, ApplicationError? Error)> EnsureHostOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<(Guid GameId, ApplicationError? Error)> EnsureMemberOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
}

public sealed class GameAccessService(
    ICurrentUserProvider currentUserProvider,
    IGameRepository gameRepository,
    IGamePlayerRepository gamePlayerRepository,
    ITicketRepository ticketRepository) : IGameAccessService
{
    public Guid CurrentUserId => currentUserProvider.Id;

    public ApplicationError? EnsureAuthenticated() =>
        CurrentUserId == Guid.Empty ? GameAccessErrors.Unauthenticated : null;

    public async Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        await gameRepository.GetByIdAsync(gameId, cancellationToken) is null ? GameAccessErrors.GameNotFound : null;

    public async Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (EnsureAuthenticated() is { } authError)
        {
            return authError;
        }

        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game is null)
        {
            return GameAccessErrors.GameNotFound;
        }

        return game.CreatedBy == CurrentUserId ? null : GameAccessErrors.NotHost;
    }

    public async Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (EnsureAuthenticated() is { } authError)
        {
            return authError;
        }

        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
        if (game is null)
        {
            return GameAccessErrors.GameNotFound;
        }

        if (game.CreatedBy == CurrentUserId)
        {
            return null;
        }

        var players = await gamePlayerRepository.GetByGame(gameId, cancellationToken);
        return players.Any(player => player.PlayerId == CurrentUserId) ? null : GameAccessErrors.NotMember;
    }

    public async Task<(Guid GameId, ApplicationError? Error)> EnsureHostOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        return ticket is null
            ? (Guid.Empty, GameAccessErrors.TicketNotFound)
            : (ticket.GameId, await EnsureHostAsync(ticket.GameId, cancellationToken));
    }

    public async Task<(Guid GameId, ApplicationError? Error)> EnsureMemberOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        return ticket is null
            ? (Guid.Empty, GameAccessErrors.TicketNotFound)
            : (ticket.GameId, await EnsureMemberAsync(ticket.GameId, cancellationToken));
    }
}
