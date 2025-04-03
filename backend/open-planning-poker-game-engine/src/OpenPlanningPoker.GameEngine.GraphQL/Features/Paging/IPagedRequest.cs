namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Paging;

public interface IPagedRequest
{
    int PageNumber { get; set; }
    int PageSize { get; set; }
}