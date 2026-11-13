using System.Globalization;

namespace OpenPlanningPoker.GameEngine.Application.Localization;

/// <summary>
/// Texts the server sends to people (error messages, the PDF report) in English, Serbian (Latin) and German
/// (OPP-24). The language is the request's UI culture, set from Accept-Language by the request localization
/// middleware; anything else falls back to English.
/// </summary>
public static class ServerText
{
    public static readonly string[] SupportedCultures = ["en", "sr-Latn", "de"];

    /// <summary>The culture for dates in the report: the request's, when it is one of the supported ones.</summary>
    public static CultureInfo Culture => Language switch
    {
        "sr" => CultureInfo.GetCultureInfo("sr-Latn"),
        "de" => CultureInfo.GetCultureInfo("de"),
        _ => CultureInfo.GetCultureInfo("en-GB"),
    };

    private static string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "sr" or "hr" or "bs" => "sr",
        "de" => "de",
        _ => "en",
    };

    public static string Get(string key, params object[] args)
    {
        if (!Texts.TryGetValue(key, out var text)) return key;
        var template = Language switch { "sr" => text.Sr, "de" => text.De, _ => text.En };
        return args.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, args);
    }

    /// <summary>The message of an error code in the request's language; unknown codes keep their own message.</summary>
    public static string ErrorMessage(string code, string? fallback) =>
        Texts.ContainsKey("error." + code) ? Get("error." + code) : fallback ?? code;

    private static readonly Dictionary<string, (string En, string Sr, string De)> Texts = new()
    {
        // Errors, by ApplicationError code
        ["error.Unauthenticated"] = ("A valid user token is required.", "Potreban je važeći korisnički token.", "Ein gültiges Benutzer-Token ist erforderlich."),
        ["error.GameNotFound"] = ("Game not found.", "Igra nije pronađena.", "Spiel nicht gefunden."),
        ["error.TicketNotFound"] = ("Ticket not found.", "Tiket nije pronađen.", "Ticket nicht gefunden."),
        ["error.NotHost"] = ("Only the game host can perform this action.", "Ovo može da uradi samo host igre.", "Nur der Host des Spiels kann das tun."),
        ["error.NotMember"] = ("Join the game before performing this action.", "Prvo uđi u igru.", "Tritt zuerst dem Spiel bei."),
        ["error.Observer"] = ("Observers cannot vote. Switch to player to vote.", "Posmatrači ne glasaju. Postani igrač da bi glasao.", "Beobachter können nicht abstimmen. Wechsle zu Spieler, um abzustimmen."),
        ["error.PlayerNotFound"] = ("That player is not in this game.", "Taj igrač nije u ovoj igri.", "Dieser Spieler ist nicht in diesem Spiel."),
        ["error.TeamNotFound"] = ("Team not found.", "Tim nije pronađen.", "Team nicht gefunden."),
        ["error.NotTeamMember"] = ("Join the team before performing this action.", "Prvo se pridruži timu.", "Tritt zuerst dem Team bei."),
        ["error.NotTeamOwner"] = ("Only the team owner can perform this action.", "Ovo može da uradi samo vlasnik tima.", "Nur der Inhaber des Teams kann das tun."),
        ["error.TeamOwnerCannotLeave"] = ("The team owner cannot leave the team.", "Vlasnik tima ne može da napusti tim.", "Der Inhaber kann das Team nicht verlassen."),
        ["error.GameSettingsNotFound"] = ("Game settings not found.", "Podešavanja igre nisu pronađena.", "Spieleinstellungen nicht gefunden."),
        ["error.VotesNotRevealed"] = ("Reveal the cards before choosing the final estimate.", "Otkrij karte pre izbora konačne procene.", "Decke die Karten auf, bevor du die endgültige Schätzung wählst."),
        ["error.InvalidVote"] = ("The vote must be one of the cards of this game's deck.", "Glas mora biti jedna od karata iz špila ove igre.", "Die Stimme muss eine Karte aus dem Deck dieses Spiels sein."),
        ["error.InvalidChatMessage"] = ("Message must be between 1 and 1000 characters.", "Poruka mora imati od 1 do 1000 znakova.", "Die Nachricht muss 1 bis 1000 Zeichen lang sein."),
        ["error.InvalidTimerDuration"] = ("Choose a timer between 10 seconds and 1 hour.", "Izaberi tajmer od 10 sekundi do 1 sata.", "Wähle einen Timer zwischen 10 Sekunden und 1 Stunde."),
        ["error.RateLimited"] = ("Too many requests. Please wait a moment and try again.", "Previše zahteva. Sačekaj malo i pokušaj ponovo.", "Zu viele Anfragen. Bitte warte kurz und versuche es erneut."),
        ["error.UserNotFound"] = ("User not found.", "Korisnik nije pronađen.", "Benutzer nicht gefunden."),

        // PDF report
        ["report.title"] = ("Game report", "Izveštaj igre", "Spielbericht"),
        ["report.created"] = ("Created {0}", "Napravljeno {0}", "Erstellt am {0}"),
        ["report.exported"] = ("Exported {0} UTC", "Izvezeno {0} UTC", "Exportiert am {0} UTC"),
        ["report.tickets"] = ("Tickets", "Tiketi", "Tickets"),
        ["report.revealed"] = ("Revealed", "Otkriveno", "Aufgedeckt"),
        ["report.revealedOf"] = ("{0} of {1}", "{0} od {1}", "{0} von {1}"),
        ["report.players"] = ("Players", "Igrači", "Spieler"),
        ["report.averageEstimate"] = ("Average estimate", "Prosečna procena", "Durchschnittliche Schätzung"),
        ["report.noTickets"] = ("No tickets in this game yet.", "U ovoj igri još nema tiketa.", "In diesem Spiel gibt es noch keine Tickets."),
        ["report.notRevealed"] = ("The cards were not revealed.", "Karte nisu otkrivene.", "Die Karten wurden nicht aufgedeckt."),
        ["report.nobodyVoted"] = ("Nobody voted.", "Niko nije glasao.", "Niemand hat abgestimmt."),
        ["report.estimate"] = ("Estimate", "Procena", "Schätzung"),
        ["report.finalEstimate"] = ("Final estimate", "Konačna procena", "Endgültige Schätzung"),
        ["report.average"] = ("Average", "Prosek", "Durchschnitt"),
        ["report.median"] = ("Median", "Medijana", "Median"),
        ["report.votes"] = ("Votes", "Glasovi", "Stimmen"),
        ["report.consensus"] = ("Consensus", "Konsenzus", "Konsens"),
        ["report.close"] = ("Close to consensus", "Blizu konsenzusa", "Fast Konsens"),
        ["report.wide"] = ("Wide spread", "Velika razlika", "Große Streuung"),
        ["report.noVotes"] = ("No votes", "Nema glasova", "Keine Stimmen"),
        ["report.notRevealedShort"] = ("Not revealed", "Nije otkriveno", "Nicht aufgedeckt"),
        ["report.generatedBy"] = ("Generated by ", "Napravio ", "Erstellt mit "),
        ["report.page"] = ("Page ", "Strana ", "Seite "),
        ["report.of"] = (" of ", " od ", " von "),
    };
}
