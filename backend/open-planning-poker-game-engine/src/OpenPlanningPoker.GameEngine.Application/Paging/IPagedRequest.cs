namespace OpenPlanningPoker.GameEngine.Application.Paging;

public interface IPagedRequest
{
    int PageNumber { get; set; }
    int PageSize { get; set; }
}