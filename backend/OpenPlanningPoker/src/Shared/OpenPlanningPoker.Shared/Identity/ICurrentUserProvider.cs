﻿namespace OpenPlanningPoker.Shared.Identity;

public interface ICurrentUserProvider
{
    Guid UserId { get; }
    Task<BaseUserProfile> GetUserAsync(CancellationToken cancellationToken);
}