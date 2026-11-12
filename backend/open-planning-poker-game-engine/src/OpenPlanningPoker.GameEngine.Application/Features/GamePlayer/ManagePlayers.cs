namespace OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;

/// <summary>Switches a player between voting and observing. Access is checked by the caller.</summary>
public sealed record SetPlayerRoleCommand(Guid GameId, Guid PlayerId, PlayerRole Role) : IRequest<Result<bool, ApplicationError>>;

/// <summary>Makes a player co-host (or a regular player again). Access is checked by the caller.</summary>
public sealed record SetCoHostCommand(Guid GameId, Guid PlayerId, bool IsCoHost) : IRequest<Result<bool, ApplicationError>>;

/// <summary>Removes a player from the game. Their votes stay on the tickets.</summary>
public sealed record RemovePlayerCommand(Guid GameId, Guid PlayerId) : IRequest<Result<bool, ApplicationError>>;

/// <summary>Hands the game over to another player. The previous host stays in the game as a player.</summary>
public sealed record TransferHostCommand(Guid GameId, Guid PlayerId) : IRequest<Result<bool, ApplicationError>>;

public static class ManagePlayers
{
    public sealed class SetPlayerRoleValidator : AbstractValidator<SetPlayerRoleCommand>
    {
        public SetPlayerRoleValidator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.PlayerId).NotEmpty();
            RuleFor(x => x.Role).IsInEnum();
        }
    }

    public sealed class SetCoHostValidator : AbstractValidator<SetCoHostCommand>
    {
        public SetCoHostValidator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.PlayerId).NotEmpty();
        }
    }

    public sealed class RemovePlayerValidator : AbstractValidator<RemovePlayerCommand>
    {
        public RemovePlayerValidator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.PlayerId).NotEmpty();
        }
    }

    public sealed class TransferHostValidator : AbstractValidator<TransferHostCommand>
    {
        public TransferHostValidator()
        {
            RuleFor(x => x.GameId).NotEmpty();
            RuleFor(x => x.PlayerId).NotEmpty();
        }
    }

    public sealed class SetPlayerRoleHandler(IGamePlayerRepository players, IUnitOfWork unitOfWork)
        : IRequestHandler<SetPlayerRoleCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(SetPlayerRoleCommand request, CancellationToken cancellationToken)
        {
            var player = await FindAsync(players, request.GameId, request.PlayerId, cancellationToken);
            if (player is null)
            {
                return GameAccessErrors.PlayerNotFound;
            }

            player.SetRole(request.Role);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class SetCoHostHandler(IGameRepository games, IGamePlayerRepository players, IUnitOfWork unitOfWork)
        : IRequestHandler<SetCoHostCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(SetCoHostCommand request, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(request.GameId, cancellationToken);
            if (game is null)
            {
                return GameAccessErrors.GameNotFound;
            }

            var player = await FindAsync(players, request.GameId, request.PlayerId, cancellationToken);
            if (player is null || player.PlayerId == game.HostId)
            {
                return GameAccessErrors.PlayerNotFound;
            }

            player.SetCoHost(request.IsCoHost);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class RemovePlayerHandler(IGameRepository games, IGamePlayerRepository players, IUnitOfWork unitOfWork)
        : IRequestHandler<RemovePlayerCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(RemovePlayerCommand request, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(request.GameId, cancellationToken);
            if (game is null)
            {
                return GameAccessErrors.GameNotFound;
            }

            // The host cannot be removed; hand the game over first.
            if (request.PlayerId == game.HostId)
            {
                return GameAccessErrors.NotHost;
            }

            var rows = (await players.GetByGame(request.GameId, cancellationToken))
                .Where(row => row.PlayerId == request.PlayerId)
                .ToList();
            if (rows.Count == 0)
            {
                return GameAccessErrors.PlayerNotFound;
            }

            players.DeleteRange(rows);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    public sealed class TransferHostHandler(IGameRepository games, IGamePlayerRepository players, IUnitOfWork unitOfWork)
        : IRequestHandler<TransferHostCommand, Result<bool, ApplicationError>>
    {
        public async Task<Result<bool, ApplicationError>> Handle(TransferHostCommand request, CancellationToken cancellationToken)
        {
            var game = await games.GetByIdAsync(request.GameId, cancellationToken);
            if (game is null)
            {
                return GameAccessErrors.GameNotFound;
            }

            var newHost = await FindAsync(players, request.GameId, request.PlayerId, cancellationToken);
            if (newHost is null)
            {
                return GameAccessErrors.PlayerNotFound;
            }

            newHost.SetCoHost(false);
            game.TransferHost(request.PlayerId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    private static async Task<Domain.GamePlayer.GamePlayer?> FindAsync(
        IGamePlayerRepository players, Guid gameId, Guid playerId, CancellationToken cancellationToken) =>
        (await players.GetByGame(gameId, cancellationToken)).FirstOrDefault(row => row.PlayerId == playerId);
}
