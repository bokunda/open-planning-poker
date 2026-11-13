using System.Globalization;

namespace OpenPlanningPoker.UserManagement.GraphQL.Localization;

/// <summary>
/// Error messages of User Management in English, Serbian (Latin) and German (OPP-24), like the Game Engine's.
/// The language is the request's UI culture, set from Accept-Language (forwarded by the gateway); anything
/// else falls back to English. Unknown codes keep their own message.
/// </summary>
public static class ServerText
{
    private static string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "sr" or "hr" or "bs" => "sr",
        "de" => "de",
        _ => "en",
    };

    private static readonly (string En, string Sr, string De) InvalidUsername = (
        "The username must be between 3 and 26 characters.",
        "Korisničko ime mora imati od 3 do 26 znakova.",
        "Der Benutzername muss 3 bis 26 Zeichen lang sein.");

    private static readonly Dictionary<string, (string En, string Sr, string De)> Errors = new()
    {
        // FluentValidation codes of the username rules
        ["NotEmptyValidator"] = InvalidUsername,
        ["MinimumLengthValidator"] = InvalidUsername,
        ["MaximumLengthValidator"] = InvalidUsername,
        ["UserNotFound"] = ("User not found.", "Korisnik nije pronađen.", "Benutzer nicht gefunden."),
        ["RateLimited"] = ("Too many requests. Please wait a moment and try again.", "Previše zahteva. Sačekaj malo i pokušaj ponovo.", "Zu viele Anfragen. Bitte warte kurz und versuche es erneut."),
    };

    public static string ErrorMessage(string code, string? fallback)
    {
        if (!Errors.TryGetValue(code, out var text)) return fallback ?? code;
        return Language switch { "sr" => text.Sr, "de" => text.De, _ => text.En };
    }
}
