namespace OpenPlanningPoker.GameEngine.Api.Models.Paging;

public interface IPagedRequestHandler<in TRequest, TResponse> where TRequest : PagedRequest<TResponse>
{
}