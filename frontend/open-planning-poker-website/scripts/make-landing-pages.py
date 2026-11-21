"""Writes the free tools, the tracker and use-case landing pages and the comparison page in English, Serbian and German
(OPP-176 to OPP-180).

Run from the website folder: python scripts/make-landing-pages.py
The output is plain HTML in pages/ (English) and i18n/<lang>/ (translations), like the guides; build.js adds them to the
site and the sitemap. App links get ?ref=<page> at build time, so the rooms they bring are counted (OPP-181).
"""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from landing_render import APP, picture, render  # noqa: E402
from landing_tools import SCALES_SCRIPT, VELOCITY_SCRIPT, scales_tool, velocity_tool  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parent.parent
DATE = "2026-11-21"
CHECKED = "2026-11-21"

# ------------------------------------------------------------------------------------------------ the pages

PAGES = {}

PAGES["velocity-calculator"] = {
    "kind": "WebPage",
    "en": {
        "title": "Sprint velocity and capacity calculator",
        "nav": "Velocity calculator",
        "description": "Free sprint velocity calculator: enter the points of your last sprints and who is available, and get how many story points fit in the next sprint. Runs in your browser.",
        "lead": "How many points fit in the next sprint? Enter what your team finished in the last sprints and how many days each person has. Nothing leaves your browser.",
        "app": "Sprint velocity and capacity calculator",
        "body": """
<h2>How the suggestion is made</h2>
<p>When at least two past sprints have <strong>person-days</strong> (the sum of the days every member was available), the calculator divides the points by the person-days of each sprint. The suggestion is that rate times the person-days of the next sprint. The range uses the slowest and the fastest of those sprints.</p>
<p>Without person-days it takes the <strong>average of the last three sprints</strong>, and the range is their lowest and highest result. Both use the last three sprints only, because older sprints say less about the team as it is now.</p>

<h2>Use it as a guide, not a target</h2>
<p>Velocity describes the past. A new member, a holiday or a sprint full of unknown work changes it. Plan a little under the suggestion when the work is new, and talk about the range rather than the single number.</p>

<h2>Let the app keep the numbers</h2>
<p>A <a class="link" href="https://app.openplanningpoker.com/team">team room</a> in Open Planning Poker keeps every sprint's points, asks for each member's days and suggests the budget with this same rule. You choose the tickets that fit and can move them into a Jira sprint.</p>
""",
        "faq": [
            ("What is sprint velocity?", "Velocity is the number of story points a team finishes in a sprint. The average of the last few sprints is a good guess for the next one."),
            ("What are person-days?", "The sum of the working days each member is available in the sprint. Four people for ten days, with one person away for two, is 38 person-days."),
            ("Is my data sent anywhere?", "No. The calculator runs in your browser and sends nothing to a server."),
        ],
        "ctaTitle": "Track this automatically in a team room.", "ctaText": "Free, no sign-up. The team room keeps your sprints and suggests the budget.", "cta": "Open a team room",
    },
    "sr": {
        "title": "Kalkulator brzine i kapaciteta sprinta",
        "nav": "Kalkulator brzine",
        "description": "Besplatan kalkulator brzine sprinta: unesi poene poslednjih sprintova i ko je dostupan, i saznaj koliko story pointsa staje u sledeći sprint. Radi u tvom browseru.",
        "lead": "Koliko poena staje u sledeći sprint? Unesi šta je tim završio u poslednjim sprintovima i koliko dana ima svaka osoba. Ništa ne napušta tvoj browser.",
        "app": "Kalkulator brzine i kapaciteta sprinta",
        "body": """
<h2>Kako nastaje predlog</h2>
<p>Kada bar dva prošla sprinta imaju <strong>čovek-dane</strong> (zbir dana koje je svaki član bio dostupan), kalkulator deli poene sa čovek-danima svakog sprinta. Predlog je taj odnos puta čovek-dani sledećeg sprinta. Raspon koristi najsporiji i najbrži od tih sprintova.</p>
<p>Bez čovek-dana uzima <strong>prosek poslednja tri sprinta</strong>, a raspon je njihov najniži i najviši rezultat. Oba načina koriste samo poslednja tri sprinta, jer stariji sprintovi manje govore o timu kakav je sada.</p>

<h2>Koristi ga kao smernicu, ne kao cilj</h2>
<p>Brzina opisuje prošlost. Novi član, praznik ili sprint pun nepoznatog posla je menjaju. Planiraj malo ispod predloga kad je posao nov, i pričajte o rasponu, a ne o jednom broju.</p>

<h2>Neka aplikacija čuva brojke</h2>
<p><a class="link" href="https://app.openplanningpoker.com/team">Timska soba</a> u Open Planning Pokeru čuva poene svakog sprinta, pita za dane svakog člana i predlaže budžet po istom pravilu. Biraš tikete koji staju i možeš da ih premestiš u Jira sprint.</p>
""",
        "faq": [
            ("Šta je brzina sprinta?", "Brzina (velocity) je broj story pointsa koje tim završi u sprintu. Prosek poslednjih nekoliko sprintova je dobra procena za sledeći."),
            ("Šta su čovek-dani?", "Zbir radnih dana koje je svaki član dostupan u sprintu. Četiri osobe po deset dana, uz jednu odsutnu dva dana, daju 38 čovek-dana."),
            ("Da li se moji podaci negde šalju?", "Ne. Kalkulator radi u tvom browseru i ništa ne šalje na server."),
        ],
        "ctaTitle": "Prati ovo automatski u timskoj sobi.", "ctaText": "Besplatno, bez registracije. Timska soba čuva sprintove i predlaže budžet.", "cta": "Otvori timsku sobu",
    },
    "de": {
        "title": "Rechner für Sprint-Velocity und Kapazität",
        "nav": "Velocity-Rechner",
        "description": "Kostenloser Velocity-Rechner: Gib die Punkte deiner letzten Sprints und die Verfügbarkeit ein und sieh, wie viele Story Points in den nächsten Sprint passen. Läuft in deinem Browser.",
        "lead": "Wie viele Punkte passen in den nächsten Sprint? Gib ein, was dein Team in den letzten Sprints geschafft hat und wie viele Tage jede Person hat. Nichts verlässt deinen Browser.",
        "app": "Rechner für Sprint-Velocity und Kapazität",
        "body": """
<h2>So entsteht der Vorschlag</h2>
<p>Wenn mindestens zwei vergangene Sprints <strong>Personentage</strong> haben (die Summe der Tage, an denen jedes Mitglied verfügbar war), teilt der Rechner die Punkte jedes Sprints durch seine Personentage. Der Vorschlag ist dieser Wert mal die Personentage des nächsten Sprints. Die Spanne nutzt den langsamsten und den schnellsten dieser Sprints.</p>
<p>Ohne Personentage nimmt er den <strong>Durchschnitt der letzten drei Sprints</strong>, die Spanne ist ihr niedrigstes und höchstes Ergebnis. Beide Wege nutzen nur die letzten drei Sprints, weil ältere Sprints weniger über das heutige Team sagen.</p>

<h2>Eine Orientierung, kein Ziel</h2>
<p>Velocity beschreibt die Vergangenheit. Ein neues Mitglied, ein Feiertag oder ein Sprint voller unbekannter Arbeit verändern sie. Plant bei neuer Arbeit etwas unter dem Vorschlag und sprecht über die Spanne statt über eine einzelne Zahl.</p>

<h2>Lass die App die Zahlen führen</h2>
<p>Ein <a class="link" href="https://app.openplanningpoker.com/team">Team-Raum</a> in Open Planning Poker speichert die Punkte jedes Sprints, fragt die Tage jedes Mitglieds ab und schlägt das Budget nach derselben Regel vor. Du wählst die passenden Tickets und kannst sie in einen Jira-Sprint verschieben.</p>
""",
        "faq": [
            ("Was ist Sprint-Velocity?", "Velocity ist die Zahl der Story Points, die ein Team in einem Sprint fertigstellt. Der Durchschnitt der letzten Sprints ist eine gute Schätzung für den nächsten."),
            ("Was sind Personentage?", "Die Summe der Arbeitstage, an denen jedes Mitglied im Sprint verfügbar ist. Vier Personen mit je zehn Tagen, eine davon zwei Tage abwesend, ergeben 38 Personentage."),
            ("Werden meine Daten irgendwohin geschickt?", "Nein. Der Rechner läuft in deinem Browser und sendet nichts an einen Server."),
        ],
        "ctaTitle": "Verfolge das automatisch in einem Team-Raum.", "ctaText": "Kostenlos, ohne Anmeldung. Der Team-Raum speichert eure Sprints und schlägt das Budget vor.", "cta": "Team-Raum öffnen",
    },
}

PAGES["story-point-scales"] = {
    "kind": "WebPage",
    "en": {
        "title": "Story point scales and a T-shirt size converter",
        "nav": "Story point scales",
        "description": "Compare Fibonacci, modified Fibonacci, powers of 2 and T-shirt sizes side by side with example tickets, and turn T-shirt sizes into points. Free, in your browser.",
        "lead": "Pick the scale that fits your team. The table puts the common decks side by side with an example ticket for each size; the converter turns T-shirt sizes into points.",
        "app": "Story point scale and T-shirt size converter",
        "body": """
<h2>Which scale to choose</h2>
<ul>
  <li><strong>Fibonacci</strong> is the usual default: the gaps grow with the size, so nobody argues about 8 versus 9.</li>
  <li><strong>Modified Fibonacci</strong> rounds the big values to 20, 40 and 100: good for refinement, when some items are still epics.</li>
  <li><strong>Powers of 2</strong> doubles at every step; easy to reason about, fewer options in the middle.</li>
  <li><strong>T-shirt sizes</strong> keep the talk about relative size. Use the converter when you later need numbers for velocity.</li>
</ul>
<p>The example tickets are only a starting point. The best reference is your own team's past work: pin two or three finished tickets per card in a team room and show them while voting.</p>
<p>More background on the special cards (? and the break card) is in the guide to <a class="link" href="/planning-poker-cards/">planning poker cards</a>.</p>
""",
        "faq": [
            ("Why do story point scales have gaps?", "Large items cannot be estimated precisely. Gaps that grow with the size make the team choose between clearly different sizes instead of arguing about small differences."),
            ("How do I turn T-shirt sizes into points?", "Agree on one point value per size with your team, for example S = 2, M = 3, L = 5, and keep it the same across sprints so velocity stays comparable."),
        ],
    },
    "sr": {
        "title": "Skale story pointsa i konverter veličina majica",
        "nav": "Skale story pointsa",
        "description": "Uporedi Fibonacci, modifikovani Fibonacci, stepene dvojke i veličine majica jedne pored drugih uz primere tiketa, i pretvori veličine majica u poene. Besplatno, u browseru.",
        "lead": "Izaberi skalu koja odgovara tvom timu. Tabela stavlja uobičajene špilove jedne pored drugih sa primerom tiketa za svaku veličinu; konverter pretvara veličine majica u poene.",
        "app": "Skale story pointsa i konverter veličina majica",
        "body": """
<h2>Koju skalu izabrati</h2>
<ul>
  <li><strong>Fibonacci</strong> je uobičajen izbor: razmaci rastu sa veličinom, pa niko ne raspravlja o 8 naspram 9.</li>
  <li><strong>Modifikovani Fibonacci</strong> zaokružuje velike vrednosti na 20, 40 i 100: dobar za refinement, kada su neke stavke još epici.</li>
  <li><strong>Stepeni dvojke</strong> se udvostručuju na svakom koraku; lako se razmišlja, ali je manje izbora u sredini.</li>
  <li><strong>Veličine majica</strong> drže razgovor o relativnoj veličini. Koristi konverter kada kasnije trebaju brojevi za brzinu.</li>
</ul>
<p>Primeri tiketa su samo početak. Najbolja referenca je prošli rad tvog tima: zakači dva-tri završena tiketa po karti u timskoj sobi i prikaži ih tokom glasanja.</p>
<p>Više o posebnim kartama (? i karta za pauzu) ima u vodiču o <a class="link" href="/planning-poker-cards/">planning poker kartama</a>.</p>
""",
        "faq": [
            ("Zašto skale story pointsa imaju razmake?", "Velike stavke ne mogu precizno da se procene. Razmaci koji rastu sa veličinom teraju tim da bira između jasno različitih veličina umesto da raspravlja o malim razlikama."),
            ("Kako da pretvorim veličine majica u poene?", "Dogovori sa timom jednu vrednost u poenima po veličini, na primer S = 2, M = 3, L = 5, i drži je istom kroz sprintove da bi brzina ostala uporediva."),
        ],
    },
    "de": {
        "title": "Story-Point-Skalen und ein Umrechner für T-Shirt-Größen",
        "nav": "Story-Point-Skalen",
        "description": "Vergleiche Fibonacci, modifiziertes Fibonacci, Zweierpotenzen und T-Shirt-Größen nebeneinander mit Beispiel-Tickets und rechne T-Shirt-Größen in Punkte um. Kostenlos, im Browser.",
        "lead": "Wähle die Skala, die zu deinem Team passt. Die Tabelle zeigt die üblichen Decks nebeneinander mit einem Beispiel-Ticket je Größe; der Umrechner macht aus T-Shirt-Größen Punkte.",
        "app": "Story-Point-Skalen und Umrechner für T-Shirt-Größen",
        "body": """
<h2>Welche Skala passt</h2>
<ul>
  <li><strong>Fibonacci</strong> ist der übliche Standard: Die Abstände wachsen mit der Größe, also streitet niemand über 8 oder 9.</li>
  <li><strong>Modifiziertes Fibonacci</strong> rundet große Werte auf 20, 40 und 100: gut fürs Refinement, wenn manche Einträge noch Epics sind.</li>
  <li><strong>Zweierpotenzen</strong> verdoppeln sich bei jedem Schritt; leicht zu verstehen, aber weniger Auswahl in der Mitte.</li>
  <li><strong>T-Shirt-Größen</strong> halten das Gespräch bei der relativen Größe. Nutze den Umrechner, wenn ihr später Zahlen für die Velocity braucht.</li>
</ul>
<p>Die Beispiel-Tickets sind nur ein Anfang. Die beste Referenz ist die vergangene Arbeit deines Teams: Pinne im Team-Raum zwei, drei erledigte Tickets pro Karte an und zeig sie beim Abstimmen.</p>
<p>Mehr über die Sonderkarten (? und die Pausenkarte) steht im Leitfaden zu <a class="link" href="/planning-poker-cards/">Planning-Poker-Karten</a>.</p>
""",
        "faq": [
            ("Warum haben Story-Point-Skalen Lücken?", "Große Einträge lassen sich nicht genau schätzen. Mit der Größe wachsende Abstände lassen das Team zwischen klar verschiedenen Größen wählen, statt über kleine Unterschiede zu streiten."),
            ("Wie rechne ich T-Shirt-Größen in Punkte um?", "Einigt euch im Team auf einen Punktwert pro Größe, zum Beispiel S = 2, M = 3, L = 5, und behaltet ihn über die Sprints bei, damit die Velocity vergleichbar bleibt."),
        ],
    },
}

# --- Trackers (OPP-179)

TRACKER = {
    "jira": {"name": "Jira", "source": {"en": "a sprint, a board or a JQL query", "sr": "sprinta, table ili JQL upita", "de": "einem Sprint, einem Board oder einer JQL-Abfrage"},
             "field": "Story Points", "sprint": True},
    "github": {"name": "GitHub", "source": {"en": "a repository, filtered by labels", "sr": "repozitorijuma, filtrirane po labelama", "de": "einem Repository, nach Labels gefiltert"},
               "field": {"en": "an \"estimate: N\" label", "sr": "labelu \"estimate: N\"", "de": "ein Label \"estimate: N\""}, "sprint": False},
    "linear": {"name": "Linear", "source": {"en": "a team's active cycle or its open issues", "sr": "aktivnog ciklusa tima ili njegovih otvorenih issue-a", "de": "dem aktiven Zyklus eines Teams oder seinen offenen Issues"},
               "field": {"en": "the issue's estimate (whole numbers)", "sr": "procenu issue-a (celi brojevi)", "de": "die Schätzung des Issues (ganze Zahlen)"}, "sprint": False},
    "azure-devops": {"name": "Azure DevOps", "source": {"en": "a project, by iteration path or a WIQL query", "sr": "projekta, po putanji iteracije ili WIQL upitu", "de": "einem Projekt, per Iterationspfad oder WIQL-Abfrage"},
                     "field": {"en": "Story Points, Effort or Size, whichever the work item has", "sr": "Story Points, Effort ili Size, šta god work item ima", "de": "Story Points, Effort oder Size, je nachdem, was das Work Item hat"}, "sprint": False},
}

TRACKER_TEXT = {
    "en": {
        "title": "Planning poker for {name}", "nav": "Planning poker for {name}",
        "description": "Free planning poker for {name}: import issues, vote in secret with your team and save the agreed estimate back to {name}. Open source, no sign-up for players.",
        "lead": "Estimate your {name} backlog together. Import the issues, vote with hidden cards, reveal at once and write the agreed estimate back to {name} in one click.",
        "steps": [("Connect {name}", "The host connects {name} once from the import dialog. Players never need an account."),
                  ("Import the issues", "Choose issues from {source}. Each issue becomes a ticket with its link and description."),
                  ("Vote and reveal", "Everyone picks a card in secret. The host reveals, and the room shows the spread, the average and the notes of the lowest and highest voters."),
                  ("Save the estimate", "After the reveal, save the agreed card to {field} with one click, or save all estimates of the session at once.")],
        "howName": "How to run planning poker with {name}",
        "body_extra": "<h2>Plan the sprint too</h2>\n<p>In a team room you set each member's days, get a point budget from past sprints and pick the issues that fit. For Jira, the planned issues move into an active or future sprint of a board in one step.</p>",
        "body_other": "<h2>Sort a long backlog first</h2>\n<p>With many issues, start with the sorting board: players drag issues onto card columns in silence, and only the ones they disagree on get a full vote. All estimates are then saved to {name} together.</p>",
        "privacy": "<h2>What is stored</h2>\n<p>The connection token is stored encrypted on the server and never reaches the browser. Disconnecting deletes it. Read the <a class=\"link\" href=\"/privacy/\">privacy policy</a> for details.</p>",
        "alt": "Planning poker room after the reveal, with the votes and the agreed estimate",
        "faq": [("Is the {name} integration free?", "Yes. Every integration of Open Planning Poker is free, including import and saving estimates back."),
                ("Do players need a {name} account?", "No. Only the host connects {name}; players join the room with a link.")],
    },
    "sr": {
        "title": "Planning poker za {name}", "nav": "Planning poker za {name}",
        "description": "Besplatan planning poker za {name}: uvezi issue-e, glasaj tajno sa timom i sačuvaj dogovorenu procenu nazad u {name}. Otvoren kod, bez registracije za igrače.",
        "lead": "Procenite {name} backlog zajedno. Uvezi issue-e, glasajte skrivenim kartama, otkrijte ih odjednom i upišite dogovorenu procenu nazad u {name} jednim klikom.",
        "steps": [("Poveži {name}", "Host jednom povezuje {name} iz dijaloga za uvoz. Igračima nikad ne treba nalog."),
                  ("Uvezi issue-e", "Izaberi issue-e iz {source}. Svaki issue postaje tiket sa linkom i opisom."),
                  ("Glasajte i otkrijte", "Svi biraju kartu tajno. Host otkriva karte, a soba prikazuje raspon, prosek i beleške najnižeg i najvišeg glasa."),
                  ("Sačuvaj procenu", "Posle otkrivanja sačuvaj dogovorenu kartu u {field} jednim klikom, ili sačuvaj sve procene sesije odjednom.")],
        "howName": "Kako voditi planning poker sa {name}",
        "body_extra": "<h2>Isplaniraj i sprint</h2>\n<p>U timskoj sobi podešavaš dane svakog člana, dobijaš budžet poena iz prošlih sprintova i biraš issue-e koji staju. Za Jiru, planirani issue-i se jednim potezom premeštaju u aktivan ili budući sprint table.</p>",
        "body_other": "<h2>Prvo rasporedi dugačak backlog</h2>\n<p>Kad ima mnogo issue-a, počni od table za sortiranje: igrači u tišini prevlače issue-e na kolone karata, a pravo glasanje dobijaju samo oni oko kojih se ne slažu. Sve procene se zatim zajedno čuvaju u {name}.</p>",
        "privacy": "<h2>Šta se čuva</h2>\n<p>Token veze se čuva šifrovan na serveru i nikad ne stiže do browsera. Prekid veze ga briše. Detalje pročitaj u <a class=\"link\" href=\"/privacy/\">politici privatnosti</a>.</p>",
        "alt": "Soba za planning poker posle otkrivanja, sa glasovima i dogovorenom procenom",
        "faq": [("Da li je integracija sa {name} besplatna?", "Da. Svaka integracija Open Planning Pokera je besplatna, uključujući uvoz i čuvanje procena nazad."),
                ("Da li igračima treba {name} nalog?", "Ne. Samo host povezuje {name}; igrači ulaze u sobu preko linka.")],
    },
    "de": {
        "title": "Planning Poker für {name}", "nav": "Planning Poker für {name}",
        "description": "Kostenloses Planning Poker für {name}: Issues importieren, im Team geheim abstimmen und die vereinbarte Schätzung zurück nach {name} speichern. Open Source, ohne Anmeldung für Spieler.",
        "lead": "Schätzt euer {name}-Backlog gemeinsam. Importiere die Issues, stimmt mit verdeckten Karten ab, deckt gleichzeitig auf und schreibt die vereinbarte Schätzung mit einem Klick zurück nach {name}.",
        "steps": [("{name} verbinden", "Der Host verbindet {name} einmal im Import-Dialog. Spieler brauchen nie ein Konto."),
                  ("Issues importieren", "Wähle Issues aus {source}. Jedes Issue wird ein Ticket mit Link und Beschreibung."),
                  ("Abstimmen und aufdecken", "Alle wählen geheim eine Karte. Der Host deckt auf, und der Raum zeigt die Streuung, den Durchschnitt und die Notizen der niedrigsten und höchsten Stimme."),
                  ("Schätzung speichern", "Nach dem Aufdecken speicherst du die vereinbarte Karte mit einem Klick in {field}, oder alle Schätzungen der Sitzung auf einmal.")],
        "howName": "So läuft Planning Poker mit {name}",
        "body_extra": "<h2>Auch den Sprint planen</h2>\n<p>Im Team-Raum trägst du die Tage jedes Mitglieds ein, bekommst ein Punkte-Budget aus vergangenen Sprints und wählst die passenden Issues. Bei Jira kommen die geplanten Issues in einem Schritt in einen aktiven oder zukünftigen Sprint eines Boards.</p>",
        "body_other": "<h2>Zuerst ein langes Backlog sortieren</h2>\n<p>Bei vielen Issues beginnst du mit dem Sortier-Board: Die Spieler ziehen Issues still auf Kartenspalten, und nur die, bei denen sie sich uneinig sind, bekommen eine richtige Abstimmung. Danach werden alle Schätzungen zusammen in {name} gespeichert.</p>",
        "privacy": "<h2>Was gespeichert wird</h2>\n<p>Das Verbindungs-Token wird verschlüsselt auf dem Server gespeichert und erreicht nie den Browser. Beim Trennen wird es gelöscht. Details stehen in der <a class=\"link\" href=\"/privacy/\">Datenschutzerklärung</a>.</p>",
        "alt": "Planning-Poker-Raum nach dem Aufdecken, mit den Stimmen und der vereinbarten Schätzung",
        "faq": [("Ist die {name}-Integration kostenlos?", "Ja. Jede Integration von Open Planning Poker ist kostenlos, auch der Import und das Zurückspeichern der Schätzungen."),
                ("Brauchen Spieler ein {name}-Konto?", "Nein. Nur der Host verbindet {name}; die Spieler treten dem Raum über einen Link bei.")],
    },
}


def tracker_page(key, lang):
    tr = TRACKER[key]
    t = TRACKER_TEXT[lang]
    name = tr["name"]
    source = tr["source"][lang]
    field = tr["field"] if isinstance(tr["field"], str) else tr["field"][lang]
    f = lambda s: s.format(name=name, source=source, field=field)  # noqa: E731
    steps = [(f(a), f(b)) for a, b in t["steps"]]
    ol = "\n".join(f"  <li><strong>{a}</strong>: {b}</li>" for a, b in steps)
    body = f"<h2>{f(t['howName'])}</h2>\n<ol>\n{ol}\n</ol>\n{picture('room', t['alt'])}\n" + (t["body_extra"] if tr["sprint"] else f(t["body_other"])) + "\n" + t["privacy"]
    return {
        "title": f(t["title"]), "nav": f(t["nav"]), "description": f(t["description"]), "lead": f(t["lead"]), "body": body,
        "howto": {"name": f(t["howName"]), "steps": steps}, "faq": [(f(q), f(a)) for q, a in t["faq"]],
    }


for key in TRACKER:
    PAGES[f"planning-poker-for-{key}"] = {"kind": "WebPage", **{lang: tracker_page(key, lang) for lang in ("en", "sr", "de")}}

# --- Use cases (OPP-179)

PAGES["remote-planning-poker"] = {
    "kind": "Article",
    "en": {
        "title": "Planning poker for remote teams",
        "nav": "Remote teams",
        "description": "Run planning poker with a remote or hybrid team: one link to join, hidden votes, a shared reveal and timer, async voting across time zones, and Slack and Teams integration.",
        "lead": "Remote estimation fails when people talk over each other or wait for the loudest voice. A shared room with hidden cards keeps everyone's first opinion honest, wherever they sit.",
        "body": f"""
<h2>What a remote session needs</h2>
<ul>
  <li><strong>One link, no accounts.</strong> People join from the meeting chat in seconds, on a laptop or a phone.</li>
  <li><strong>Hidden votes and one reveal.</strong> Nobody anchors on the first number said out loud.</li>
  <li><strong>A shared timer.</strong> The discussion after a split vote stays short.</li>
  <li><strong>Notes from the lowest and highest voters.</strong> Quiet people get heard without having to interrupt.</li>
</ul>
{picture('voting', 'Players voting in secret with a shared timer')}
<h2>Across time zones</h2>
<p>With <strong>async voting</strong>, everyone votes on the tickets before the meeting; the votes stay hidden until the reveal. The call then only covers the tickets the team disagrees on.</p>

<h2>Inside the tools you already use</h2>
<p>Start a room from Slack with <code>/poker</code>, or add it as a tab to a Microsoft Teams meeting. See the <a class="link" href="/integrations/">integrations</a>.</p>
""",
    },
    "sr": {
        "title": "Planning poker za timove na daljinu",
        "nav": "Timovi na daljinu",
        "description": "Vodi planning poker sa timom na daljinu ili hibridnim timom: jedan link za ulazak, tajni glasovi, zajedničko otkrivanje i tajmer, asinhrono glasanje kroz vremenske zone, Slack i Teams.",
        "lead": "Procena na daljinu ne uspeva kad ljudi pričaju jedni preko drugih ili čekaju najglasnijeg. Zajednička soba sa skrivenim kartama čuva iskreno prvo mišljenje svakog, gde god da sedi.",
        "body": f"""
<h2>Šta treba sesiji na daljinu</h2>
<ul>
  <li><strong>Jedan link, bez naloga.</strong> Ljudi ulaze iz chata sastanka za par sekundi, sa laptopa ili telefona.</li>
  <li><strong>Tajni glasovi i jedno otkrivanje.</strong> Niko se ne vezuje za prvi broj izgovoren naglas.</li>
  <li><strong>Zajednički tajmer.</strong> Diskusija posle podeljenog glasanja ostaje kratka.</li>
  <li><strong>Beleške najnižeg i najvišeg glasa.</strong> Tihi ljudi budu saslušani a da ne moraju da prekidaju.</li>
</ul>
{picture('voting', 'Igrači glasaju tajno uz zajednički tajmer')}
<h2>Kroz vremenske zone</h2>
<p>Uz <strong>asinhrono glasanje</strong> svi glasaju o tiketima pre sastanka; glasovi ostaju skriveni do otkrivanja. Poziv onda pokriva samo tikete oko kojih se tim ne slaže.</p>

<h2>U alatima koje već koristiš</h2>
<p>Pokreni sobu iz Slacka komandom <code>/poker</code> ili je dodaj kao tab u Microsoft Teams sastanak. Pogledaj <a class="link" href="/integrations/">integracije</a>.</p>
""",
    },
    "de": {
        "title": "Planning Poker für Remote-Teams",
        "nav": "Remote-Teams",
        "description": "Planning Poker mit einem Remote- oder Hybrid-Team: ein Link zum Beitreten, geheime Stimmen, gemeinsames Aufdecken und Timer, asynchrones Abstimmen über Zeitzonen, Slack und Teams.",
        "lead": "Remote-Schätzungen scheitern, wenn alle durcheinanderreden oder auf die lauteste Stimme warten. Ein gemeinsamer Raum mit verdeckten Karten hält die erste Meinung aller ehrlich, egal wo sie sitzen.",
        "body": f"""
<h2>Was eine Remote-Sitzung braucht</h2>
<ul>
  <li><strong>Ein Link, keine Konten.</strong> Man tritt in Sekunden aus dem Meeting-Chat bei, am Laptop oder am Handy.</li>
  <li><strong>Geheime Stimmen und ein Aufdecken.</strong> Niemand richtet sich nach der ersten laut genannten Zahl.</li>
  <li><strong>Ein gemeinsamer Timer.</strong> Die Diskussion nach einer geteilten Abstimmung bleibt kurz.</li>
  <li><strong>Notizen der niedrigsten und höchsten Stimme.</strong> Stille Leute werden gehört, ohne unterbrechen zu müssen.</li>
</ul>
{picture('voting', 'Spieler stimmen geheim mit einem gemeinsamen Timer ab')}
<h2>Über Zeitzonen hinweg</h2>
<p>Mit <strong>asynchronem Abstimmen</strong> stimmen alle vor dem Meeting über die Tickets ab; die Stimmen bleiben bis zum Aufdecken verborgen. Im Call geht es dann nur um die Tickets, bei denen das Team uneinig ist.</p>

<h2>In den Tools, die ihr schon nutzt</h2>
<p>Starte einen Raum in Slack mit <code>/poker</code> oder füge ihn als Tab in ein Microsoft-Teams-Meeting ein. Siehe <a class="link" href="/integrations/">Integrationen</a>.</p>
""",
    },
}

PAGES["scrum-estimation-for-beginners"] = {
    "kind": "Article",
    "en": {
        "title": "Story point estimation for Scrum beginners",
        "nav": "Scrum for beginners",
        "description": "New to Scrum estimation? What story points are, how a first planning poker session works step by step, and the mistakes new teams make. With a free tool to try it.",
        "lead": "Story points measure how big a piece of work is compared with other work, not how many hours it takes. Planning poker is the simplest way for a new team to agree on them.",
        "body": """
<h2>Your first session in five steps</h2>
<ol>
  <li><strong>Pick a reference ticket</strong> the whole team knows and call it a 3.</li>
  <li><strong>Read the next ticket</strong> out loud; answer questions about what it means, not how long it takes.</li>
  <li><strong>Everyone votes in secret</strong>, comparing it with the reference: smaller, about the same, or bigger?</li>
  <li><strong>Reveal together.</strong> If the votes are far apart, the lowest and highest voters say why.</li>
  <li><strong>Vote again once</strong> and take the card most people agree on. Move on.</li>
</ol>
<h2>Mistakes new teams make</h2>
<ul>
  <li>Turning points back into hours. Points only work as relative size.</li>
  <li>Discussing every ticket for ten minutes. Use a timer; a close vote is good enough.</li>
  <li>Comparing velocity between teams. Every team's scale is its own.</li>
</ul>
<p>Next, read <a class="link" href="/what-is-planning-poker/">what planning poker is</a> and <a class="link" href="/how-to-run-planning-poker/">how to run a session</a>, or pick a scale with the <a class="link" href="/story-point-scales/">story point scales</a> tool.</p>
""",
        "howto": {"name": "How to run a first planning poker session", "steps": [
            ("Pick a reference ticket", "Choose a ticket the whole team knows and call it a 3."),
            ("Read the next ticket", "Read it out loud and answer questions about what it means."),
            ("Vote in secret", "Everyone compares the ticket with the reference and picks a card."),
            ("Reveal together", "If the votes are far apart, the lowest and highest voters explain why."),
            ("Vote again once", "Take the card most people agree on and move on.")]},
    },
    "sr": {
        "title": "Procena story pointsa za Scrum početnike",
        "nav": "Scrum za početnike",
        "description": "Nov si u Scrum procenama? Šta su story points, kako izgleda prva planning poker sesija korak po korak i koje greške prave novi timovi. Uz besplatan alat da probaš.",
        "lead": "Story points mere koliko je neki posao veliki u odnosu na drugi posao, a ne koliko sati traje. Planning poker je najjednostavniji način da se nov tim dogovori oko njih.",
        "body": """
<h2>Prva sesija u pet koraka</h2>
<ol>
  <li><strong>Izaberi referentni tiket</strong> koji ceo tim zna i nazovi ga 3.</li>
  <li><strong>Pročitaj sledeći tiket</strong> naglas; odgovaraj na pitanja o tome šta znači, a ne koliko traje.</li>
  <li><strong>Svi glasaju tajno</strong>, upoređujući ga sa referencom: manji, otprilike isti ili veći?</li>
  <li><strong>Otkrijte zajedno.</strong> Ako su glasovi daleko, najniži i najviši glas kažu zašto.</li>
  <li><strong>Glasajte još jednom</strong> i uzmite kartu oko koje se većina slaže. Idite dalje.</li>
</ol>
<h2>Greške koje prave novi timovi</h2>
<ul>
  <li>Pretvaranje poena nazad u sate. Poeni rade samo kao relativna veličina.</li>
  <li>Diskusija od deset minuta o svakom tiketu. Koristi tajmer; blizak glas je dovoljno dobar.</li>
  <li>Poređenje brzine između timova. Svaki tim ima svoju skalu.</li>
</ul>
<p>Dalje pročitaj <a class="link" href="/what-is-planning-poker/">šta je planning poker</a> i <a class="link" href="/how-to-run-planning-poker/">kako se vodi sesija</a>, ili izaberi skalu alatom <a class="link" href="/story-point-scales/">skale story pointsa</a>.</p>
""",
        "howto": {"name": "Kako voditi prvu planning poker sesiju", "steps": [
            ("Izaberi referentni tiket", "Izaberi tiket koji ceo tim zna i nazovi ga 3."),
            ("Pročitaj sledeći tiket", "Pročitaj ga naglas i odgovori na pitanja o tome šta znači."),
            ("Glasajte tajno", "Svi upoređuju tiket sa referencom i biraju kartu."),
            ("Otkrijte zajedno", "Ako su glasovi daleko, najniži i najviši glas objašnjavaju zašto."),
            ("Glasajte još jednom", "Uzmite kartu oko koje se većina slaže i idite dalje.")]},
    },
    "de": {
        "title": "Story-Point-Schätzung für Scrum-Einsteiger",
        "nav": "Scrum für Einsteiger",
        "description": "Neu beim Schätzen in Scrum? Was Story Points sind, wie die erste Planning-Poker-Sitzung Schritt für Schritt abläuft und welche Fehler neue Teams machen. Mit kostenlosem Tool zum Ausprobieren.",
        "lead": "Story Points messen, wie groß eine Arbeit im Vergleich zu anderer Arbeit ist, nicht wie viele Stunden sie dauert. Planning Poker ist der einfachste Weg, damit sich ein neues Team darauf einigt.",
        "body": """
<h2>Die erste Sitzung in fünf Schritten</h2>
<ol>
  <li><strong>Wähle ein Referenz-Ticket</strong>, das das ganze Team kennt, und nenne es eine 3.</li>
  <li><strong>Lies das nächste Ticket</strong> laut vor; beantworte Fragen dazu, was es bedeutet, nicht wie lange es dauert.</li>
  <li><strong>Alle stimmen geheim ab</strong> und vergleichen es mit der Referenz: kleiner, etwa gleich oder größer?</li>
  <li><strong>Gemeinsam aufdecken.</strong> Liegen die Stimmen weit auseinander, sagen die niedrigste und die höchste Stimme warum.</li>
  <li><strong>Einmal neu abstimmen</strong> und die Karte nehmen, auf die sich die meisten einigen. Weiter.</li>
</ol>
<h2>Fehler, die neue Teams machen</h2>
<ul>
  <li>Punkte wieder in Stunden umrechnen. Punkte funktionieren nur als relative Größe.</li>
  <li>Jedes Ticket zehn Minuten diskutieren. Nutzt einen Timer; eine knappe Abstimmung reicht.</li>
  <li>Velocity zwischen Teams vergleichen. Jedes Team hat seine eigene Skala.</li>
</ul>
<p>Lies als Nächstes, <a class="link" href="/what-is-planning-poker/">was Planning Poker ist</a> und <a class="link" href="/how-to-run-planning-poker/">wie eine Sitzung abläuft</a>, oder wähle eine Skala mit dem Tool <a class="link" href="/story-point-scales/">Story-Point-Skalen</a>.</p>
""",
        "howto": {"name": "So läuft die erste Planning-Poker-Sitzung", "steps": [
            ("Referenz-Ticket wählen", "Wähle ein Ticket, das das ganze Team kennt, und nenne es eine 3."),
            ("Nächstes Ticket lesen", "Lies es laut vor und beantworte Fragen dazu, was es bedeutet."),
            ("Geheim abstimmen", "Alle vergleichen das Ticket mit der Referenz und wählen eine Karte."),
            ("Gemeinsam aufdecken", "Liegen die Stimmen weit auseinander, erklären die niedrigste und die höchste Stimme warum."),
            ("Einmal neu abstimmen", "Nehmt die Karte, auf die sich die meisten einigen, und macht weiter.")]},
    },
}

PAGES["magic-estimation"] = {
    "kind": "Article",
    "en": {
        "title": "Magic estimation: size a long backlog in minutes",
        "nav": "Magic estimation",
        "description": "Magic estimation (silent sorting) sizes dozens of backlog items in minutes: players place tickets on card columns without talking, and only disputed tickets get a full planning poker vote.",
        "lead": "Planning poker takes a few minutes per ticket. With fifty tickets, that is an afternoon. Magic estimation sorts them first, in silence, and saves the talk for the ones the team disagrees on.",
        "body": f"""
<h2>How it works</h2>
<ol>
  <li>The host opens the <strong>sorting board</strong>: one column per card of the deck.</li>
  <li>Players drag tickets onto the column they think fits, <strong>without talking</strong>. Anyone may move any ticket.</li>
  <li>A ticket moved three times, or moved by someone else to another card, is <strong>marked for a talk</strong>.</li>
  <li>The host sends the marked tickets to a normal vote and turns the rest into <strong>final estimates</strong> in one step.</li>
</ol>
{picture('room', 'Planning poker room with the agreed estimates')}
<h2>When to use it</h2>
<p>Use it for refinement of a large backlog, for a new project, or before release planning. Use normal planning poker for the handful of tickets of the next sprint, where the details matter.</p>
<p>The estimates count in velocity, the reports and the reference history like voted ones, and can be saved to <a class="link" href="/planning-poker-for-jira/">Jira</a>, GitHub, Linear or Azure DevOps together.</p>
""",
        "howto": {"name": "How to run magic estimation", "time": "PT15M", "steps": [
            ("Open the sorting board", "The host opens a board with one column per card of the deck."),
            ("Sort in silence", "Players drag tickets onto the card they think fits, without talking."),
            ("Talk about the disputed ones", "Tickets that keep moving are marked; the host sends them to a normal vote."),
            ("Apply the estimates", "The host turns the columns into final estimates in one step.")]},
    },
    "sr": {
        "title": "Magična procena: rasporedi dugačak backlog za nekoliko minuta",
        "nav": "Magična procena",
        "description": "Magična procena (tiho sortiranje) procenjuje desetine stavki iz backloga za nekoliko minuta: igrači bez razgovora stavljaju tikete na kolone karata, a pravo glasanje dobijaju samo sporni tiketi.",
        "lead": "Planning poker traje nekoliko minuta po tiketu. Sa pedeset tiketa to je celo popodne. Magična procena ih prvo raspoređuje u tišini, a razgovor čuva za one oko kojih se tim ne slaže.",
        "body": f"""
<h2>Kako radi</h2>
<ol>
  <li>Host otvara <strong>tablu za sortiranje</strong>: po jedna kolona za svaku kartu špila.</li>
  <li>Igrači prevlače tikete na kolonu za koju misle da odgovara, <strong>bez razgovora</strong>. Svako može da pomeri bilo koji tiket.</li>
  <li>Tiket pomeren tri puta, ili koji je neko drugi premestio na drugu kartu, dobija <strong>oznaku za razgovor</strong>.</li>
  <li>Host šalje označene tikete na obično glasanje, a ostale jednim potezom pretvara u <strong>konačne procene</strong>.</li>
</ol>
{picture('room', 'Soba za planning poker sa dogovorenim procenama')}
<h2>Kada je koristiti</h2>
<p>Koristi je za refinement velikog backloga, za nov projekat ili pre planiranja izdanja. Za nekoliko tiketa sledećeg sprinta, gde su detalji bitni, koristi običan planning poker.</p>
<p>Procene se računaju u brzinu, izveštaje i istoriju referenci kao i one iz glasanja, i mogu zajedno da se sačuvaju u <a class="link" href="/planning-poker-for-jira/">Jiru</a>, GitHub, Linear ili Azure DevOps.</p>
""",
        "howto": {"name": "Kako voditi magičnu procenu", "time": "PT15M", "steps": [
            ("Otvori tablu za sortiranje", "Host otvara tablu sa po jednom kolonom za svaku kartu špila."),
            ("Sortirajte u tišini", "Igrači bez razgovora prevlače tikete na kartu za koju misle da odgovara."),
            ("Razgovarajte o spornim", "Tiketi koji se stalno pomeraju dobijaju oznaku; host ih šalje na obično glasanje."),
            ("Primeni procene", "Host jednim potezom pretvara kolone u konačne procene.")]},
    },
    "de": {
        "title": "Magic Estimation: ein langes Backlog in Minuten schätzen",
        "nav": "Magic Estimation",
        "description": "Magic Estimation (stilles Sortieren) schätzt Dutzende Backlog-Einträge in Minuten: Die Spieler legen Tickets ohne zu reden auf Kartenspalten, nur strittige Tickets bekommen eine richtige Abstimmung.",
        "lead": "Planning Poker braucht ein paar Minuten pro Ticket. Bei fünfzig Tickets ist das ein Nachmittag. Magic Estimation sortiert sie zuerst still und spart das Gespräch für die, bei denen das Team uneinig ist.",
        "body": f"""
<h2>So funktioniert es</h2>
<ol>
  <li>Der Host öffnet das <strong>Sortier-Board</strong>: eine Spalte pro Karte des Decks.</li>
  <li>Die Spieler ziehen Tickets auf die Spalte, die sie für passend halten, <strong>ohne zu reden</strong>. Jeder darf jedes Ticket bewegen.</li>
  <li>Ein Ticket, das dreimal bewegt oder von jemand anderem auf eine andere Karte gezogen wurde, wird <strong>für ein Gespräch markiert</strong>.</li>
  <li>Der Host schickt die markierten Tickets in eine normale Abstimmung und macht den Rest in einem Schritt zu <strong>endgültigen Schätzungen</strong>.</li>
</ol>
{picture('room', 'Planning-Poker-Raum mit den vereinbarten Schätzungen')}
<h2>Wann es passt</h2>
<p>Nutze es fürs Refinement eines großen Backlogs, für ein neues Projekt oder vor der Release-Planung. Für die paar Tickets des nächsten Sprints, bei denen Details zählen, nimm normales Planning Poker.</p>
<p>Die Schätzungen zählen in Velocity, Berichten und Referenz-Historie wie abgestimmte und lassen sich zusammen in <a class="link" href="/planning-poker-for-jira/">Jira</a>, GitHub, Linear oder Azure DevOps speichern.</p>
""",
        "howto": {"name": "So läuft Magic Estimation", "time": "PT15M", "steps": [
            ("Sortier-Board öffnen", "Der Host öffnet ein Board mit einer Spalte pro Karte des Decks."),
            ("Still sortieren", "Die Spieler ziehen Tickets ohne zu reden auf die passende Karte."),
            ("Über strittige sprechen", "Tickets, die immer wieder bewegt werden, werden markiert; der Host schickt sie in eine normale Abstimmung."),
            ("Schätzungen übernehmen", "Der Host macht die Spalten in einem Schritt zu endgültigen Schätzungen.")]},
    },
}

PAGES["sprint-planning-tool"] = {
    "kind": "Article",
    "en": {
        "title": "Free sprint planning tool: capacity, budget and the plan",
        "nav": "Sprint planning",
        "description": "Plan a sprint from your team's own numbers: each member's available days, a point budget from past sprints, the tickets that fit, and a move into a Jira sprint. Free and open source.",
        "lead": "A sprint plan is a guess about the future. It gets better when it starts from how much the team really finished and who is around this time.",
        "body": f"""
<h2>From estimates to a plan</h2>
<ol>
  <li><strong>Estimate</strong> the candidate tickets with planning poker or the sorting board.</li>
  <li><strong>Set the capacity</strong>: the sprint length and each member's available days, in half days.</li>
  <li><strong>Get a budget</strong> from the last sprints: points per person-day when the days are known, otherwise the average.</li>
  <li><strong>Pick the tickets</strong> that fit; a bar shows how full the sprint is. Copy the plan as text, or move the Jira issues into a sprint.</li>
</ol>
<p>Try the formula without an account in the <a class="link" href="/velocity-calculator/">velocity calculator</a>.</p>
{picture('new-game', 'Starting a planning session with a chosen deck')}
<h2>After the sprint</h2>
<p>Run the retro in the same team room: write in silence, group, vote with dots and agree on action items, with the sprint's numbers on the board. Open action items come back at the next retro.</p>
""",
        "howto": {"name": "How to plan a sprint from your team's numbers", "steps": [
            ("Estimate", "Estimate the candidate tickets with planning poker or the sorting board."),
            ("Set the capacity", "Enter the sprint length and each member's available days."),
            ("Get a budget", "Use the points per person-day of the last sprints, or their average."),
            ("Pick the tickets", "Choose the tickets that fit and copy the plan or move it into a Jira sprint.")]},
    },
    "sr": {
        "title": "Besplatan alat za planiranje sprinta: kapacitet, budžet i plan",
        "nav": "Planiranje sprinta",
        "description": "Planiraj sprint na osnovu brojki svog tima: dostupni dani svakog člana, budžet poena iz prošlih sprintova, tiketi koji staju i premeštanje u Jira sprint. Besplatno i otvorenog koda.",
        "lead": "Plan sprinta je pretpostavka o budućnosti. Bolji je kad polazi od toga koliko je tim stvarno završio i ko je tu ovog puta.",
        "body": f"""
<h2>Od procena do plana</h2>
<ol>
  <li><strong>Proceni</strong> kandidat tikete planning pokerom ili tablom za sortiranje.</li>
  <li><strong>Podesi kapacitet</strong>: dužinu sprinta i dostupne dane svakog člana, u polovinama dana.</li>
  <li><strong>Dobij budžet</strong> iz poslednjih sprintova: poeni po čovek-danu kad su dani poznati, inače prosek.</li>
  <li><strong>Izaberi tikete</strong> koji staju; traka pokazuje koliko je sprint pun. Kopiraj plan kao tekst ili premesti Jira issue-e u sprint.</li>
</ol>
<p>Probaj formulu bez naloga u <a class="link" href="/velocity-calculator/">kalkulatoru brzine</a>.</p>
{picture('new-game', 'Početak sesije planiranja sa izabranim špilom')}
<h2>Posle sprinta</h2>
<p>Vodi retrospektivu u istoj timskoj sobi: pišite u tišini, grupišite, glasajte tačkicama i dogovorite akcije, uz brojke sprinta na tabli. Otvorene akcije se vraćaju na sledećoj retrospektivi.</p>
""",
        "howto": {"name": "Kako isplanirati sprint na osnovu brojki tima", "steps": [
            ("Proceni", "Proceni kandidat tikete planning pokerom ili tablom za sortiranje."),
            ("Podesi kapacitet", "Unesi dužinu sprinta i dostupne dane svakog člana."),
            ("Dobij budžet", "Koristi poene po čovek-danu iz poslednjih sprintova, ili njihov prosek."),
            ("Izaberi tikete", "Izaberi tikete koji staju i kopiraj plan ili ga premesti u Jira sprint.")]},
    },
    "de": {
        "title": "Kostenloses Tool für die Sprint-Planung: Kapazität, Budget und Plan",
        "nav": "Sprint-Planung",
        "description": "Plane einen Sprint mit den Zahlen deines Teams: verfügbare Tage jedes Mitglieds, ein Punkte-Budget aus vergangenen Sprints, die passenden Tickets und das Verschieben in einen Jira-Sprint. Kostenlos und Open Source.",
        "lead": "Ein Sprint-Plan ist eine Vermutung über die Zukunft. Sie wird besser, wenn sie davon ausgeht, was das Team wirklich geschafft hat und wer diesmal da ist.",
        "body": f"""
<h2>Von Schätzungen zum Plan</h2>
<ol>
  <li><strong>Schätze</strong> die Kandidaten-Tickets mit Planning Poker oder dem Sortier-Board.</li>
  <li><strong>Lege die Kapazität fest</strong>: die Sprint-Länge und die verfügbaren Tage jedes Mitglieds, in halben Tagen.</li>
  <li><strong>Hol dir ein Budget</strong> aus den letzten Sprints: Punkte pro Personentag, wenn die Tage bekannt sind, sonst den Durchschnitt.</li>
  <li><strong>Wähle die Tickets</strong>, die passen; ein Balken zeigt, wie voll der Sprint ist. Kopiere den Plan als Text oder verschiebe die Jira-Issues in einen Sprint.</li>
</ol>
<p>Probier die Formel ohne Konto im <a class="link" href="/velocity-calculator/">Velocity-Rechner</a> aus.</p>
{picture('new-game', 'Start einer Planungssitzung mit dem gewählten Deck')}
<h2>Nach dem Sprint</h2>
<p>Führt die Retro im selben Team-Raum: still schreiben, gruppieren, mit Punkten abstimmen und Maßnahmen vereinbaren, mit den Zahlen des Sprints auf dem Board. Offene Maßnahmen kommen in der nächsten Retro wieder.</p>
""",
        "howto": {"name": "So planst du einen Sprint mit den Zahlen deines Teams", "steps": [
            ("Schätzen", "Schätze die Kandidaten-Tickets mit Planning Poker oder dem Sortier-Board."),
            ("Kapazität festlegen", "Trag die Sprint-Länge und die verfügbaren Tage jedes Mitglieds ein."),
            ("Budget holen", "Nutze die Punkte pro Personentag der letzten Sprints oder ihren Durchschnitt."),
            ("Tickets wählen", "Wähle die passenden Tickets und kopiere den Plan oder verschiebe ihn in einen Jira-Sprint.")]},
    },
}

# --- Comparison (OPP-180). Facts from each product's own pages, checked on CHECKED; "not stated" means its site does not say.

SOURCES = {
    "planitpoker": ["https://www.planitpoker.com/pricing", "https://www.planitpoker.com/"],
    "pointingpoker": ["https://www.pointingpoker.com/", "https://www.pointingpoker.com/About"],
    "parabol": ["https://www.parabol.co/pricing/", "https://www.parabol.co/agile/sprint-poker/", "https://github.com/ParabolInc/parabol"],
    "kollabe": ["https://kollabe.com/pricing", "https://kollabe.com/planning-poker"],
}
TOOLS = ["opp", "planitpoker", "pointingpoker", "parabol", "kollabe"]
TOOL_NAMES = {"opp": "Open Planning Poker", "planitpoker": "PlanITpoker", "pointingpoker": "Pointing Poker", "parabol": "Parabol", "kollabe": "Kollabe"}

COMPARE = {
    "en": {
        "na": "Not stated",
        "rows": [
            ("Free plan", ["Everything, no limits", "Up to 7 participants, unlimited rooms", "Free with ads, no paid plan", "Up to 2 teams, 10 meetings a month, 30 days of history", "10 members per room, 4 poker meetings a month, 7 days of history"]),
            ("Cheapest paid plan", ["None", "$20 a month", "None", "$8 per active user a month", "$12 per space a month"]),
            ("Source code", ["Open source (GPL-3.0)", None, None, "Public, Business Source License 1.1 (production use needs a commercial license)", None]),
            ("Self-hosting", ["Yes, free (Docker)", None, None, "On-premises on the Enterprise plan", None]),
            ("Issue trackers", ["Jira, GitHub, Linear, Azure DevOps, free", "Import from Jira XML or CSV files", "None listed", "Jira, GitHub, GitLab and more, free", "Jira, GitHub, Azure DevOps, Linear on paid plans"]),
            ("Async voting", ["Yes", None, None, None, "Yes"]),
            ("Backlog sorting / magic estimation", ["Yes, sorting board", "Reorder stories", None, None, "Reorder tickets in a game"]),
            ("Retrospectives", ["Yes, in the team room", "Separate product", "Yes", "Yes", "Yes, 4 a month on the free plan"]),
        ],
        "feature": "Feature", "checked": "Checked on {date} from each product's own pages. Tell us on GitHub if something changed.",
        "sources": "Sources",
    },
    "sr": {
        "na": "Nije navedeno",
        "rows": [
            ("Besplatan plan", ["Sve, bez ograničenja", "Do 7 učesnika, neograničeno soba", "Besplatno uz reklame, bez plaćenog plana", "Do 2 tima, 10 sastanaka mesečno, 30 dana istorije", "10 članova po sobi, 4 poker sastanka mesečno, 7 dana istorije"]),
            ("Najjeftiniji plaćeni plan", ["Nema", "20 $ mesečno", "Nema", "8 $ po aktivnom korisniku mesečno", "12 $ po prostoru mesečno"]),
            ("Izvorni kod", ["Otvoren kod (GPL-3.0)", None, None, "Javan, Business Source License 1.1 (produkcija traži komercijalnu licencu)", None]),
            ("Self-hosting", ["Da, besplatno (Docker)", None, None, "Na sopstvenim serverima u Enterprise planu", None]),
            ("Alati za tikete", ["Jira, GitHub, Linear, Azure DevOps, besplatno", "Uvoz iz Jira XML ili CSV fajlova", "Nisu navedeni", "Jira, GitHub, GitLab i drugi, besplatno", "Jira, GitHub, Azure DevOps, Linear u plaćenim planovima"]),
            ("Asinhrono glasanje", ["Da", None, None, None, "Da"]),
            ("Sortiranje backloga / magična procena", ["Da, tabla za sortiranje", "Promena redosleda priča", None, None, "Promena redosleda tiketa u igri"]),
            ("Retrospektive", ["Da, u timskoj sobi", "Poseban proizvod", "Da", "Da", "Da, 4 mesečno u besplatnom planu"]),
        ],
        "feature": "Mogućnost", "checked": "Provereno {date} na zvaničnim stranicama svakog proizvoda. Javi nam na GitHubu ako se nešto promenilo.",
        "sources": "Izvori",
    },
    "de": {
        "na": "Nicht angegeben",
        "rows": [
            ("Kostenloser Plan", ["Alles, ohne Grenzen", "Bis 7 Teilnehmende, unbegrenzt Räume", "Kostenlos mit Werbung, kein bezahlter Plan", "Bis 2 Teams, 10 Meetings im Monat, 30 Tage Verlauf", "10 Mitglieder pro Raum, 4 Poker-Meetings im Monat, 7 Tage Verlauf"]),
            ("Günstigster bezahlter Plan", ["Keiner", "20 $ im Monat", "Keiner", "8 $ pro aktivem Nutzer im Monat", "12 $ pro Space im Monat"]),
            ("Quellcode", ["Open Source (GPL-3.0)", None, None, "Öffentlich, Business Source License 1.1 (Produktion braucht eine kommerzielle Lizenz)", None]),
            ("Self-Hosting", ["Ja, kostenlos (Docker)", None, None, "On-Premises im Enterprise-Plan", None]),
            ("Issue-Tracker", ["Jira, GitHub, Linear, Azure DevOps, kostenlos", "Import aus Jira-XML- oder CSV-Dateien", "Keine genannt", "Jira, GitHub, GitLab und mehr, kostenlos", "Jira, GitHub, Azure DevOps, Linear in bezahlten Plänen"]),
            ("Asynchrones Abstimmen", ["Ja", None, None, None, "Ja"]),
            ("Backlog sortieren / Magic Estimation", ["Ja, Sortier-Board", "Stories umsortieren", None, None, "Tickets im Spiel umsortieren"]),
            ("Retrospektiven", ["Ja, im Team-Raum", "Eigenes Produkt", "Ja", "Ja", "Ja, 4 im Monat im kostenlosen Plan"]),
        ],
        "feature": "Funktion", "checked": "Geprüft am {date} auf den eigenen Seiten jedes Produkts. Sag uns auf GitHub Bescheid, wenn sich etwas geändert hat.",
        "sources": "Quellen",
    },
}


def compare_table(lang):
    import html as h
    c = COMPARE[lang]
    head = "".join(f'<th scope="col">{h.escape(TOOL_NAMES[t])}</th>' for t in TOOLS)
    na = f'<span class="text-gray-400">{h.escape(c["na"])}</span>'
    rows = "\n".join(
        f'    <tr><th scope="row">{h.escape(label)}</th>' + "".join(
            f'<td>{h.escape(v) if v else na}</td>' for v in values) + "</tr>"
        for label, values in c["rows"])
    sources = "\n".join(
        f'  <li><strong>{TOOL_NAMES[t]}</strong>: ' + ", ".join(f'<a class="link" href="{u}" rel="nofollow noopener">{u.split("//")[1]}</a>' for u in urls) + "</li>"
        for t, urls in SOURCES.items())
    return (f'<div class="table-wrap">\n<table>\n  <thead><tr><th scope="col">{h.escape(c["feature"])}</th>{head}</tr></thead>\n  <tbody>\n{rows}\n  </tbody>\n</table>\n</div>\n'
            f'<p class="text-sm">{h.escape(c["checked"].format(date=CHECKED))}</p>\n<h2>{h.escape(c["sources"])}</h2>\n<ul>\n{sources}\n</ul>')


PAGES["planning-poker-tools-compared"] = {
    "kind": "Article",
    "wide": True,
    "en": {
        "title": "Planning poker tools compared",
        "nav": "Tools compared",
        "description": "A fair comparison of planning poker tools: free plan limits, prices, open source, self-hosting, issue trackers, async voting, backlog sorting and retros, with sources and the date checked.",
        "lead": "Every team needs something a little different. This table lists facts from each product's own website, so you can pick the tool that fits; where a site does not say, the table says so.",
        "body": "<h2>The table</h2>\n" + compare_table("en") + """
<h2>How to read it</h2>
<p>A free plan with a participant or meeting limit is fine for a small team that estimates now and then. For a team that estimates every sprint, check the history limit too: it decides whether you can look back at past estimates and velocity.</p>
<p>Names of other products are trademarks of their owners and are used only to describe them.</p>
""",
    },
    "sr": {
        "title": "Poređenje alata za planning poker",
        "nav": "Poređenje alata",
        "description": "Pošteno poređenje alata za planning poker: ograničenja besplatnog plana, cene, otvoren kod, self-hosting, alati za tikete, asinhrono glasanje, sortiranje backloga i retrospektive, uz izvore i datum provere.",
        "lead": "Svakom timu treba nešto malo drugačije. Tabela navodi činjenice sa zvaničnog sajta svakog proizvoda, da izabereš alat koji ti odgovara; gde sajt ne kaže, tabela to i navodi.",
        "body": "<h2>Tabela</h2>\n" + compare_table("sr") + """
<h2>Kako je čitati</h2>
<p>Besplatan plan sa ograničenjem učesnika ili sastanaka je dovoljan malom timu koji procenjuje povremeno. Za tim koji procenjuje svaki sprint proveri i ograničenje istorije: od njega zavisi da li možeš da pogledaš prošle procene i brzinu.</p>
<p>Nazivi drugih proizvoda su žigovi njihovih vlasnika i koriste se samo da bi se ti proizvodi opisali.</p>
""",
    },
    "de": {
        "title": "Planning-Poker-Tools im Vergleich",
        "nav": "Tools im Vergleich",
        "description": "Ein fairer Vergleich von Planning-Poker-Tools: Grenzen des kostenlosen Plans, Preise, Open Source, Self-Hosting, Issue-Tracker, asynchrones Abstimmen, Backlog-Sortierung und Retros, mit Quellen und Prüfdatum.",
        "lead": "Jedes Team braucht etwas anderes. Die Tabelle nennt Fakten von der eigenen Website jedes Produkts, damit du das passende Tool wählen kannst; wo eine Website nichts sagt, steht das auch so da.",
        "body": "<h2>Die Tabelle</h2>\n" + compare_table("de") + """
<h2>So liest du sie</h2>
<p>Ein kostenloser Plan mit Grenzen bei Teilnehmenden oder Meetings reicht für ein kleines Team, das ab und zu schätzt. Für ein Team, das jeden Sprint schätzt, zählt auch die Grenze beim Verlauf: Sie entscheidet, ob ihr frühere Schätzungen und die Velocity noch seht.</p>
<p>Namen anderer Produkte sind Marken ihrer Inhaber und werden nur zu ihrer Beschreibung verwendet.</p>
""",
    },
}

# ------------------------------------------------------------------------------------------------ tools and related pages

PAGES["velocity-calculator"]["tool"] = velocity_tool
PAGES["velocity-calculator"]["script"] = VELOCITY_SCRIPT
PAGES["story-point-scales"]["tool"] = lambda lang: scales_tool(lang, APP)
PAGES["story-point-scales"]["script"] = SCALES_SCRIPT
PAGES["velocity-calculator"]["cta_href"] = f"{APP}/team"

RELATED = {
    "velocity-calculator": ["sprint-planning-tool", "story-point-scales", "planning-poker-for-jira"],
    "story-point-scales": ["velocity-calculator", "scrum-estimation-for-beginners", "magic-estimation"],
    "planning-poker-for-jira": ["sprint-planning-tool", "magic-estimation", "planning-poker-for-github"],
    "planning-poker-for-github": ["planning-poker-for-linear", "magic-estimation", "remote-planning-poker"],
    "planning-poker-for-linear": ["planning-poker-for-github", "magic-estimation", "remote-planning-poker"],
    "planning-poker-for-azure-devops": ["planning-poker-for-jira", "magic-estimation", "sprint-planning-tool"],
    "remote-planning-poker": ["scrum-estimation-for-beginners", "planning-poker-tools-compared", "planning-poker-for-jira"],
    "scrum-estimation-for-beginners": ["story-point-scales", "remote-planning-poker", "velocity-calculator"],
    "magic-estimation": ["sprint-planning-tool", "planning-poker-for-jira", "story-point-scales"],
    "sprint-planning-tool": ["velocity-calculator", "magic-estimation", "planning-poker-for-jira"],
    "planning-poker-tools-compared": ["remote-planning-poker", "sprint-planning-tool", "velocity-calculator"],
}


def page_for(slug, lang):
    page = dict(PAGES[slug][lang])
    meta = PAGES[slug]
    if "tool" in meta:
        page["tool"] = meta["tool"](lang)
        page["script"] = meta["script"]
    if "cta_href" in meta:
        page["cta_href"] = meta["cta_href"]
    page["wide"] = meta.get("wide", False)
    return page


for slug in PAGES:
    for lang in ("en", "sr", "de"):
        related = [(other, PAGES[other][lang]["nav"]) for other in RELATED[slug]]
        out = render(slug, lang, page_for(slug, lang), related, DATE, PAGES[slug]["kind"])
        target = ROOT / "pages" / f"{slug}.html" if lang == "en" else ROOT / "i18n" / lang / f"{slug}.html"
        target.write_text(out, encoding="utf-8", newline="\n")
    print("wrote", slug)
