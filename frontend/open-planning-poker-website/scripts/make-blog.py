"""Writes the blog (OPP-176): an index and one article per feature that had no page of its own, in English, Serbian and
German. Run from the website folder: python scripts/make-blog.py

Articles are pages/blog/<slug>.html and i18n/<lang>/blog/<slug>.html; build.js serves them at /blog/<slug>/.
"""
import html
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from landing_render import picture, prefix, render  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parent.parent
DATE = "2026-11-22"
APP = "https://app.openplanningpoker.com"


def code(text):
    return f'<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>{html.escape(text.strip())}</code></pre>'


CURL = """curl -H "Authorization: Bearer opp_…" https://ge.openplanningpoker.com/api/v1/sessions?take=5"""
NODE_SERVER = """import http from 'node:http';
import { verify } from './verify.js'; // the check from the API docs

http.createServer((req, res) => {
  let body = '';
  req.on('data', (chunk) => (body += chunk));
  req.on('end', () => {
    if (!verify(process.env.OPP_WEBHOOK_SECRET, req.headers['x-opp-signature'] ?? '', body)) {
      res.writeHead(401).end();
      return;
    }
    const event = JSON.parse(body);
    if (event.type === 'ticket.estimated') {
      console.log(`${event.data.ticket.externalKey ?? event.data.ticket.name}: ${event.data.ticket.estimate}`);
    }
    res.writeHead(204).end();
  });
}).listen(3000);"""

POSTS = {}

POSTS["public-api-build-your-own-apps"] = {
    "en": {
        "title": "Build your own apps on the Open Planning Poker API",
        "nav": "Build on the public API",
        "description": "The public API and webhooks of Open Planning Poker are free: build a dashboard, connect a tool we do not support, or practise integrations against a real API with real data.",
        "lead": "Every team room now has a small read-only API and signed webhooks. They are free, documented, and real, which makes them a good place to build your own tools or to practise integration work.",
        "body": f"""
<h2>What you can build</h2>
<ul>
  <li><strong>Your own velocity dashboard</strong> in a spreadsheet, Grafana or a static page, from <code>/api/v1/sessions</code>.</li>
  <li><strong>A bridge to a tracker we do not support yet</strong>: listen for <code>ticket.estimated</code> and write the estimate wherever your team keeps work.</li>
  <li><strong>Chat notifications</strong> in Discord, Mattermost or Matrix when a session finishes.</li>
  <li><strong>A teaching project</strong>: an API with tokens, paging, rate limits and signed webhooks is a realistic exercise for students and juniors.</li>
</ul>

<h2>Start in five minutes</h2>
<ol>
  <li>Open a team room and, as its owner, create a token under <strong>API and webhooks</strong>.</li>
  <li>Call the API:</li>
</ol>
{code(CURL)}
<p>You get the team's sessions with points, first-vote agreement and re-votes, paged with <code>skip</code> and <code>take</code>. Tickets of a session come from <code>/api/v1/sessions/{{id}}/tickets</code> with their agreed estimates and tracker links. The API never returns single votes or player names.</p>

<h2>Practise webhooks the right way</h2>
<p>Add a webhook address and you receive signed JSON for each estimate. A small receiver in Node shows the whole pattern: read the raw body, check the signature, then handle the event.</p>
{code(NODE_SERVER)}
<p>Things worth practising on it: rejecting requests with a wrong or old signature, dropping duplicates with <code>X-OPP-Delivery</code>, answering fast and doing slow work later, and watching retries in the deliveries list when your receiver returns an error.</p>

<h2>Limits and fair use</h2>
<p>60 requests a minute per token, plus the usual limit per address. Above it you get <code>429</code> with <code>Retry-After</code>. Webhooks go only to public https addresses.</p>
<p>Everything else is in the <a class="link" href="/public-api/">API documentation</a>: endpoints, events, signature checks in Node and Python, and n8n and Zapier steps.</p>
""",
        "cta": "Open a team room", "cta_href": f"{APP}/team",
    },
    "sr": {
        "title": "Pravi svoje aplikacije na Open Planning Poker API-ju",
        "nav": "Pravi na javnom API-ju",
        "description": "Javni API i webhookovi Open Planning Pokera su besplatni: napravi dashboard, poveži alat koji još ne podržavamo ili vežbaj integracije na pravom API-ju sa pravim podacima.",
        "lead": "Svaka timska soba sada ima mali API samo za čitanje i potpisane webhookove. Besplatni su, dokumentovani i pravi, pa su dobro mesto da napraviš svoje alate ili vežbaš integracije.",
        "body": f"""
<h2>Šta možeš da napraviš</h2>
<ul>
  <li><strong>Sopstveni dashboard brzine</strong> u tabeli, Grafani ili na statičnoj stranici, iz <code>/api/v1/sessions</code>.</li>
  <li><strong>Most ka alatu koji još ne podržavamo</strong>: osluškuj <code>ticket.estimated</code> i upiši procenu tamo gde tim drži posao.</li>
  <li><strong>Obaveštenja u chatu</strong> na Discordu, Mattermostu ili Matrixu kad se sesija završi.</li>
  <li><strong>Projekat za učenje</strong>: API sa tokenima, straničenjem, ograničenjima i potpisanim webhookovima je realna vežba za studente i juniore.</li>
</ul>

<h2>Kreni za pet minuta</h2>
<ol>
  <li>Otvori timsku sobu i, kao vlasnik, napravi token pod <strong>API i webhookovi</strong>.</li>
  <li>Pozovi API:</li>
</ol>
{code(CURL)}
<p>Dobijaš sesije tima sa poenima, slaganjem u prvom glasanju i ponovnim glasanjima, straničeno sa <code>skip</code> i <code>take</code>. Tiketi sesije stižu sa <code>/api/v1/sessions/{{id}}/tickets</code>, sa dogovorenim procenama i linkovima ka alatu. API nikad ne vraća pojedinačne glasove ni imena igrača.</p>

<h2>Vežbaj webhookove kako treba</h2>
<p>Dodaj adresu webhooka i za svaku procenu dobijaš potpisan JSON. Mali prijemnik u Nodu pokazuje ceo obrazac: pročitaj sirovo telo, proveri potpis, pa obradi događaj.</p>
{code(NODE_SERVER)}
<p>Vredi vežbati: odbijanje zahteva sa pogrešnim ili starim potpisom, odbacivanje duplikata pomoću <code>X-OPP-Delivery</code>, brz odgovor uz kasniji spori posao, i praćenje ponovnih pokušaja u listi isporuka kad prijemnik vrati grešku.</p>

<h2>Ograničenja i fer korišćenje</h2>
<p>60 zahteva u minuti po tokenu, uz uobičajeno ograničenje po adresi. Iznad toga dobijaš <code>429</code> sa <code>Retry-After</code>. Webhookovi idu samo na javne https adrese.</p>
<p>Sve ostalo je u <a class="link" href="/public-api/">dokumentaciji API-ja</a>: endpointi, događaji, provera potpisa u Nodu i Pythonu, i koraci za n8n i Zapier.</p>
""",
        "cta": "Otvori timsku sobu", "cta_href": f"{APP}/team",
    },
    "de": {
        "title": "Eigene Apps auf der Open-Planning-Poker-API bauen",
        "nav": "Auf der öffentlichen API bauen",
        "description": "Die öffentliche API und die Webhooks von Open Planning Poker sind kostenlos: Bau ein Dashboard, binde ein Tool an, das wir noch nicht unterstützen, oder übe Integrationen an einer echten API mit echten Daten.",
        "lead": "Jeder Team-Raum hat jetzt eine kleine API zum Lesen und signierte Webhooks. Sie sind kostenlos, dokumentiert und echt und damit ein guter Ort, eigene Tools zu bauen oder Integrationen zu üben.",
        "body": f"""
<h2>Was du bauen kannst</h2>
<ul>
  <li><strong>Ein eigenes Velocity-Dashboard</strong> in einer Tabelle, in Grafana oder auf einer statischen Seite, aus <code>/api/v1/sessions</code>.</li>
  <li><strong>Eine Brücke zu einem Tracker, den wir noch nicht unterstützen</strong>: Hör auf <code>ticket.estimated</code> und schreib die Schätzung dorthin, wo dein Team die Arbeit führt.</li>
  <li><strong>Chat-Benachrichtigungen</strong> in Discord, Mattermost oder Matrix, wenn eine Sitzung endet.</li>
  <li><strong>Ein Lernprojekt</strong>: Eine API mit Tokens, Seiten, Limits und signierten Webhooks ist eine realistische Übung für Studierende und Junioren.</li>
</ul>

<h2>In fünf Minuten loslegen</h2>
<ol>
  <li>Öffne einen Team-Raum und erstelle als Inhaber unter <strong>API und Webhooks</strong> ein Token.</li>
  <li>Ruf die API auf:</li>
</ol>
{code(CURL)}
<p>Du bekommst die Sitzungen des Teams mit Punkten, Einigkeit beim ersten Abstimmen und Neuabstimmungen, seitenweise mit <code>skip</code> und <code>take</code>. Die Tickets einer Sitzung liefert <code>/api/v1/sessions/{{id}}/tickets</code> mit vereinbarten Schätzungen und Tracker-Links. Die API liefert nie einzelne Stimmen oder Spielernamen.</p>

<h2>Webhooks richtig üben</h2>
<p>Füg eine Webhook-Adresse hinzu und du bekommst für jede Schätzung signiertes JSON. Ein kleiner Empfänger in Node zeigt das ganze Muster: den rohen Body lesen, die Signatur prüfen, dann das Ereignis verarbeiten.</p>
{code(NODE_SERVER)}
<p>Was sich daran zu üben lohnt: Anfragen mit falscher oder alter Signatur ablehnen, Duplikate mit <code>X-OPP-Delivery</code> verwerfen, schnell antworten und langsame Arbeit später erledigen, und in der Liste der Zustellungen die Wiederholungen beobachten, wenn dein Empfänger einen Fehler liefert.</p>

<h2>Grenzen und faire Nutzung</h2>
<p>60 Anfragen pro Minute und Token, dazu die übliche Grenze pro Adresse. Darüber kommt <code>429</code> mit <code>Retry-After</code>. Webhooks gehen nur an öffentliche https-Adressen.</p>
<p>Alles Weitere steht in der <a class="link" href="/public-api/">API-Dokumentation</a>: Endpunkte, Ereignisse, Signaturprüfung in Node und Python sowie Schritte für n8n und Zapier.</p>
""",
        "cta": "Team-Raum öffnen", "cta_href": f"{APP}/team",
    },
}

POSTS["sprint-retros-in-the-team-room"] = {
    "en": {
        "title": "Sprint retros in the team room, starting from the numbers",
        "nav": "Sprint retros",
        "description": "Run the sprint retro where you planned the sprint: write in silence, group, vote with dots and carry action items over, with the sprint's estimation numbers on the board.",
        "lead": "Most teams plan in one tool and hold the retro in another. Open Planning Poker now runs both in the same team room, so the retro starts from what actually happened in the sprint.",
        "body": f"""
<h2>Four steps</h2>
<ol>
  <li><strong>Write</strong>: everyone writes cards in silence and sees only their own; the others show as a count per column.</li>
  <li><strong>Group</strong>: the host reveals all cards; similar ones are dragged onto each other and get a title.</li>
  <li><strong>Vote</strong>: each person places three dots (the host can change the number); nobody sees the others' dots.</li>
  <li><strong>Discuss</strong>: the cards sort by dots, and the team agrees on action items with an owner.</li>
</ol>
{picture('room', 'A team room with the sprint and its numbers')}
<h2>Start from data, not from memory</h2>
<p>The <strong>Sprint in numbers</strong> panel shows the estimated and planned points, the velocity of the last sprints, tickets that took much longer or shorter than their card, and tickets the team was unsure about. One click turns any of them into a card.</p>

<h2>Action items that come back</h2>
<p>Open action items show in the team room until they are done, and the next retro opens with them. That is the part of a retro that most often gets lost.</p>
<p>The retro uses the sprint's room, so everyone joins with the same link, guests too, and the room timer works for the writing step. Templates: Went well / To improve / Ideas, Start / Stop / Continue, Mad / Sad / Glad and 4Ls.</p>
""",
        "cta": "Open a team room", "cta_href": f"{APP}/team",
    },
    "sr": {
        "title": "Retrospektive sprinta u timskoj sobi, polazeći od brojki",
        "nav": "Retrospektive sprinta",
        "description": "Vodi retrospektivu tamo gde si planirao sprint: piši u tišini, grupiši, glasaj tačkicama i prenesi akcije dalje, uz brojke procena sprinta na tabli.",
        "lead": "Većina timova planira u jednom alatu, a retrospektivu drži u drugom. Open Planning Poker sada vodi oboje u istoj timskoj sobi, pa retrospektiva polazi od onoga što se stvarno desilo u sprintu.",
        "body": f"""
<h2>Četiri koraka</h2>
<ol>
  <li><strong>Pisanje</strong>: svi pišu kartice u tišini i vide samo svoje; tuđe se vide kao broj po koloni.</li>
  <li><strong>Grupisanje</strong>: host otkriva sve kartice; slične se prevlače jedna na drugu i dobijaju naslov.</li>
  <li><strong>Glasanje</strong>: svako raspoređuje tri tačkice (host može da promeni broj); niko ne vidi tuđe tačkice.</li>
  <li><strong>Diskusija</strong>: kartice se ređaju po tačkicama, a tim dogovara akcije sa zaduženom osobom.</li>
</ol>
{picture('room', 'Timska soba sa sprintom i njegovim brojkama')}
<h2>Kreni od podataka, ne od sećanja</h2>
<p>Panel <strong>Sprint u brojkama</strong> prikazuje procenjene i planirane poene, brzinu poslednjih sprintova, tikete koji su trajali mnogo duže ili kraće od svoje karte i tikete u koje tim nije bio siguran. Jednim klikom svaki postaje kartica.</p>

<h2>Akcije koje se vraćaju</h2>
<p>Otvorene akcije stoje u timskoj sobi dok se ne završe, a sledeća retrospektiva počinje sa njima. To je deo retrospektive koji se najčešće izgubi.</p>
<p>Retrospektiva koristi sobu sprinta, pa svi ulaze istim linkom, i gosti, a tajmer sobe radi i za pisanje. Šabloni: Dobro / Za poboljšanje / Ideje, Počni / Prestani / Nastavi, Ljut / Tužan / Srećan i 4L.</p>
""",
        "cta": "Otvori timsku sobu", "cta_href": f"{APP}/team",
    },
    "de": {
        "title": "Sprint-Retros im Team-Raum, ausgehend von den Zahlen",
        "nav": "Sprint-Retros",
        "description": "Führ die Sprint-Retro dort, wo du den Sprint geplant hast: still schreiben, gruppieren, mit Punkten abstimmen und Maßnahmen mitnehmen, mit den Schätzzahlen des Sprints auf dem Board.",
        "lead": "Die meisten Teams planen in einem Tool und halten die Retro in einem anderen. Open Planning Poker führt jetzt beides im selben Team-Raum, damit die Retro bei dem beginnt, was im Sprint wirklich passiert ist.",
        "body": f"""
<h2>Vier Schritte</h2>
<ol>
  <li><strong>Schreiben</strong>: Alle schreiben still Karten und sehen nur ihre eigenen; die anderen erscheinen als Anzahl pro Spalte.</li>
  <li><strong>Gruppieren</strong>: Der Host deckt alle Karten auf; ähnliche werden aufeinandergezogen und bekommen einen Titel.</li>
  <li><strong>Abstimmen</strong>: Jede Person verteilt drei Punkte (der Host kann die Zahl ändern); niemand sieht die Punkte der anderen.</li>
  <li><strong>Besprechen</strong>: Die Karten sortieren sich nach Punkten, und das Team vereinbart Maßnahmen mit Verantwortlichen.</li>
</ol>
{picture('room', 'Ein Team-Raum mit dem Sprint und seinen Zahlen')}
<h2>Von Daten ausgehen, nicht vom Gedächtnis</h2>
<p>Das Feld <strong>Sprint in Zahlen</strong> zeigt geschätzte und geplante Punkte, die Velocity der letzten Sprints, Tickets, die viel länger oder kürzer als ihre Karte dauerten, und Tickets, bei denen das Team unsicher war. Ein Klick macht aus jedem eine Karte.</p>

<h2>Maßnahmen, die wiederkommen</h2>
<p>Offene Maßnahmen stehen im Team-Raum, bis sie erledigt sind, und die nächste Retro beginnt mit ihnen. Genau dieser Teil einer Retro geht sonst am häufigsten verloren.</p>
<p>Die Retro nutzt den Raum des Sprints, also kommen alle über denselben Link hinein, auch Gäste, und der Timer des Raums funktioniert beim Schreiben. Vorlagen: Lief gut / Verbessern / Ideen, Beginnen / Aufhören / Weitermachen, Wütend / Traurig / Froh und 4L.</p>
""",
        "cta": "Team-Raum öffnen", "cta_href": f"{APP}/team",
    },
}

POSTS["share-a-session-recap"] = {
    "en": {
        "title": "Share how a planning session went, in one link",
        "nav": "Shareable recaps",
        "description": "A read-only recap link for stakeholders: tickets, final estimates, total points, consensus and discussion time, with a preview card in Slack, Teams and email.",
        "lead": "After planning, someone always asks what was decided. A recap link answers it without inviting them into the room or copying numbers into a message.",
        "body": f"""
<h2>What the recap shows</h2>
<ul>
  <li>The tickets with their final estimates and links to the tracker.</li>
  <li>Total points, how many tickets reached consensus, and how long the discussion took.</li>
  <li>Player names only when the host chooses to show them.</li>
</ul>
<p>It never shows ticket descriptions, notes or single votes. When the host stops sharing, the link stops working.</p>
{picture('voting', 'Voting with hidden cards before the reveal')}
<h2>A preview where people already are</h2>
<p>Pasted into Slack, Microsoft Teams or an email, the link shows a card with the room name and three numbers. Invite links to a game show "Join … — planning poker" in the visitor's language.</p>
<p>Turn it on from the room menu: <strong>Share the recap</strong>.</p>
""",
    },
    "sr": {
        "title": "Podeli kako je prošla sesija planiranja, jednim linkom",
        "nav": "Rezime za deljenje",
        "description": "Link do rezimea samo za čitanje, za zainteresovane: tiketi, konačne procene, ukupno poena, konsenzus i vreme diskusije, uz karticu za pregled u Slacku, Teams-u i mailu.",
        "lead": "Posle planiranja neko uvek pita šta je dogovoreno. Link do rezimea odgovara na to, bez pozivanja u sobu i bez prepisivanja brojki u poruku.",
        "body": f"""
<h2>Šta rezime prikazuje</h2>
<ul>
  <li>Tikete sa konačnim procenama i linkovima ka alatu za tikete.</li>
  <li>Ukupno poena, koliko je tiketa dostiglo konsenzus i koliko je trajala diskusija.</li>
  <li>Imena igrača samo ako host odluči da ih prikaže.</li>
</ul>
<p>Nikad ne prikazuje opise tiketa, beleške ni pojedinačne glasove. Kad host prestane da deli, link prestaje da radi.</p>
{picture('voting', 'Glasanje skrivenim kartama pre otkrivanja')}
<h2>Pregled tamo gde su ljudi već</h2>
<p>Nalepljen u Slack, Microsoft Teams ili mail, link prikazuje karticu sa imenom sobe i tri broja. Linkovi poziva u igru prikazuju „Pridruži se … — planning poker” na jeziku posetioca.</p>
<p>Uključi ga iz menija sobe: <strong>Share the recap</strong>.</p>
""",
    },
    "de": {
        "title": "Teilen, wie eine Planungssitzung lief, mit einem Link",
        "nav": "Rückblick teilen",
        "description": "Ein schreibgeschützter Rückblick-Link für Stakeholder: Tickets, endgültige Schätzungen, Punkte gesamt, Konsens und Diskussionszeit, mit Vorschaukarte in Slack, Teams und E-Mail.",
        "lead": "Nach der Planung fragt immer jemand, was entschieden wurde. Ein Rückblick-Link beantwortet das, ohne ihn in den Raum einzuladen oder Zahlen in eine Nachricht zu kopieren.",
        "body": f"""
<h2>Was der Rückblick zeigt</h2>
<ul>
  <li>Die Tickets mit ihren endgültigen Schätzungen und Links zum Tracker.</li>
  <li>Die Punkte gesamt, wie viele Tickets Konsens erreichten und wie lange die Diskussion dauerte.</li>
  <li>Spielernamen nur, wenn der Host sie zeigen will.</li>
</ul>
<p>Er zeigt nie Ticketbeschreibungen, Notizen oder einzelne Stimmen. Beendet der Host das Teilen, funktioniert der Link nicht mehr.</p>
{picture('voting', 'Abstimmen mit verdeckten Karten vor dem Aufdecken')}
<h2>Eine Vorschau dort, wo alle schon sind</h2>
<p>In Slack, Microsoft Teams oder eine E-Mail eingefügt, zeigt der Link eine Karte mit dem Raumnamen und drei Zahlen. Einladungslinks zu einem Spiel zeigen „… beitreten — Planning Poker“ in der Sprache des Besuchers.</p>
<p>Einschalten im Raum-Menü: <strong>Share the recap</strong>.</p>
""",
    },
}

POSTS["ai-help-that-never-picks-a-card"] = {
    "en": {
        "title": "AI help in planning poker that never picks a card",
        "nav": "AI help, not AI estimates",
        "description": "An optional assistant checks a ticket for gaps before voting, suggests how to split a big one, and summarizes the discussion in one sentence. It never suggests an estimate.",
        "lead": "An estimate is a team's shared understanding, so a model should not make it. What a model can do well is ask the questions the team forgot and write the note nobody wants to write.",
        "body": """
<h2>Three small jobs</h2>
<ul>
  <li><strong>Check the ticket</strong> before voting: up to three short points such as missing acceptance criteria or open questions. Everyone in the room sees them.</li>
  <li><strong>Ideas to split</strong> after a reveal with the top card or many "?" votes: two to four smaller tickets, each can be added to the game with one click.</li>
  <li><strong>Summarize the discussion</strong> after the final estimate: one sentence from the voters' notes and the chat since the reveal. The host edits it, and it goes into the Jira comment and the PDF report.</li>
</ul>

<h2>The rules it follows</h2>
<ul>
  <li>It never suggests a card, a number of points or hours.</li>
  <li>Only the ticket title and description, the notes and the chat lines are sent; player names are replaced with "a player" first.</li>
  <li>It is off unless the server operator adds an API key, and then off in every room until the host turns it on.</li>
</ul>
<p>Self-hosting? Set <code>Ai__ApiKey</code> on the game engine to turn it on for your server.</p>
""",
    },
    "sr": {
        "title": "AI pomoć u planning pokeru koja nikad ne bira kartu",
        "nav": "AI pomoć, ne AI procene",
        "description": "Opcioni asistent proverava tiket pre glasanja, predlaže kako da se podeli veliki tiket i sažima diskusiju u jednu rečenicu. Nikad ne predlaže procenu.",
        "lead": "Procena je zajedničko razumevanje tima, pa je model ne treba da pravi. Ono što model dobro radi je da postavi pitanja koja je tim zaboravio i da napiše belešku koju niko ne želi da piše.",
        "body": """
<h2>Tri mala posla</h2>
<ul>
  <li><strong>Proveri tiket</strong> pre glasanja: najviše tri kratke tačke, na primer kriterijumi prihvatanja koji fale ili otvorena pitanja. Svi u sobi ih vide.</li>
  <li><strong>Ideje za podelu</strong> posle otkrivanja sa najvišom kartom ili mnogo „?” glasova: dva do četiri manja tiketa, svaki se dodaje u igru jednim klikom.</li>
  <li><strong>Sažmi diskusiju</strong> posle konačne procene: jedna rečenica iz beleški uz glasove i chata od otkrivanja. Host je menja, a ona ide u Jira komentar i PDF izveštaj.</li>
</ul>

<h2>Pravila kojih se drži</h2>
<ul>
  <li>Nikad ne predlaže kartu, broj poena ni sate.</li>
  <li>Šalju se samo naslov i opis tiketa, beleške i redovi iz chata; imena igrača se prvo zamene sa „a player”.</li>
  <li>Isključen je dok operater servera ne doda API ključ, a onda je isključen u svakoj sobi dok ga host ne uključi.</li>
</ul>
<p>Sam hostuješ? Postavi <code>Ai__ApiKey</code> na game engine-u da ga uključiš za svoj server.</p>
""",
    },
    "de": {
        "title": "KI-Hilfe beim Planning Poker, die nie eine Karte wählt",
        "nav": "KI-Hilfe, keine KI-Schätzung",
        "description": "Ein optionaler Assistent prüft ein Ticket vor dem Abstimmen auf Lücken, schlägt vor, wie man ein großes aufteilt, und fasst die Diskussion in einem Satz zusammen. Eine Schätzung schlägt er nie vor.",
        "lead": "Eine Schätzung ist das gemeinsame Verständnis eines Teams, also sollte kein Modell sie machen. Was ein Modell gut kann: die Fragen stellen, die das Team vergessen hat, und die Notiz schreiben, die niemand schreiben will.",
        "body": """
<h2>Drei kleine Aufgaben</h2>
<ul>
  <li><strong>Ticket prüfen</strong> vor dem Abstimmen: bis zu drei kurze Punkte wie fehlende Akzeptanzkriterien oder offene Fragen. Alle im Raum sehen sie.</li>
  <li><strong>Ideen zum Aufteilen</strong> nach einem Aufdecken mit der höchsten Karte oder vielen „?“: zwei bis vier kleinere Tickets, jedes mit einem Klick ins Spiel übernommen.</li>
  <li><strong>Diskussion zusammenfassen</strong> nach der endgültigen Schätzung: ein Satz aus den Notizen zu den Stimmen und dem Chat seit dem Aufdecken. Der Host bearbeitet ihn, und er kommt in den Jira-Kommentar und den PDF-Bericht.</li>
</ul>

<h2>Die Regeln, an die er sich hält</h2>
<ul>
  <li>Er schlägt nie eine Karte, Punkte oder Stunden vor.</li>
  <li>Gesendet werden nur Titel und Beschreibung des Tickets, die Notizen und die Chat-Zeilen; Spielernamen werden vorher durch „a player“ ersetzt.</li>
  <li>Er ist aus, bis der Betreiber des Servers einen API-Schlüssel hinterlegt, und dann in jedem Raum aus, bis der Host ihn einschaltet.</li>
</ul>
<p>Selbst gehostet? Setz <code>Ai__ApiKey</code> in der Game Engine, um ihn für deinen Server einzuschalten.</p>
""",
    },
}

POSTS["organizations-teams-side-by-side"] = {
    "en": {
        "title": "Organizations: every team room of a company in one place",
        "nav": "Organizations",
        "description": "Put the team rooms of a company under one organization: invite with a link, roles admin and member, and every team's first-vote agreement, re-votes and velocity side by side.",
        "lead": "When several teams estimate with Open Planning Poker, someone wants to see them together. Organizations do that without taking the rooms away from their teams.",
        "body": """
<h2>How it works</h2>
<ol>
  <li>Create an organization from the app menu and share its invite link.</li>
  <li>Team owners move their own rooms in. They stay the owners; nothing else about the room changes.</li>
  <li>Admins see the organization's teams side by side and can download the table as CSV.</li>
</ol>

<h2>Numbers that help, not a ranking</h2>
<p>The comparison shows first-vote agreement, re-votes per ticket and the velocity trend of each team over its last six sessions. Team numbers only, never per player.</p>
<p>Velocity is not comparable across teams, because every team has its own scale. The table is for spotting a team that suddenly re-votes a lot or whose velocity drops, so someone can ask what changed.</p>

<h2>Roles</h2>
<ul>
  <li><strong>Admin</strong>: renames the organization, makes a new invite link, changes roles, removes members and teams, and sees the numbers.</li>
  <li><strong>Member</strong>: sees the organization's teams and can move their own team in or out.</li>
</ul>
""",
        "cta": "Create an organization", "cta_href": f"{APP}/org",
    },
    "sr": {
        "title": "Organizacije: sve timske sobe jedne kompanije na jednom mestu",
        "nav": "Organizacije",
        "description": "Okupi timske sobe kompanije u jednu organizaciju: poziv preko linka, uloge administrator i član, i slaganje u prvom glasanju, ponovna glasanja i brzina svih timova jedno pored drugog.",
        "lead": "Kad više timova procenjuje u Open Planning Pokeru, neko želi da ih vidi zajedno. Organizacije to rade bez oduzimanja soba timovima.",
        "body": """
<h2>Kako radi</h2>
<ol>
  <li>Napravi organizaciju iz menija aplikacije i podeli njen link za poziv.</li>
  <li>Vlasnici timova ubacuju svoje sobe. Ostaju vlasnici; ništa drugo se u sobi ne menja.</li>
  <li>Administratori vide timove organizacije jedan pored drugog i mogu da preuzmu tabelu kao CSV.</li>
</ol>

<h2>Brojke koje pomažu, ne rang lista</h2>
<p>Poređenje prikazuje slaganje u prvom glasanju, ponovna glasanja po tiketu i trend brzine svakog tima kroz poslednjih šest sesija. Samo brojke tima, nikad po igraču.</p>
<p>Brzina nije uporediva između timova, jer svaki tim ima svoju skalu. Tabela služi da se primeti tim koji odjednom mnogo ponovo glasa ili kome brzina pada, pa neko može da pita šta se promenilo.</p>

<h2>Uloge</h2>
<ul>
  <li><strong>Administrator</strong>: preimenuje organizaciju, pravi novi link za poziv, menja uloge, uklanja članove i timove i vidi brojke.</li>
  <li><strong>Član</strong>: vidi timove organizacije i može da ubaci ili izvadi svoj tim.</li>
</ul>
""",
        "cta": "Napravi organizaciju", "cta_href": f"{APP}/org",
    },
    "de": {
        "title": "Organisationen: alle Team-Räume eines Unternehmens an einem Ort",
        "nav": "Organisationen",
        "description": "Fass die Team-Räume eines Unternehmens in einer Organisation zusammen: Einladung per Link, Rollen Admin und Mitglied, und die Einigkeit beim ersten Abstimmen, Neuabstimmungen und Velocity aller Teams nebeneinander.",
        "lead": "Wenn mehrere Teams mit Open Planning Poker schätzen, will jemand sie zusammen sehen. Organisationen machen das, ohne den Teams ihre Räume wegzunehmen.",
        "body": """
<h2>So funktioniert es</h2>
<ol>
  <li>Erstelle eine Organisation im App-Menü und teile ihren Einladungslink.</li>
  <li>Team-Inhaber fügen ihre eigenen Räume hinzu. Sie bleiben Inhaber; sonst ändert sich am Raum nichts.</li>
  <li>Admins sehen die Teams der Organisation nebeneinander und können die Tabelle als CSV herunterladen.</li>
</ol>

<h2>Zahlen, die helfen, keine Rangliste</h2>
<p>Der Vergleich zeigt die Einigkeit beim ersten Abstimmen, Neuabstimmungen pro Ticket und den Velocity-Verlauf jedes Teams über seine letzten sechs Sitzungen. Nur Team-Zahlen, nie pro Person.</p>
<p>Velocity ist zwischen Teams nicht vergleichbar, weil jedes Team seine eigene Skala hat. Die Tabelle dient dazu, ein Team zu bemerken, das plötzlich oft neu abstimmt oder dessen Velocity sinkt, damit jemand fragen kann, was sich geändert hat.</p>

<h2>Rollen</h2>
<ul>
  <li><strong>Admin</strong>: benennt die Organisation um, erstellt einen neuen Einladungslink, ändert Rollen, entfernt Mitglieder und Teams und sieht die Zahlen.</li>
  <li><strong>Mitglied</strong>: sieht die Teams der Organisation und kann das eigene Team hinzufügen oder herausnehmen.</li>
</ul>
""",
        "cta": "Organisation erstellen", "cta_href": f"{APP}/org",
    },
}

POSTS["run-a-better-discussion"] = {
    "en": {
        "title": "Run a better discussion after the reveal",
        "nav": "A better discussion",
        "description": "Notes from the lowest and highest voters, a discussion timer, a confidence check, reactions and a needs-information flag: small tools that keep the talk after a split vote short and useful.",
        "lead": "The reveal is where planning poker pays off or wastes time. When the votes are far apart, the next five minutes decide whether the team learns something or just argues.",
        "body": """
<h2>Let the outliers speak first</h2>
<p>With <strong>outlier notes</strong> on, the lowest and highest voters can add a one-line reason with their vote. After the reveal everyone sees why someone voted 3 and someone 13, before anybody talks.</p>

<h2>Keep it short</h2>
<p>A <strong>discussion timer</strong> of two, three or five minutes starts after the reveal. When it ends, the team votes again or the host picks the estimate.</p>

<h2>Know how sure the team is</h2>
<p>A <strong>confidence check</strong> asks everyone, after the estimate, how sure they are from 1 to 5. Low confidence is worth a note in the ticket, and it shows up in the sprint retro.</p>

<h2>Park what cannot be estimated</h2>
<p><strong>Needs more information</strong> marks a ticket that should go back to its author instead of being guessed. <strong>Reactions</strong> let people agree without interrupting.</p>
<p>All of it is in the room menu under the discussion options, and each part can be turned off.</p>
""",
    },
    "sr": {
        "title": "Vodi bolju diskusiju posle otkrivanja karata",
        "nav": "Bolja diskusija",
        "description": "Beleške najnižeg i najvišeg glasa, tajmer diskusije, provera sigurnosti, reakcije i oznaka „treba više informacija”: mali alati koji razgovor posle podeljenog glasanja drže kratkim i korisnim.",
        "lead": "Otkrivanje karata je trenutak kada se planning poker isplati ili gubi vreme. Kad su glasovi daleko, narednih pet minuta odlučuje da li će tim nešto naučiti ili samo raspravljati.",
        "body": """
<h2>Neka prvo govore krajnji glasovi</h2>
<p>Uz uključene <strong>beleške krajnjih glasova</strong>, najniži i najviši glas mogu uz glas da dodaju razlog u jednom redu. Posle otkrivanja svi vide zašto je neko glasao 3, a neko 13, pre nego što iko progovori.</p>

<h2>Neka bude kratko</h2>
<p><strong>Tajmer diskusije</strong> od dva, tri ili pet minuta kreće posle otkrivanja. Kad istekne, tim glasa ponovo ili host bira procenu.</p>

<h2>Znaj koliko je tim siguran</h2>
<p><strong>Provera sigurnosti</strong> posle procene pita svakoga koliko je siguran, od 1 do 5. Niska sigurnost zaslužuje belešku u tiketu i pojavljuje se u retrospektivi sprinta.</p>

<h2>Skloni ono što ne može da se proceni</h2>
<p><strong>Treba više informacija</strong> označava tiket koji treba vratiti autoru umesto da se nagađa. <strong>Reakcije</strong> omogućavaju da se ljudi slože bez prekidanja.</p>
<p>Sve je u meniju sobe, pod opcijama diskusije, i svaki deo može da se isključi.</p>
""",
    },
    "de": {
        "title": "Eine bessere Diskussion nach dem Aufdecken führen",
        "nav": "Eine bessere Diskussion",
        "description": "Notizen der niedrigsten und höchsten Stimme, ein Diskussions-Timer, eine Sicherheitsabfrage, Reaktionen und eine Markierung „braucht mehr Informationen“: kleine Werkzeuge, die das Gespräch nach einer geteilten Abstimmung kurz und nützlich halten.",
        "lead": "Beim Aufdecken lohnt sich Planning Poker oder kostet Zeit. Liegen die Stimmen weit auseinander, entscheiden die nächsten fünf Minuten, ob das Team etwas lernt oder nur streitet.",
        "body": """
<h2>Zuerst sprechen die Ausreißer</h2>
<p>Mit <strong>Ausreißer-Notizen</strong> können die niedrigste und die höchste Stimme einen Grund in einer Zeile hinzufügen. Nach dem Aufdecken sehen alle, warum jemand 3 und jemand 13 gewählt hat, bevor überhaupt jemand spricht.</p>

<h2>Kurz halten</h2>
<p>Ein <strong>Diskussions-Timer</strong> von zwei, drei oder fünf Minuten startet nach dem Aufdecken. Läuft er ab, stimmt das Team neu ab oder der Host wählt die Schätzung.</p>

<h2>Wissen, wie sicher das Team ist</h2>
<p>Eine <strong>Sicherheitsabfrage</strong> fragt nach der Schätzung alle, wie sicher sie sind, von 1 bis 5. Geringe Sicherheit verdient eine Notiz im Ticket und taucht in der Sprint-Retro auf.</p>

<h2>Zurückstellen, was sich nicht schätzen lässt</h2>
<p><strong>Braucht mehr Informationen</strong> markiert ein Ticket, das zurück an seinen Autor gehört, statt geraten zu werden. <strong>Reaktionen</strong> lassen Leute zustimmen, ohne zu unterbrechen.</p>
<p>Alles steht im Raum-Menü unter den Diskussionsoptionen, und jeder Teil lässt sich ausschalten.</p>
""",
    },
}

ORDER = list(POSTS)

INDEX = {
    "en": {"title": "Blog", "nav": "Blog", "description": "What is new in Open Planning Poker and how to get more out of planning poker, retros and sprint planning.",
           "lead": "Short articles about features and about running estimation and retros well.", "read": "Read"},
    "sr": {"title": "Blog", "nav": "Blog", "description": "Šta je novo u Open Planning Pokeru i kako da izvučeš više iz planning pokera, retrospektiva i planiranja sprinta.",
           "lead": "Kratki članci o funkcijama i o tome kako dobro voditi procene i retrospektive.", "read": "Pročitaj"},
    "de": {"title": "Blog", "nav": "Blog", "description": "Neues in Open Planning Poker und wie du mehr aus Planning Poker, Retros und Sprint-Planung herausholst.",
           "lead": "Kurze Artikel über Funktionen und darüber, wie man Schätzungen und Retros gut führt.", "read": "Lesen"},
}


def index_page(lang):
    t = INDEX[lang]
    items = "\n".join(
        f'<article class="not-prose mt-6"><h2 class="font-display text-xl font-extrabold text-gray-900 dark:text-white"><a class="link" href="{prefix(lang)}blog/{slug}/">{html.escape(POSTS[slug][lang]["title"])}</a></h2>'
        f'<p class="mt-2">{html.escape(POSTS[slug][lang]["description"])}</p></article>'
        for slug in ORDER)
    return {"title": t["title"], "nav": t["nav"], "description": t["description"], "lead": t["lead"], "body": items}


def write(slug, lang, page, related, kind):
    out = render(slug, lang, page, related, DATE, kind)
    target = ROOT / "pages" / f"{slug}.html" if lang == "en" else ROOT / "i18n" / lang / f"{slug}.html"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(out, encoding="utf-8", newline="\n")


for lang in ("en", "sr", "de"):
    posts = [(f"blog/{s}", POSTS[s][lang]["nav"]) for s in ORDER]
    write("blog", lang, index_page(lang), posts[:3], "WebPage")
    for i, slug in enumerate(ORDER):
        page = dict(POSTS[slug][lang])
        related = [p for p in posts if p[0] != f"blog/{slug}"][:2] + [("blog", INDEX[lang]["nav"])]
        write(f"blog/{slug}", lang, page, related, "Article")
print("wrote", len(ORDER), "articles and the index")
