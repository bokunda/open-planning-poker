namespace OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;

public sealed record ListPlayersItem(Guid Id, string Name);

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

            return new ApiCollection<ListPlayersItem>(
                [.. gamePlayers.Select(x => new ListPlayersItem(x.PlayerId, "TODO"))], 
                gamePlayers.Count);
        }
    }
}