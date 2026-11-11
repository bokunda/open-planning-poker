using OpenPlanningPoker.GameEngine.Application.Features.GameSettings;

namespace OpenPlanningPoker.GameEngine.Application.Features.Votes;

public sealed record CreateOrUpdateVoteResponse(Guid Id, Guid PlayerId, Guid TicketId, string Value);

public sealed record CreateOrUpdateVoteCommand(Guid TicketId, string Value) : IRequest<Result<CreateOrUpdateVoteResponse, ApplicationError>>;

public static class CreateOrUpdateVote
{
    public class Validator : AbstractValidator<CreateOrUpdateVoteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId)
                .NotEmpty();

            RuleFor(x => x.Value)
                .NotEmpty()
                .MaximumLength(GameSettingsConstants.MaxCardLength);
        }
    }


    public sealed class RequestHandler(
        IVoteRepository voteRepository,
        ITicketRepository ticketRepository,
        IGameSettingsRepository gameSettingsRepository,
        ICurrentUserProvider currentUserProvider,
        IUnitOfWork unitOfWork)
        : IRequestHandler<CreateOrUpdateVoteCommand, Result<CreateOrUpdateVoteResponse, ApplicationError>>
    {
        public async Task<Result<CreateOrUpdateVoteResponse, ApplicationError>> Handle(CreateOrUpdateVoteCommand request, CancellationToken cancellationToken = default)
        {
            var ticket = await ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            var settings = await gameSettingsRepository.GetByGame(ticket.GameId, cancellationToken);
            if (settings is not null && !GameSettingsConstants.ParseDeck(settings.DeckSetup).Contains(request.Value))
            {
                return new ApplicationError("InvalidVote", "The vote must be one of the cards of this game's deck.");
            }

            var vote = (await voteRepository.GetByTicket(request.TicketId, currentUserProvider.Id, cancellationToken)).FirstOrDefault();

            if (vote is not null)
            {
                vote!.Update(request.Value);
                voteRepository.Update(vote);
            }
            else
            {
                vote = Vote.Create(currentUserProvider.Id, request.TicketId, request.Value);
                voteRepository.Add(vote);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return vote.ToCreateOrUpdateVoteResponse();
        }
    }
}