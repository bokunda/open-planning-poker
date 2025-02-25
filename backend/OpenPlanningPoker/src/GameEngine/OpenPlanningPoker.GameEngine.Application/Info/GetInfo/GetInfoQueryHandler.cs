namespace OpenPlanningPoker.GameEngine.Application.Info.GetInfo;
public class GetInfoQueryHandler : IRequestHandler<GetInfoQuery, GetInfoResponse>
{
    public async Task<GetInfoResponse> Handle(GetInfoQuery request, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new GetInfoResponse("1.0.0", "Bojan Piskulic", "bpiskulic1996@gmail.com"));
    }
}
