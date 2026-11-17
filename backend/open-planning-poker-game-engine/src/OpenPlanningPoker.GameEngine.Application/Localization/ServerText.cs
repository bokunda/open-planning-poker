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
        ["error.CardsNotRevealed"] = ("Reveal the cards first.", "Prvo otkrij karte.", "Decke zuerst die Karten auf."),
        ["error.NoVote"] = ("Vote on this ticket first.", "Prvo glasaj za ovaj tiket.", "Stimme zuerst für dieses Ticket ab."),
        ["error.NoFinalEstimate"] = ("Choose the final estimate first.", "Prvo izaberi konačnu procenu.", "Wähle zuerst die endgültige Schätzung."),
        ["error.InvalidConfidence"] = ("Confidence must be between 1 and 5.", "Sigurnost mora biti od 1 do 5.", "Die Sicherheit muss zwischen 1 und 5 liegen."),
        ["error.InvalidDiscussionTime"] = ("Choose 0, 2, 3 or 5 minutes.", "Izaberi 0, 2, 3 ili 5 minuta.", "Wähle 0, 2, 3 oder 5 Minuten."),
        ["error.InvalidReaction"] = ("Unknown reaction.", "Nepoznata reakcija.", "Unbekannte Reaktion."),
        ["error.ReactionsOff"] = ("Reactions are turned off in this room.", "Reakcije su isključene u ovoj sobi.", "Reaktionen sind in diesem Raum ausgeschaltet."),
        ["error.InvalidIssueLink"] = ("Use a link to a Jira Cloud issue.", "Koristi link ka Jira Cloud issue-u.", "Verwende einen Link zu einem Jira-Cloud-Issue."),
        ["error.TicketNotInTeam"] = ("This ticket is not part of the team's sprints.", "Ovaj tiket nije iz sprintova ovog tima.", "Dieses Ticket gehört nicht zu den Sprints des Teams."),
        ["error.TicketNotEstimated"] = ("Only estimated tickets can be pinned.", "Mogu da se zakače samo procenjeni tiketi.", "Nur geschätzte Tickets können angeheftet werden."),
        ["error.AnchorLimit"] = ("This card already has two reference tickets. Unpin one first.", "Ova karta već ima dva referentna tiketa. Prvo otkači jedan.", "Diese Karte hat schon zwei Referenz-Tickets. Löse zuerst eines."),
        ["error.AnchorNotFound"] = ("Reference ticket not found.", "Referentni tiket nije pronađen.", "Referenz-Ticket nicht gefunden."),
        ["error.TrackerUnknown"] = ("Unknown issue tracker.", "Nepoznat alat za tikete.", "Unbekannter Issue-Tracker."),
        ["error.TrackerNotConfigured"] = ("This tracker is not set up on this server.", "Ovaj alat nije podešen na ovom serveru.", "Dieser Tracker ist auf diesem Server nicht eingerichtet."),
        ["error.TrackerNotConnected"] = ("Connect the tracker first.", "Prvo poveži alat.", "Verbinde zuerst den Tracker."),
        ["error.TrackerReconnect"] = ("Access to the tracker was revoked or expired. Connect it again.", "Pristup alatu je opozvan ili je istekao. Poveži ga ponovo.", "Der Zugriff auf den Tracker wurde widerrufen oder ist abgelaufen. Verbinde ihn erneut."),
        ["error.TrackerInvalidState"] = ("The sign-in expired or does not belong to you. Try again.", "Prijava je istekla ili nije tvoja. Pokušaj ponovo.", "Die Anmeldung ist abgelaufen oder gehört nicht dir. Versuche es erneut."),
        ["error.TrackerFailed"] = ("The tracker did not answer as expected. Try again in a moment.", "Alat nije odgovorio kako treba. Pokušaj ponovo za trenutak.", "Der Tracker hat nicht wie erwartet geantwortet. Versuche es gleich noch einmal."),
        ["error.TrackerNotLinked"] = ("This ticket was not imported from a tracker.", "Ovaj tiket nije uvezen iz alata.", "Dieses Ticket wurde nicht aus einem Tracker importiert."),
        ["error.TrackerNoEstimate"] = ("Reveal the cards and choose an estimate first.", "Prvo otkrij karte i izaberi procenu.", "Decke zuerst die Karten auf und wähle eine Schätzung."),
        ["error.UserNotFound"] = ("User not found.", "Korisnik nije pronađen.", "Benutzer nicht gefunden."),

        // Jira integration (OPP-10, OPP-11, OPP-12)
        ["error.JiraNotConfigured"] = ("The Jira integration is not set up on this server.", "Jira integracija nije podešena na ovom serveru.", "Die Jira-Integration ist auf diesem Server nicht eingerichtet."),
        ["error.JiraNotConnected"] = ("Connect Jira first.", "Prvo poveži Jiru.", "Verbinde zuerst Jira."),
        ["error.JiraReconnect"] = ("Jira access was revoked or expired. Reconnect Jira.", "Pristup Jiri je opozvan ili je istekao. Ponovo poveži Jiru.", "Der Jira-Zugriff wurde widerrufen oder ist abgelaufen. Verbinde Jira erneut."),
        ["error.JiraNoSite"] = ("Choose a Jira site first.", "Prvo izaberi Jira sajt.", "Wähle zuerst eine Jira-Site."),
        ["error.JiraInvalidState"] = ("The Jira sign-in expired or does not belong to you. Try again.", "Jira prijava je istekla ili nije tvoja. Pokušaj ponovo.", "Die Jira-Anmeldung ist abgelaufen oder gehört nicht dir. Versuche es erneut."),
        ["error.JiraForbidden"] = ("Jira does not allow you to do this. Ask a Jira admin for permission.", "Jira ti ovo ne dozvoljava. Zatraži dozvolu od Jira administratora.", "Jira erlaubt dir das nicht. Bitte einen Jira-Admin um die Berechtigung."),
        ["error.JiraFieldMissing"] = ("This issue has no Story Points field on its edit screen.", "Ovaj issue nema polje Story Points na ekranu za izmenu.", "Dieses Issue hat kein Story-Points-Feld in seiner Bearbeitungsmaske."),
        ["error.JiraNotNumeric"] = ("Only numeric estimates can be saved as story points.", "Kao story points mogu da se sačuvaju samo brojevi.", "Nur Zahlen können als Story Points gespeichert werden."),
        ["error.JiraNoEstimate"] = ("Reveal the cards and choose an estimate first.", "Prvo otkrij karte i izaberi procenu.", "Decke zuerst die Karten auf und wähle eine Schätzung."),
        ["error.JiraNotLinked"] = ("This ticket was not imported from Jira.", "Ovaj tiket nije uvezen iz Jire.", "Dieses Ticket wurde nicht aus Jira importiert."),
        ["error.JiraInvalidQuery"] = ("Jira could not run this query. Check the JQL.", "Jira nije mogla da izvrši ovaj upit. Proveri JQL.", "Jira konnte diese Abfrage nicht ausführen. Prüfe die JQL."),
        ["error.JiraFailed"] = ("Jira did not answer as expected. Try again in a moment.", "Jira nije odgovorila kako treba. Pokušaj ponovo za trenutak.", "Jira hat nicht wie erwartet geantwortet. Versuche es gleich noch einmal."),

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
        ["report.needsInfo"] = ("Needs more information", "Treba više informacija", "Braucht mehr Informationen"),
        ["report.confidence"] = ("Confidence {0} of 5", "Sigurnost {0} od 5", "Sicherheit {0} von 5"),
        ["report.lowConfidence"] = ("low", "niska", "gering"),
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
