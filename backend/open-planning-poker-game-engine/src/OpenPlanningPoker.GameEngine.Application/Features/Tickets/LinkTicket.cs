using System.Text.RegularExpressions;

namespace OpenPlanningPoker.GameEngine.Application.Features.Tickets;

/// <summary>
/// Links a ticket to a tracker issue it was started from, for example the Jira issue panel (OPP-120). Only Jira Cloud
/// keys and https links on atlassian.net are accepted, so a link can never point anywhere else.
/// </summary>
public sealed record LinkTicketCommand(Guid TicketId, string Key, string Url) : IRequest<Result<GetTicketResponse, ApplicationError>>;

public static partial class LinkTicket
{
    [GeneratedRegex(@"^[A-Z][A-Z0-9_]{0,30}-\d{1,12}$")]
    private static partial Regex JiraKey();

    public static bool IsJiraIssueUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.EndsWith(".atlassian.net", StringComparison.OrdinalIgnoreCase);

    public sealed class Validator : AbstractValidator<LinkTicketCommand>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).NotEmpty();
            RuleFor(x => x.Key).NotEmpty().MaximumLength(Ticket.MaxExternalKeyLength)
                .Must(key => JiraKey().IsMatch(key)).WithMessage("Use a Jira issue key such as OPP-118.");
            RuleFor(x => x.Url).NotEmpty().MaximumLength(Ticket.MaxExternalUrlLength)
                .Must(IsJiraIssueUrl).WithMessage("Use a link to a Jira Cloud issue (https://….atlassian.net).");
        }
    }

    public sealed class RequestHandler(ITicketRepository tickets, IUnitOfWork unitOfWork)
        : IRequestHandler<LinkTicketCommand, Result<GetTicketResponse, ApplicationError>>
    {
        public async Task<Result<GetTicketResponse, ApplicationError>> Handle(LinkTicketCommand request, CancellationToken cancellationToken)
        {
            var ticket = await tickets.GetByIdAsync(request.TicketId, cancellationToken);
            if (ticket is null)
            {
                return GameAccessErrors.TicketNotFound;
            }

            ticket.LinkTo(request.Key, request.Url);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ticket.ToGetTicketResponse();
        }
    }
}
