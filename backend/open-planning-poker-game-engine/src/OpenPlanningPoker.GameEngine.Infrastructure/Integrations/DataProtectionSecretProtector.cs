using Microsoft.AspNetCore.DataProtection;
using OpenPlanningPoker.GameEngine.Application.Features.Integrations.Jira;

namespace OpenPlanningPoker.GameEngine.Infrastructure.Integrations;

/// <summary>
/// ASP.NET Core Data Protection with its keys in the database (data_protection_keys), so tokens stay readable
/// across container restarts and deploys.
/// </summary>
public sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _tokens = provider.CreateProtector("OpenPlanningPoker.Integrations.Tokens.v1");
    private readonly ITimeLimitedDataProtector _state = provider.CreateProtector("OpenPlanningPoker.Integrations.OAuthState.v1").ToTimeLimitedDataProtector();

    public string Protect(string value) => _tokens.Protect(value);
    public string Unprotect(string value) => _tokens.Unprotect(value);
    public string ProtectState(string value, TimeSpan lifetime) => _state.Protect(value, lifetime);

    public string? UnprotectState(string value)
    {
        try { return _state.Unprotect(value); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
