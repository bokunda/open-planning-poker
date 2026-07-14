namespace OpenPlanningPoker.Shared.Errors;

public static class ApplicationErrorConstants
{
    public static KeyValuePair<string, string> UserNotFound => new("UserNotFound", "User not found");
}
