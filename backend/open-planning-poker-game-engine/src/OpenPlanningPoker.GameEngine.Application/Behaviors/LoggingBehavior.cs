namespace OpenPlanningPoker.GameEngine.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = request.GetType().Name;

        try
        {
            logger.LogInformation("Executing {IRequest}", name);

            var result = await next();

            logger.LogInformation("{IRequest} processed successfully", name);

            return result;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "{IRequest} processing failed", name);

            throw;
        }
    }
}