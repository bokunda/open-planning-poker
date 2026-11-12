namespace OpenPlanningPoker.GameEngine.Application.Abstractions.Authorization;

public static class GameAccessErrors
{
    public static ApplicationError Unauthenticated => new("Unauthenticated", "A valid user token is required.");
    public static ApplicationError GameNotFound => new("GameNotFound", "Game not found.");
    public static ApplicationError TicketNotFound => new("TicketNotFound", "Ticket not found.");
    public static ApplicationError NotHost => new("NotHost", "Only the game host can perform this action.");
    public static ApplicationError NotMember => new("NotMember", "Join the game before performing this action.");
    public static ApplicationError Observer => new("Observer", "Observers cannot vote. Switch to player to vote.");
    public static ApplicationError PlayerNotFound => new("PlayerNotFound", "That player is not in this game.");
    public static ApplicationError TeamNotFound => new("TeamNotFound", "Team not found.");
    public static ApplicationError NotTeamMember => new("NotTeamMember", "Join the team before performing this action.");
    public static ApplicationError NotTeamOwner => new("NotTeamOwner", "Only the team owner can perform this action.");
}

/// <summary>
/// Server-side access checks for game actions. The host is <see cref="Game.HostId"/> (the creator until the role
/// is handed over); co-hosts can run the game next to the host; members are users who joined it.
/// Every method returns <c>null</c> when access is granted, otherwise the error to return.
/// </summary>
public interface IGameAccessService
{
    Guid CurrentUserId { get; }
    ApplicationError? EnsureAuthenticated();
    Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>Host or co-host: manages tickets, reveals, timer and settings.</summary>
    Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default);

    /// <summary>The host only: hands the game over, names co-hosts, deletes the game.</summary>
    Task<ApplicationError?> EnsureOwnerAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<(Guid GameId, ApplicationError? Error)> EnsureHostOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<(Guid GameId, ApplicationError? Error)> EnsureMemberOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>A member who votes (not an observer).</summary>
    Task<(Guid GameId, ApplicationError? Error)> EnsureVoterOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<ApplicationError?> EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<ApplicationError?> EnsureTeamOwnerAsync(Guid teamId, CancellationToken cancellationToken = default);
}

public sealed class GameAccessService(
    ICurrentUserProvider currentUserProvider,
    IGameRepository gameRepository,
    IGamePlayerRepository gamePlayerRepository,
    ITicketRepository ticketRepository,
    ITeamRepository teamRepository) : IGameAccessService
{
    public Guid CurrentUserId => currentUserProvider.Id;

    public ApplicationError? EnsureAuthenticated() =>
        CurrentUserId == Guid.Empty ? GameAccessErrors.Unauthenticated : null;

    public async Task<ApplicationError?> EnsureGameExistsAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        await gameRepository.GetByIdAsync(gameId, cancellationToken) is null ? GameAccessErrors.GameNotFound : null;

    public async Task<ApplicationError?> EnsureHostAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var (game, error) = await GetGameAsync(gameId, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        if (game!.HostId == CurrentUserId)
        {
            return null;
        }

        var players = await gamePlayerRepository.GetByGame(gameId, cancellationToken);
        return players.Any(player => player.PlayerId == CurrentUserId && player.IsCoHost) ? null : GameAccessErrors.NotHost;
    }

    public async Task<ApplicationError?> EnsureOwnerAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var (game, error) = await GetGameAsync(gameId, cancellationToken);
        return error ?? (game!.HostId == CurrentUserId ? null : GameAccessErrors.NotHost);
    }

    public async Task<ApplicationError?> EnsureMemberAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        var (game, error) = await GetGameAsync(gameId, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        if (game!.HostId == CurrentUserId)
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

    public async Task<(Guid GameId, ApplicationError? Error)> EnsureVoterOfTicketAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var (gameId, error) = await EnsureMemberOfTicketAsync(ticketId, cancellationToken);
        if (error is not null)
        {
            return (gameId, error);
        }

        var players = await gamePlayerRepository.GetByGame(gameId, cancellationToken);
        var isObserver = players.Any(player => player.PlayerId == CurrentUserId && player.Role == PlayerRole.Observer);
        return (gameId, isObserver ? GameAccessErrors.Observer : null);
    }

    public async Task<ApplicationError?> EnsureTeamMemberAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var (team, error) = await GetTeamAsync(teamId, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        if (team!.CreatedBy == CurrentUserId)
        {
            return null;
        }

        var members = await teamRepository.GetMembersAsync(teamId, cancellationToken);
        return members.Any(member => member.PlayerId == CurrentUserId) ? null : GameAccessErrors.NotTeamMember;
    }

    public async Task<ApplicationError?> EnsureTeamOwnerAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var (team, error) = await GetTeamAsync(teamId, cancellationToken);
        return error ?? (team!.CreatedBy == CurrentUserId ? null : GameAccessErrors.NotTeamOwner);
    }

    private async Task<(Game? Game, ApplicationError? Error)> GetGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        if (EnsureAuthenticated() is { } authError)
        {
            return (null, authError);
        }

        var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
        return game is null ? (null, GameAccessErrors.GameNotFound) : (game, null);
    }

    private async Task<(Team? Team, ApplicationError? Error)> GetTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        if (EnsureAuthenticated() is { } authError)
        {
            return (null, authError);
        }

        var team = await teamRepository.GetByIdAsync(teamId, cancellationToken);
        return team is null ? (null, GameAccessErrors.TeamNotFound) : (team, null);
    }
}
