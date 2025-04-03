namespace OpenPlanningPoker.GameEngine.Api.Models.Paging;

public interface IPagedRequest
{
    int PageNumber { get; set; }
    int PageSize { get; set; }
}