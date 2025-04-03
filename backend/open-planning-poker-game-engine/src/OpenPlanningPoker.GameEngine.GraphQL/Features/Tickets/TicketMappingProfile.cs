namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Tickets;

public class TicketMappingProfile : Profile
{
    public TicketMappingProfile()
    {
        CreateMap<GetTicketResponse, Ticket>();
        CreateMap<CreateTicketResponse, Ticket>();
        CreateMap<UpdateTicketResponse, Ticket>();
        CreateMap<GetTicketsItem, Ticket>();
    }
}
