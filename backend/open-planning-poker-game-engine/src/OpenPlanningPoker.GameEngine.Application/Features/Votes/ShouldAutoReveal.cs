namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

/// <summary>
/// True when the game has auto-reveal on, the ticket is still hidden, and every player who votes
/// (observers excluded) has a vote on it.
/// </summary>
public sealed record ShouldAutoRevealQuery(Guid TicketId) : IRequest<bool>;

public static class ShouldAutoReveal
{
    public sealed class RequestHandler(
        ITicketRepository tickets,
        IGameSettingsRepository settingsRepository,
        IGamePlayerRepository players,
        IVoteRepository votes)
        : IRequestHandler<ShouldAutoRevealQuery, bool>
    {
        public async Task<bool> Handle(ShouldAutoRevealQuery request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null || ticket.VotesRevealed)
            {
                return false;
            }

            var settings = await settingsRepository.GetByGame(ticket.GameId, cancellationToken);
            if (settings is null || !settings.AutoReveal)
            {
                return false;
            }

            var voters = (await players.GetByGame(ticket.GameId, cancellationToken))
                .Where(player => player.Role == PlayerRole.Player)
                .Select(player => player.PlayerId)
                .ToHashSet();
            if (voters.Count == 0)
            {
                return false;
            }

            var voted = (await votes.GetByTicket(ticket.Id, null, cancellationToken))
                .Select(vote => vote.PlayerId)
                .ToHashSet();
            return voters.IsSubsetOf(voted);
        }
    }
}
