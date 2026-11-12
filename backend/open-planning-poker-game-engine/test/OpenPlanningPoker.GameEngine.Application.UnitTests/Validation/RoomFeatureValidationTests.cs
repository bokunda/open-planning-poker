using OpenPlanningPoker.GameEngine.Application.Features.GameSettings;
using OpenPlanningPoker.GameEngine.Application.Features.Teams;
using OpenPlanningPoker.GameEngine.Application.Features.Tickets;

namespace OpenPlanningPoker.GameEngine.Application.UnitTests.Validation;

public class RoomFeatureValidationTests
{
    [Fact]
    public void Reorder_RejectsDuplicateTickets()
    {
        var id = Guid.NewGuid();
        var result = new ManageTickets.ReorderValidator().Validate(new ReorderTicketsCommand(Guid.NewGuid(), [id, id]));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Reorder_AcceptsDistinctTickets()
    {
        var result = new ManageTickets.ReorderValidator().Validate(new ReorderTicketsCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void FinalEstimate_HasAMaximumLength()
    {
        var validator = new ManageTickets.FinalEstimateValidator();
        validator.Validate(new SetFinalEstimateCommand(Guid.NewGuid(), "13")).IsValid.ShouldBeTrue();
        validator.Validate(new SetFinalEstimateCommand(Guid.NewGuid(), null)).IsValid.ShouldBeTrue();
        validator.Validate(new SetFinalEstimateCommand(Guid.NewGuid(), new string('9', 33))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AsyncWindow_IsAtMostTwoWeeks()
    {
        var validator = new UpdateGameOptions.Validator();
        validator.Validate(new UpdateGameOptionsCommand(Guid.NewGuid(), false, DateTimeOffset.UtcNow.AddDays(3))).IsValid.ShouldBeTrue();
        validator.Validate(new UpdateGameOptionsCommand(Guid.NewGuid(), false, null)).IsValid.ShouldBeTrue();
        validator.Validate(new UpdateGameOptionsCommand(Guid.NewGuid(), false, DateTimeOffset.UtcNow.AddDays(30))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Team_NeedsAName()
    {
        var validator = new Teams.CreateValidator();
        validator.Validate(new CreateTeamCommand("", "", Guid.NewGuid())).IsValid.ShouldBeFalse();
        validator.Validate(new CreateTeamCommand("Payments", "", Guid.NewGuid())).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SprintEstimate_PrefersTheHostsFinalEstimate()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.RevealVotes();

        Teams.EstimateOf(ticket, ["3", "5", "5"]).ShouldBe("5");

        ticket.SetFinalEstimate("8");
        Teams.EstimateOf(ticket, ["3", "5", "5"]).ShouldBe("8");
    }

    [Fact]
    public void SprintEstimate_IsEmptyForHiddenTickets()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        Teams.EstimateOf(ticket, ["3", "5"]).ShouldBeNull();
    }
}
