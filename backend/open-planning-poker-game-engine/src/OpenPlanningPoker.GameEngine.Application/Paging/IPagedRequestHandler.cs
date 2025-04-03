namespace OpenPlanningPoker.GameEngine.Application.Paging;

public interface IPagedRequestHandler<in TRequest, TResponse> : IRequestHandler<TRequest, PagedResponse<TResponse>>
    where TRequest : PagedRequest<TResponse>
{
}