namespace OpenPlanningPoker.GameEngine.Application.Info.GetInfo;

public sealed record GetInfoQuery() : IRequest<Result<GetInfoResponse, ApplicationError>>;
