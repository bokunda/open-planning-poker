namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Votes;

public class VoteMappingProfile : Profile
{
    public VoteMappingProfile()
    {
        CreateMap<GetVotesItem, Vote>();
        CreateMap<CreateOrUpdateVoteResponse, Vote>();
    }
}
