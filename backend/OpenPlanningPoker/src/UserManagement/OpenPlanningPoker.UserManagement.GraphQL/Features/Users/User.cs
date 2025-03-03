﻿namespace OpenPlanningPoker.UserManagement.GraphQL.Features.Users;

public sealed class User(Guid id, string username)
{
    public Guid Id { get; private set; } = id;
    public string UserName { get; private set; } = username;

    public string GetCacheKey() => $"User:{Id}";
}
