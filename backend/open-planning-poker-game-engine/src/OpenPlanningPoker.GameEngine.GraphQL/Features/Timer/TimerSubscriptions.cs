using HotChocolate;

namespace OpenPlanningPoker.GameEngine.GraphQL.Features.Timer;

[SubscriptionType]
public class TimerSubscriptions
{
    /// <summary>Fires when the host starts or stops the timer.</summary>
    [Subscribe]
    [Topic("TimerChanged_{gameId}")]
    public GameTimer OnTimerChanged(Guid gameId, [EventMessage] GameTimer timer)
    {
        timer.ServerTime = DateTime.UtcNow;
        return timer;
    }
}
