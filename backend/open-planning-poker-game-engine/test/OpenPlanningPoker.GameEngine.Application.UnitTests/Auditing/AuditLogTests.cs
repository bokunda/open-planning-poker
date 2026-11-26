using Microsoft.Extensions.Logging.Abstractions;
using OpenPlanningPoker.GameEngine.Application.Abstractions.Auditing;
using OpenPlanningPoker.GameEngine.Application.Abstractions.Authorization;
using OpenPlanningPoker.GameEngine.Application.Features.Audits;
using OpenPlanningPoker.GameEngine.Application.Features.GamePlayer;
using OpenPlanningPoker.GameEngine.Application.Features.Teams;
using OpenPlanningPoker.GameEngine.Application.Features.Tickets;
using OpenPlanningPoker.GameEngine.Application.Features.Votes;
using OpenPlanningPoker.GameEngine.Domain.Audits;
using OpenPlanningPoker.Shared.Errors;
using OpenPlanningPoker.Shared.Results;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Auditing;

/// <summary>The activity log (GitHub #20): what is written, when, and who can read it.</summary>
public class AuditLogTests
{
    private sealed record Plain(int Value);

    // ------------------------------------------------------------ AuditBehavior

    [Fact]
    public async Task Behavior_ResolvesBeforeTheHandler_AndWritesAfterASuccess()
    {
        var log = new RecordingAuditLog();
        var behavior = new AuditBehavior<SetTicketVotesRevealedCommand, Result<bool, ApplicationError>>(log);
        var ticketId = Guid.NewGuid();

        var result = await behavior.Handle(new SetTicketVotesRevealedCommand(ticketId, true), _ =>
        {
            log.Calls.Add("handler");
            return Task.FromResult<Result<bool, ApplicationError>>(true);
        }, default);

        result.IsSuccess.ShouldBeTrue();
        log.Calls.ShouldBe(["resolve", "handler", "write"]);
        var entry = log.Written.ShouldHaveSingleItem();
        entry.Action.ShouldBe(AuditActions.VotesRevealed);
        entry.TicketId.ShouldBe(ticketId);
        entry.ScopeId.ShouldBe(log.TicketGame!.Value);
    }

    [Fact]
    public async Task Behavior_DoesNotWrite_WhenTheHandlerReturnsAnError()
    {
        var log = new RecordingAuditLog();
        var behavior = new AuditBehavior<RemovePlayerCommand, Result<bool, ApplicationError>>(log);

        var result = await behavior.Handle(new RemovePlayerCommand(Guid.NewGuid(), Guid.NewGuid()),
            _ => Task.FromResult<Result<bool, ApplicationError>>(GameAccessErrors.PlayerNotFound), default);

        result.IsSuccess.ShouldBeFalse();
        log.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task Behavior_DoesNotWrite_WhenTheHandlerThrows()
    {
        var log = new RecordingAuditLog();
        var behavior = new AuditBehavior<DeleteTicketCommand, Result<bool, ApplicationError>>(log);

        await Should.ThrowAsync<InvalidOperationException>(() => behavior.Handle(new DeleteTicketCommand(Guid.NewGuid()),
            _ => throw new InvalidOperationException(), default));

        log.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task Behavior_IgnoresRequestsThatAreNotAudited()
    {
        var log = new RecordingAuditLog();
        var behavior = new AuditBehavior<CreateOrUpdateVoteCommand, Result<bool, ApplicationError>>(log);
        var ran = false;

        await behavior.Handle(new CreateOrUpdateVoteCommand(Guid.NewGuid(), "5"), _ =>
        {
            ran = true;
            return Task.FromResult<Result<bool, ApplicationError>>(true);
        }, default);

        ran.ShouldBeTrue();
        log.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Behavior_SkipsTheEntry_WhenItCannotBePlaced()
    {
        var log = new RecordingAuditLog { TicketGame = null };
        var behavior = new AuditBehavior<ResetTicketVotesCommand, Result<bool, ApplicationError>>(log);

        await behavior.Handle(new ResetTicketVotesCommand(Guid.NewGuid()), _ => Task.FromResult<Result<bool, ApplicationError>>(true), default);

        log.Written.ShouldBeEmpty();
    }

    [Fact]
    public void Behavior_TreatsOtherResultsAsSuccess()
    {
        AuditBehavior<CreateTicketCommand, Plain>.IsSuccess(new Plain(1)).ShouldBeTrue();
        AuditBehavior<CreateTicketCommand, Plain?>.IsSuccess(null).ShouldBeFalse();
        AuditBehavior<CreateTicketCommand, Result<bool, ApplicationError>>.IsSuccess(GameAccessErrors.NotHost).ShouldBeFalse();
    }

    // ------------------------------------------------------------ what the commands write

    [Fact]
    public void Commands_DescribeThemselves()
    {
        var game = Guid.NewGuid();
        var player = Guid.NewGuid();
        var ticket = Guid.NewGuid();

        new SetTicketVotesRevealedCommand(ticket, false).ToAuditEntry()!.Action.ShouldBe(AuditActions.VotesHidden);
        new SetCoHostCommand(game, player, true).ToAuditEntry().ShouldBe(AuditEntry.Game(AuditActions.PlayerCoHostGranted, AuditType.Update, game, player));
        new SetCoHostCommand(game, player, false).ToAuditEntry()!.Action.ShouldBe(AuditActions.PlayerCoHostRevoked);
        new SetPlayerRoleCommand(game, player, PlayerRole.Observer).ToAuditEntry()!.Value.ShouldBe("Observer");
        new SetFinalEstimateCommand(ticket, "8").ToAuditEntry().ShouldBe(AuditEntry.Ticket(AuditActions.TicketEstimated, AuditType.Update, ticket, "8"));
        new UpdateTicketCommand(ticket, "New name", "").ToAuditEntry()!.Detail.ShouldBe("New name");
        new CreateTicketCommand(game, "Login", "").ToAuditEntry()!.ShouldSatisfyAllConditions(
            e => e.ScopeId.ShouldBe(game), e => e.Detail.ShouldBe("Login"), e => e.Type.ShouldBe(AuditType.Create));

        var team = Guid.NewGuid();
        var leave = new LeaveTeamCommand(team, player).ToAuditEntry()!;
        leave.Scope.ShouldBe(AuditScope.Team);
        leave.TargetId.ShouldBe(player);
        leave.Action.ShouldBe(AuditActions.TeamMemberRemoved);
    }

    [Fact]
    public void WebhookHost_KeepsOnlyTheHost_BecausePathsCarrySecrets()
    {
        TeamApiService.WebhookHost("https://hooks.zapier.com/hooks/catch/123/secret/").ShouldBe("hooks.zapier.com");
        TeamApiService.WebhookHost("not a url").ShouldBeEmpty();
    }

    // ------------------------------------------------------------ AuditLog (the writer)

    [Fact]
    public async Task AuditLog_PlacesATicketEntryInItsGame_WithTheTicketName()
    {
        var tickets = new FakeAuditTickets();
        var ticket = Ticket.Create(Guid.NewGuid(), "Checkout page", "");
        tickets.Add(ticket);
        var log = new AuditLog(new FakeAuditRepository(), tickets, new FakeAuditUnitOfWork(), NullLogger<AuditLog>.Instance);

        var resolved = await log.ResolveAsync(AuditEntry.Ticket(AuditActions.TicketDeleted, AuditType.Delete, ticket.Id));

        resolved!.ScopeId.ShouldBe(ticket.GameId);
        resolved.Detail.ShouldBe("Checkout page");
    }

    [Fact]
    public async Task AuditLog_KeepsTheScopeOfATeamEntry_AndAddsTheTicketName()
    {
        var tickets = new FakeAuditTickets();
        var ticket = Ticket.Create(Guid.NewGuid(), "Search", "");
        tickets.Add(ticket);
        var team = Guid.NewGuid();
        var log = new AuditLog(new FakeAuditRepository(), tickets, new FakeAuditUnitOfWork(), NullLogger<AuditLog>.Instance);

        var resolved = await log.ResolveAsync(AuditEntry.Team(AuditActions.TeamAnchorPinned, AuditType.Create, team, ticketId: ticket.Id));

        resolved!.ScopeId.ShouldBe(team);
        resolved.Detail.ShouldBe("Search");
    }

    [Fact]
    public async Task AuditLog_CannotPlaceATicketThatDoesNotExist()
    {
        var log = new AuditLog(new FakeAuditRepository(), new FakeAuditTickets(), new FakeAuditUnitOfWork(), NullLogger<AuditLog>.Instance);

        (await log.ResolveAsync(AuditEntry.Ticket(AuditActions.VotesReset, AuditType.Update, Guid.NewGuid()))).ShouldBeNull();
    }

    [Fact]
    public async Task AuditLog_WritesARow()
    {
        var audits = new FakeAuditRepository();
        var unitOfWork = new FakeAuditUnitOfWork();
        var game = Guid.NewGuid();
        var player = Guid.NewGuid();
        IAuditLog log = new AuditLog(audits, new FakeAuditTickets(), unitOfWork, NullLogger<AuditLog>.Instance);

        await log.RecordAsync(AuditEntry.Game(AuditActions.PlayerRemoved, AuditType.Delete, game, player));

        var row = audits.Rows.ShouldHaveSingleItem();
        row.Scope.ShouldBe(AuditScope.Game);
        row.ScopeId.ShouldBe(game);
        row.TargetId.ShouldBe(player);
        row.Action.ShouldBe(AuditActions.PlayerRemoved);
        row.Id.ShouldNotBe(Guid.Empty);
        unitOfWork.Saves.ShouldBe(1);
    }

    [Fact]
    public async Task AuditLog_NeverFailsTheAction_AndForgetsTheUnsavedRow()
    {
        var audits = new FakeAuditRepository();
        var log = new AuditLog(audits, new FakeAuditTickets(), new FakeAuditUnitOfWork { Fail = true }, NullLogger<AuditLog>.Instance);

        await Should.NotThrowAsync(() => log.WriteAsync(AuditEntry.Game(AuditActions.VotesReset, AuditType.Update, Guid.NewGuid())));

        audits.Rows.ShouldBeEmpty();
        audits.Discarded.ShouldHaveSingleItem();
    }

    [Fact]
    public void Audit_ShortensLongTexts_AndNeedsAScope()
    {
        var audit = Audit.Create(AuditScope.Game, Guid.NewGuid(), AuditActions.TicketCreated, AuditType.Create, detail: new string('a', 600), value: "  8 ");
        audit.Detail!.Length.ShouldBe(Audit.MaxDetailLength);
        audit.Detail.ShouldEndWith("…");
        audit.Value.ShouldBe("8");

        Should.Throw<ArgumentException>(() => Audit.Create(AuditScope.Game, Guid.Empty, AuditActions.TicketCreated, AuditType.Create));
        Should.Throw<ArgumentException>(() => Audit.Create(AuditScope.Game, Guid.NewGuid(), "", AuditType.Create));
    }

    // ------------------------------------------------------------ reading (access checks and paging)

    private static (GetAuditLog.Handler Handler, FakeAuditAccess Access, FakeAuditRepository Audits) Reader()
    {
        var access = new FakeAuditAccess();
        var audits = new FakeAuditRepository();
        return (new GetAuditLog.Handler(access, audits), access, audits);
    }

    [Fact]
    public async Task Read_AGame_NeedsTheHostOrACoHost()
    {
        var (handler, access, _) = Reader();
        access.HostError = GameAccessErrors.NotHost;

        var result = await handler.Handle(new GetAuditLogQuery(AuditScope.Game, Guid.NewGuid()), default);

        result.Error!.Code.ShouldBe("NotHost");
        access.Checks.ShouldBe(["host"]);
    }

    [Fact]
    public async Task Read_ATeam_NeedsTheOwner()
    {
        var (handler, access, _) = Reader();
        access.TeamOwnerError = GameAccessErrors.NotTeamOwner;

        (await handler.Handle(new GetAuditLogQuery(AuditScope.Team, Guid.NewGuid()), default)).Error!.Code.ShouldBe("NotTeamOwner");
        access.Checks.ShouldBe(["teamOwner"]);
    }

    [Fact]
    public async Task Read_AnOrganization_NeedsAnAdmin()
    {
        var (handler, access, _) = Reader();
        access.OrganizationAdminError = GameAccessErrors.NotOrganizationAdmin;

        (await handler.Handle(new GetAuditLogQuery(AuditScope.Organization, Guid.NewGuid()), default)).Error!.Code.ShouldBe("NotOrganizationAdmin");
        access.Checks.ShouldBe(["organizationAdmin"]);
    }

    [Fact]
    public async Task Read_AUserLog_OnlyYourOwn()
    {
        var (handler, access, _) = Reader();

        (await handler.Handle(new GetAuditLogQuery(AuditScope.User, Guid.NewGuid()), default)).Error!.Code.ShouldBe("Forbidden");
        (await handler.Handle(new GetAuditLogQuery(AuditScope.User, access.CurrentUserId), default)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Read_ReturnsAPage_AndTellsWhetherThereIsMore()
    {
        var (handler, _, audits) = Reader();
        var game = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
        {
            audits.Add(Audit.Create(AuditScope.Game, game, AuditActions.VotesRevealed, AuditType.Update));
        }
        audits.Add(Audit.Create(AuditScope.Game, Guid.NewGuid(), AuditActions.VotesRevealed, AuditType.Update));

        var first = (await handler.Handle(new GetAuditLogQuery(AuditScope.Game, game, Take: 3), default)).Value!;
        first.Entries.Count.ShouldBe(3);
        first.HasMore.ShouldBeTrue();

        var all = (await handler.Handle(new GetAuditLogQuery(AuditScope.Game, game, Take: 500), default)).Value!;
        all.Entries.Count.ShouldBe(5);
        all.HasMore.ShouldBeFalse();
    }
}
