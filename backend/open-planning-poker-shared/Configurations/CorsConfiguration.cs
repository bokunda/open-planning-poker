namespace OpenPlanningPoker.Shared.Configurations;

public class CorsConfiguration
{
    public string PolicyName { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];
}