namespace OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;

public sealed record ListPlayersItem(Guid Id, string Name, PlayerRole Role = PlayerRole.Player, bool IsCoHost = false);

public sealed record ListPlayersQuery(Guid GameId) : IRequest<Result<ApiCollection<ListPlayersItem>, ApplicationError>>;

public static class ListPlayers
{
    public class Validator : AbstractValidator<ListPlayersQuery>
    {
        public Validator()
        {
            RuleFor(x => x.GameId)
                .NotEmpty();
        }
    }

    public sealed class RequestHandler(IGamePlayerRepository gamePlayerRepository)
        : IRequestHandler<ListPlayersQuery, Result<ApiCollection<ListPlayersItem>, ApplicationError>>
    {
        public async Task<Result<ApiCollection<ListPlayersItem>, ApplicationError>> Handle(ListPlayersQuery request, CancellationToken cancellationToken = default)
        {
            var gamePlayers = await gamePlayerRepository.GetByGame(request.GameId, cancellationToken);

            var distinctPlayers = gamePlayers
                .DistinctBy(x => x.PlayerId)
                .Select(x => new ListPlayersItem(x.PlayerId, string.Empty, x.Role, x.IsCoHost))
                .ToList();

            return new ApiCollection<ListPlayersItem>(
                [.. distinctPlayers],
                distinctPlayers.Count);
        }
    }
}