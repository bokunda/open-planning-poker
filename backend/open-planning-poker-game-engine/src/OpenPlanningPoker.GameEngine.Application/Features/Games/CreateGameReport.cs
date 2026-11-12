using OpenPlanningPoker.GameEngine.Application.Features.Games.Report;
using OpenPlanningPoker.Shared.Services;

namespace OpenPlanningPoker.GameEngine.Application.Features.Games;

public sealed record CreateGameReportResponse(string GameName, MemoryStream Stream);

public sealed record CreateGameReportCommand(Guid GameId) : IRequest<Result<CreateGameReportResponse, ApplicationError>>;

public static class CreateGameReport
{
    public sealed class RequestHandler(
        IGameRepository gameRepository,
        ITicketRepository ticketRepository,
        IVoteRepository voteRepository,
        IUserService userService)
        : IRequestHandler<CreateGameReportCommand, Result<CreateGameReportResponse, ApplicationError>>
    {
        public async Task<Result<CreateGameReportResponse, ApplicationError>> Handle(CreateGameReportCommand request, CancellationToken cancellationToken = default)
        {
            // TODO: Enhance this to use one query
            var game = await GetGameData(request.GameId, cancellationToken);
            var usernames = await GetUsernames(game, cancellationToken);

            var model = new ReportModel(
                game!.Name,
                game.Description,
                game.CreatedOn,
                DateTimeOffset.UtcNow,
                [.. game.Tickets!.Select(ticket => new ReportTicket(
                    ticket.Name,
                    ticket.Description,
                    ticket.VotesRevealed,
                    [.. (ticket.Votes ?? []).Select(vote => new ReportVote(usernames[vote.PlayerId], vote.Value))],
                    ticket.FinalEstimate))]);

            using var memoryStream = new MemoryStream();
            new GameReportDocument(model).GeneratePdf(memoryStream);

            return new CreateGameReportResponse(game!.Name, memoryStream);
        }

        private async Task<Dictionary<Guid, string>> GetUsernames(Game? game, CancellationToken cancellationToken)
        {
            var playerIds = game?.Tickets?
                .SelectMany(ticket => ticket.Votes ?? Enumerable.Empty<Vote>())
                .Select(vote => vote.PlayerId)
                .Distinct()
                .ToList();

            var usernames = new Dictionary<Guid, string>();
            foreach (var playerId in playerIds ?? Enumerable.Empty<Guid>())
            {
                var username = (await userService.GetAsync($"{playerId}", cancellationToken))?.UserName ?? "Former player";
                usernames.Add(playerId, username);
            }

            return usernames;
        }

        private async Task<Game?> GetGameData(Guid gameId, CancellationToken cancellationToken)
        {
            var game = await gameRepository.GetByIdAsync(gameId, cancellationToken);
            var tickets = await ticketRepository.GetByGame(gameId, cancellationToken);
            var votes = await voteRepository.GetByGame(gameId, null, cancellationToken);

            if (game is null || tickets is null || votes is null)
            {
                throw new Exception($"Game data is corrupted, gameId: {gameId}.");
            }

            foreach (var ticket in tickets)
            {
                // Unrevealed votes must not leak through the report.
                ticket.Votes = ticket.VotesRevealed
                    ? [.. votes.Where(vote => vote.TicketId == ticket.Id)]
                    : [];
            }
            game.Tickets = [.. tickets];
            return game;
        }
    }
}