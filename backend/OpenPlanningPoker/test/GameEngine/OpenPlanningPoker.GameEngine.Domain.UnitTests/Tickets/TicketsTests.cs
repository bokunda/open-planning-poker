namespace OpenPlanningPoker.GameEngine.Domain.UnitTests.Tickets;

public class TicketsTests : BaseTest
{
    [Fact]
    public void CreateTicket_Should_Raise_CreateTicketDomainEvent()
    {
        // Arrange
        var gameId = Guid.Parse("33a02402-abdc-484c-ae12-e908f99d7889");
        const string name = "Test Name";
        const string description = "Description Test";

        // Act
        var ticket = Ticket.Create(gameId, name, description);

        // Assert
        AssertDomainEventWasPublished<CreateTicketDomainEvent>(ticket);
        ticket.GameId.ShouldBe(gameId);
        ticket.Name.ShouldBe(name);
        ticket.Description.ShouldBe(description);
    }

    [Fact]
    public void UpdateTicket_Should_Raise_CreateTicketDomainEvent()
    {
        // Arrange
        var gameId = Guid.Parse("33a02402-abdc-484c-ae12-e908f99d7889");
        const string name = "Test Name";
        const string description = "Description Test";

        const string nameUpdated = "Test Name Updated";
        const string descriptionUpdated = "Description Test Updated";

        // Act
        var ticket = Ticket.Create(gameId, name, description);
        ticket.Update(nameUpdated, descriptionUpdated);

        // Assert
        AssertDomainEventWasPublished<UpdateTicketDomainEvent>(ticket);
        ticket.GameId.ShouldBe(gameId);
        ticket.Name.ShouldBe(nameUpdated);
        ticket.Description.ShouldBe(descriptionUpdated);
    }

}