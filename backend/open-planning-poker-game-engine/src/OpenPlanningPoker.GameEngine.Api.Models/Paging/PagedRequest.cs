namespace OpenPlanningPoker.GameEngine.Api.Models.Paging;

public record PagedRequest<TResponse> : IPagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}