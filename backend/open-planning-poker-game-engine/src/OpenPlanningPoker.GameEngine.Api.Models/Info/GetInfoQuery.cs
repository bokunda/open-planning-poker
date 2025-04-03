namespace OpenPlanningPoker.GameEngine.Api.Models.Info;

public sealed record GetInfoQuery();
public sealed record GetInfoResponse(string Version, string Author, string Contact);
