using OpenPlanningPoker.GameEngine.Domain.GamePlayer;
using OpenPlanningPoker.GameEngine.Domain.Games;
using OpenPlanningPoker.GameEngine.Domain.Teams;

namespace OpenPlanningPoker.GameEngine.Domain.UnitTests.Tickets;

public class RoomFeaturesTests
{
    [Fact]
    public void FinalEstimate_IsTrimmed_AndEmptyClearsIt()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.RevealVotes();

        ticket.SetFinalEstimate(" 8 ");
        ticket.FinalEstimate.ShouldBe("8");

        ticket.SetFinalEstimate("   ");
        ticket.FinalEstimate.ShouldBeNull();
    }

    [Fact]
    public void NewVotingRound_DropsFinalEstimate()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "");
        ticket.RevealVotes();
        ticket.SetFinalEstimate("5");

        ticket.HideVotes();

        ticket.VotesRevealed.ShouldBeFalse();
        ticket.FinalEstimate.ShouldBeNull();
    }

    [Fact]
    public void Ticket_KeepsItsPosition()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Login", "", position: 3);
        ticket.Position.ShouldBe(3);

        ticket.MoveTo(0);
        ticket.Position.ShouldBe(0);
    }

    [Fact]
    public void Game_StartsWithCreatorAsHost_AndCanBeHandedOver()
    {
        var creator = Guid.NewGuid();
        var other = Guid.NewGuid();
        var team = Guid.NewGuid();

        var game = Game.Create("Sprint 42", "", creator, team);
        game.HostId.ShouldBe(creator);
        game.TeamId.ShouldBe(team);

        game.TransferHost(other);
        game.HostId.ShouldBe(other);
    }

    [Fact]
    public void Player_DefaultsToVoting_AndCanObserveOrCoHost()
    {
        var player = Domain.GamePlayer.GamePlayer.Create(Guid.NewGuid(), Guid.NewGuid());
        player.Role.ShouldBe(PlayerRole.Player);
        player.IsCoHost.ShouldBeFalse();

        player.SetRole(PlayerRole.Observer);
        player.SetCoHost(true);

        player.Role.ShouldBe(PlayerRole.Observer);
        player.IsCoHost.ShouldBeTrue();
    }

    [Fact]
    public void Settings_Options_DoNotTouchTheDeck()
    {
        var settings = Domain.GameSettings.GameSettings.Create(Guid.NewGuid(), "1,2,3");
        var endsAt = DateTimeOffset.UtcNow.AddDays(1);

        settings.UpdateOptions(autoReveal: true, asyncVotingEndsAt: endsAt);

        settings.DeckSetup.ShouldBe("1,2,3");
        settings.AutoReveal.ShouldBeTrue();
        settings.AsyncVotingEndsAt.ShouldBe(endsAt);
    }

    [Fact]
    public void Team_HasAnId_AndCanBeRenamed()
    {
        var team = Team.Create("Payments", "Checkout squad");
        team.Id.ShouldNotBe(Guid.Empty);

        team.Update("Payments & Billing", "");
        team.Name.ShouldBe("Payments & Billing");

        var member = TeamMember.Create(team.Id, Guid.NewGuid());
        member.TeamId.ShouldBe(team.Id);
    }
}
