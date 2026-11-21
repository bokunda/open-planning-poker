"""Writes the integrations, privacy, support and terms pages in English, Serbian and German (OPP-113).

Run from the website folder: python scripts/make-legal-pages.py
The output is plain HTML in pages/ (English) and i18n/<lang>/ (translations), like the guides.
"""
import html
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
SITE = "https://openplanningpoker.com"
DATE = "2026-11-21"
SLACK_INSTALL = "https://ge.openplanningpoker.com/integrations/slack/install"
TEAMS_PACKAGE = "https://github.com/bokunda/open-planning-poker/releases/latest"
GITHUB = "https://github.com/bokunda/open-planning-poker"
ISSUES = "https://github.com/bokunda/open-planning-poker/issues"

UI = {
    "en": {"home": "Home", "more": "More pages", "ctaTitle": "Try it with your team.", "ctaText": "Free, no sign-up. Share one link and you are playing.", "cta": "Start a game", "updated": "Last updated"},
    "sr": {"home": "Početna", "more": "Još stranica", "ctaTitle": "Probaj sa svojim timom.", "ctaText": "Besplatno, bez registracije. Podeli jedan link i igra počinje.", "cta": "Pokreni igru", "updated": "Poslednja izmena"},
    "de": {"home": "Startseite", "more": "Weitere Seiten", "ctaTitle": "Probier es mit deinem Team aus.", "ctaText": "Kostenlos, ohne Anmeldung. Teile einen Link und ihr spielt.", "cta": "Spiel starten", "updated": "Zuletzt geändert"},
}

PAGES = {
    "integrations": {
        "en": {
            "title": "Integrations: Slack, Microsoft Teams and Jira",
            "nav": "Integrations",
            "description": "Start planning poker from Slack with /poker, add it as a tab in a Microsoft Teams meeting, and import a Jira sprint and save the estimates back. Free and open source.",
            "lead": "Open Planning Poker works where your team already works. Every integration is free and needs no account: players still join with a link.",
            "body": f"""
<h2>Slack</h2>
<p>Type <code>/poker Sprint 43</code> in any channel. Open Planning Poker creates a room and posts a <strong>Join</strong> button. Only you get the link to run the game. After each reveal, the ticket and its estimate are posted in the thread.</p>
<p><a class="link" href="{SLACK_INSTALL}">Add to Slack</a></p>

<h2>Microsoft Teams</h2>
<p>Add Open Planning Poker as a tab in a meeting, a channel or a chat. Start a new game or show one you already have. Everyone votes from the tab, in the meeting side panel or on the shared stage.</p>
<p>A Teams admin uploads the app package once in the Teams admin center. The package <code>open-planning-poker-teams.zip</code> is attached to every <a class="link" href="{TEAMS_PACKAGE}">release on GitHub</a>.</p>

<h2>Jira Cloud</h2>
<p>Connect your Jira site once from the import dialog. <strong>From Jira</strong> imports the issues of a sprint, a board or a JQL query, with their story points. After the reveal, <strong>Save to KEY</strong> writes the agreed estimate to the issue, optionally with a comment showing the votes and the notes of the lowest and highest voters.</p>

<h2>GitHub, Linear and Azure DevOps</h2>
<p>The same import and save-back works for GitHub Issues, Linear and Azure DevOps Boards. Step-by-step pages: <a class="link" href="/planning-poker-for-jira/">Jira</a>, <a class="link" href="/planning-poker-for-github/">GitHub</a>, <a class="link" href="/planning-poker-for-linear/">Linear</a> and <a class="link" href="/planning-poker-for-azure-devops/">Azure DevOps</a>.</p>

<h2>What is stored</h2>
<ul>
  <li>Slack and Jira tokens are stored encrypted on the server and never reach the browser. Disconnecting Jira deletes its tokens.</li>
  <li>Teams needs no tokens: the tab is the normal web app inside Teams.</li>
  <li>Read the <a class="link" href="/privacy/">privacy policy</a> for the details.</li>
</ul>
""",
        },
        "sr": {
            "title": "Integracije: Slack, Microsoft Teams i Jira",
            "nav": "Integracije",
            "description": "Pokreni planning poker iz Slacka komandom /poker, dodaj ga kao tab u Microsoft Teams sastanak, uvezi Jira sprint i sačuvaj procene nazad. Besplatno i otvorenog koda.",
            "lead": "Open Planning Poker radi tamo gde tvoj tim već radi. Svaka integracija je besplatna i ne traži nalog: igrači i dalje ulaze preko linka.",
            "body": f"""
<h2>Slack</h2>
<p>Ukucaj <code>/poker Sprint 43</code> u bilo kom kanalu. Open Planning Poker pravi sobu i objavljuje dugme <strong>Join</strong>. Samo ti dobijaš link za vođenje igre. Posle svakog otkrivanja karata, tiket i njegova procena stižu u thread.</p>
<p><a class="link" href="{SLACK_INSTALL}">Dodaj u Slack</a></p>

<h2>Microsoft Teams</h2>
<p>Dodaj Open Planning Poker kao tab u sastanak, kanal ili chat. Pokreni novu igru ili prikaži postojeću. Svi glasaju iz taba, u bočnom panelu sastanka ili na zajedničkom ekranu.</p>
<p>Teams administrator jednom otprema paket aplikacije u Teams admin centru. Paket <code>open-planning-poker-teams.zip</code> je priložen uz svako <a class="link" href="{TEAMS_PACKAGE}">izdanje na GitHubu</a>.</p>

<h2>Jira Cloud</h2>
<p>Poveži svoj Jira sajt jednom iz dijaloga za uvoz. <strong>From Jira</strong> uvozi issue-e iz sprinta, table ili JQL upita, zajedno sa story points. Posle otkrivanja karata, <strong>Save to KEY</strong> upisuje dogovorenu procenu u issue, po želji sa komentarom koji prikazuje glasove i beleške najnižeg i najvišeg glasa.</p>

<h2>GitHub, Linear i Azure DevOps</h2>
<p>Isti uvoz i čuvanje nazad rade i za GitHub Issues, Linear i Azure DevOps Boards. Stranice korak po korak: <a class="link" href="/sr/planning-poker-for-jira/">Jira</a>, <a class="link" href="/sr/planning-poker-for-github/">GitHub</a>, <a class="link" href="/sr/planning-poker-for-linear/">Linear</a> i <a class="link" href="/sr/planning-poker-for-azure-devops/">Azure DevOps</a>.</p>

<h2>Šta se čuva</h2>
<ul>
  <li>Slack i Jira tokeni se čuvaju šifrovani na serveru i nikad ne stižu do browsera. Prekid veze sa Jirom briše njene tokene.</li>
  <li>Teams ne traži tokene: tab je obična web aplikacija unutar Teams-a.</li>
  <li>Detalje pročitaj u <a class="link" href="/sr/privacy/">politici privatnosti</a>.</li>
</ul>
""",
        },
        "de": {
            "title": "Integrationen: Slack, Microsoft Teams und Jira",
            "nav": "Integrationen",
            "description": "Starte Planning Poker in Slack mit /poker, füge es als Tab in ein Microsoft-Teams-Meeting ein, importiere einen Jira-Sprint und speichere die Schätzungen zurück. Kostenlos und Open Source.",
            "lead": "Open Planning Poker funktioniert dort, wo dein Team schon arbeitet. Jede Integration ist kostenlos und braucht kein Konto: Spieler treten weiterhin über einen Link bei.",
            "body": f"""
<h2>Slack</h2>
<p>Tippe <code>/poker Sprint 43</code> in einen beliebigen Kanal. Open Planning Poker erstellt einen Raum und postet einen <strong>Join</strong>-Button. Nur du bekommst den Link, um das Spiel zu leiten. Nach jedem Aufdecken erscheinen das Ticket und seine Schätzung im Thread.</p>
<p><a class="link" href="{SLACK_INSTALL}">Zu Slack hinzufügen</a></p>

<h2>Microsoft Teams</h2>
<p>Füge Open Planning Poker als Tab in ein Meeting, einen Kanal oder einen Chat ein. Starte ein neues Spiel oder zeige ein vorhandenes. Alle stimmen im Tab ab, im Seitenbereich des Meetings oder auf der gemeinsamen Bühne.</p>
<p>Ein Teams-Admin lädt das App-Paket einmal im Teams Admin Center hoch. Das Paket <code>open-planning-poker-teams.zip</code> hängt an jedem <a class="link" href="{TEAMS_PACKAGE}">Release auf GitHub</a>.</p>

<h2>Jira Cloud</h2>
<p>Verbinde deine Jira-Site einmal im Import-Dialog. <strong>From Jira</strong> importiert die Issues eines Sprints, Boards oder einer JQL-Abfrage mit ihren Story Points. Nach dem Aufdecken schreibt <strong>Save to KEY</strong> die vereinbarte Schätzung in das Issue, auf Wunsch mit einem Kommentar mit den Stimmen und den Notizen der niedrigsten und höchsten Stimme.</p>

<h2>GitHub, Linear und Azure DevOps</h2>
<p>Derselbe Import und das Zurückspeichern funktionieren auch für GitHub Issues, Linear und Azure DevOps Boards. Schritt-für-Schritt-Seiten: <a class="link" href="/de/planning-poker-for-jira/">Jira</a>, <a class="link" href="/de/planning-poker-for-github/">GitHub</a>, <a class="link" href="/de/planning-poker-for-linear/">Linear</a> und <a class="link" href="/de/planning-poker-for-azure-devops/">Azure DevOps</a>.</p>

<h2>Was gespeichert wird</h2>
<ul>
  <li>Slack- und Jira-Tokens werden verschlüsselt auf dem Server gespeichert und erreichen nie den Browser. Beim Trennen von Jira werden seine Tokens gelöscht.</li>
  <li>Teams braucht keine Tokens: Der Tab ist die normale Web-App innerhalb von Teams.</li>
  <li>Details stehen in der <a class="link" href="/de/privacy/">Datenschutzerklärung</a>.</li>
</ul>
""",
        },
    },
    "privacy": {
        "en": {
            "title": "Privacy policy",
            "nav": "Privacy",
            "description": "What Open Planning Poker stores, for how long, and what the Slack, Microsoft Teams and Jira integrations can access.",
            "lead": "Open Planning Poker is built to need as little data as possible. There are no accounts, no passwords and no ads. The code is open, so you can check everything below.",
            "body": f"""
<h2>Who runs it</h2>
<p>openplanningpoker.com and app.openplanningpoker.com are run by Bojan Piskulic (Bitcrafta). Contact: open an issue on <a class="link" href="{ISSUES}">GitHub</a>.</p>

<h2>What we store</h2>
<ul>
  <li><strong>Usernames</strong>: random by default or the name you type. Deleted 24 hours after they are created.</li>
  <li><strong>Games, tickets, votes and notes</strong>: kept in our database so teams can see past estimates, team rooms and reports. A host can delete a game at any time.</li>
  <li><strong>Chat</strong>: the last 100 messages per game, deleted after 24 hours.</li>
  <li><strong>Reactions</strong>: sent to the room and never stored.</li>
  <li>We do not store email addresses, passwords or payment details.</li>
</ul>

<h2>Integrations</h2>
<ul>
  <li><strong>Slack</strong>: we store the workspace's bot token (encrypted) to post the room and the results. We read only the text of the <code>/poker</code> command. We do not read messages.</li>
  <li><strong>Microsoft Teams</strong>: the tab is our web app inside Teams. We store which game a tab shows. We do not access Teams data.</li>
  <li><strong>Jira Cloud</strong>: we store the access tokens of the person who connected Jira (encrypted). We read the issues you import and write the estimate and an optional comment when you click Save. Disconnecting deletes the tokens.</li>
</ul>

<h2>Analytics</h2>
<p>We use Google Analytics and Microsoft Clarity on the website and the app to see which pages and features are used. They do not see ticket names, votes or chat.</p>
<p>Our own server also counts, without cookies, how often each website page is opened and how many rooms are created from it. Only the name of the page is counted: no IP address, no user id.</p>

<h2>Where the data is</h2>
<p>Our servers are at Hetzner in Germany (EU). Backups are encrypted and kept for 14 days.</p>

<h2>Your choices</h2>
<p>You can use Open Planning Poker without any integration. To have a game or a connection removed, delete it in the app or ask us on GitHub.</p>
""",
        },
        "sr": {
            "title": "Politika privatnosti",
            "nav": "Privatnost",
            "description": "Šta Open Planning Poker čuva, koliko dugo, i čemu mogu da pristupe Slack, Microsoft Teams i Jira integracije.",
            "lead": "Open Planning Poker je napravljen tako da mu treba što manje podataka. Nema naloga, lozinki ni reklama. Kod je otvoren, pa sve ovo možeš da proveriš.",
            "body": f"""
<h2>Ko ga vodi</h2>
<p>openplanningpoker.com i app.openplanningpoker.com vodi Bojan Piskulić (Bitcrafta). Kontakt: otvori issue na <a class="link" href="{ISSUES}">GitHubu</a>.</p>

<h2>Šta čuvamo</h2>
<ul>
  <li><strong>Korisnička imena</strong>: nasumična ili ono koje upišeš. Brišu se 24 sata posle nastanka.</li>
  <li><strong>Igre, tiketi, glasovi i beleške</strong>: čuvaju se u bazi da bi timovi videli ranije procene, timske sobe i izveštaje. Host može da obriše igru bilo kada.</li>
  <li><strong>Chat</strong>: poslednjih 100 poruka po igri, brišu se posle 24 sata.</li>
  <li><strong>Reakcije</strong>: šalju se sobi i nikad se ne čuvaju.</li>
  <li>Ne čuvamo email adrese, lozinke ni podatke o plaćanju.</li>
</ul>

<h2>Integracije</h2>
<ul>
  <li><strong>Slack</strong>: čuvamo bot token radnog prostora (šifrovan) da bismo objavili sobu i rezultate. Čitamo samo tekst komande <code>/poker</code>. Ne čitamo poruke.</li>
  <li><strong>Microsoft Teams</strong>: tab je naša web aplikacija unutar Teams-a. Čuvamo koju igru tab prikazuje. Ne pristupamo podacima iz Teams-a.</li>
  <li><strong>Jira Cloud</strong>: čuvamo pristupne tokene osobe koja je povezala Jiru (šifrovane). Čitamo issue-e koje uvezeš i upisujemo procenu i opcioni komentar kada klikneš Save. Prekid veze briše tokene.</li>
</ul>

<h2>Analitika</h2>
<p>Na sajtu i u aplikaciji koristimo Google Analytics i Microsoft Clarity da vidimo koje stranice i funkcije se koriste. Oni ne vide nazive tiketa, glasove ni chat.</p>
<p>Naš server takođe broji, bez kolačića, koliko puta je otvorena svaka stranica sajta i koliko je soba napravljeno sa nje. Broji se samo naziv stranice: bez IP adrese i bez ID-ja korisnika.</p>

<h2>Gde su podaci</h2>
<p>Naši serveri su kod Hetznera u Nemačkoj (EU). Rezervne kopije su šifrovane i čuvaju se 14 dana.</p>

<h2>Tvoj izbor</h2>
<p>Open Planning Poker možeš da koristiš bez ijedne integracije. Da bi igra ili veza bila uklonjena, obriši je u aplikaciji ili nam piši na GitHubu.</p>
""",
        },
        "de": {
            "title": "Datenschutzerklärung",
            "nav": "Datenschutz",
            "description": "Was Open Planning Poker speichert, wie lange, und worauf die Integrationen für Slack, Microsoft Teams und Jira zugreifen können.",
            "lead": "Open Planning Poker braucht so wenig Daten wie möglich. Es gibt keine Konten, keine Passwörter und keine Werbung. Der Code ist offen, du kannst alles unten selbst prüfen.",
            "body": f"""
<h2>Wer es betreibt</h2>
<p>openplanningpoker.com und app.openplanningpoker.com werden von Bojan Piskulic (Bitcrafta) betrieben. Kontakt: Eröffne ein Issue auf <a class="link" href="{ISSUES}">GitHub</a>.</p>

<h2>Was wir speichern</h2>
<ul>
  <li><strong>Benutzernamen</strong>: zufällig oder der Name, den du eingibst. Gelöscht 24 Stunden nach dem Erstellen.</li>
  <li><strong>Spiele, Tickets, Stimmen und Notizen</strong>: in unserer Datenbank, damit Teams frühere Schätzungen, Team-Räume und Berichte sehen. Ein Host kann ein Spiel jederzeit löschen.</li>
  <li><strong>Chat</strong>: die letzten 100 Nachrichten pro Spiel, gelöscht nach 24 Stunden.</li>
  <li><strong>Reaktionen</strong>: an den Raum gesendet und nie gespeichert.</li>
  <li>Wir speichern keine E-Mail-Adressen, Passwörter oder Zahlungsdaten.</li>
</ul>

<h2>Integrationen</h2>
<ul>
  <li><strong>Slack</strong>: Wir speichern das Bot-Token des Workspaces (verschlüsselt), um den Raum und die Ergebnisse zu posten. Wir lesen nur den Text des Befehls <code>/poker</code>. Nachrichten lesen wir nicht.</li>
  <li><strong>Microsoft Teams</strong>: Der Tab ist unsere Web-App innerhalb von Teams. Wir speichern, welches Spiel ein Tab zeigt. Auf Teams-Daten greifen wir nicht zu.</li>
  <li><strong>Jira Cloud</strong>: Wir speichern die Zugriffstokens der Person, die Jira verbunden hat (verschlüsselt). Wir lesen die importierten Issues und schreiben die Schätzung und einen optionalen Kommentar, wenn du auf Save klickst. Beim Trennen werden die Tokens gelöscht.</li>
</ul>

<h2>Analyse</h2>
<p>Auf der Website und in der App nutzen wir Google Analytics und Microsoft Clarity, um zu sehen, welche Seiten und Funktionen genutzt werden. Sie sehen keine Ticketnamen, Stimmen oder Chats.</p>
<p>Unser eigener Server zählt außerdem ohne Cookies, wie oft jede Seite der Website geöffnet wird und wie viele Räume von ihr aus erstellt werden. Gezählt wird nur der Name der Seite: keine IP-Adresse, keine Nutzer-ID.</p>

<h2>Wo die Daten liegen</h2>
<p>Unsere Server stehen bei Hetzner in Deutschland (EU). Backups sind verschlüsselt und werden 14 Tage aufbewahrt.</p>

<h2>Deine Wahl</h2>
<p>Du kannst Open Planning Poker ohne jede Integration nutzen. Um ein Spiel oder eine Verbindung entfernen zu lassen, lösche es in der App oder schreib uns auf GitHub.</p>
""",
        },
    },
    "support": {
        "en": {
            "title": "Support",
            "nav": "Support",
            "description": "Get help with Open Planning Poker and its Slack, Microsoft Teams and Jira integrations: guides, known questions and how to report a problem.",
            "lead": "Open Planning Poker is free and open source. Help comes from the guides, the community and the maintainer on GitHub.",
            "body": f"""
<h2>Start here</h2>
<ul>
  <li><a class="link" href="/open-planning-poker-guide/">How to use Open Planning Poker</a>: rooms, roles, voting, the reveal and reports.</li>
  <li><a class="link" href="/integrations/">Integrations</a>: Slack, Microsoft Teams and Jira.</li>
  <li><a class="link" href="/changelog/">Changelog</a>: what changed in each release.</li>
</ul>

<h2>Common questions</h2>
<ul>
  <li><strong>The /poker command does not answer.</strong> Reinstall the app from the <a class="link" href="/integrations/">integrations page</a>; the install may have been removed by a workspace admin.</li>
  <li><strong>The Teams tab is blank.</strong> Ask your Teams admin to allow the app, and check that app.openplanningpoker.com is not blocked by your network.</li>
  <li><strong>Save to Jira is missing.</strong> The ticket has to be imported from Jira, and the issue needs a Story Points field on its edit screen.</li>
</ul>

<h2>Report a problem</h2>
<p>Open an issue on <a class="link" href="{ISSUES}">GitHub</a> with what you did, what you expected and what happened. Do not post tokens or private data.</p>

<h2 id="badge">Add the badge</h2>
<p>Using Open Planning Poker in your team? Put the badge in a README or team wiki. There is a light and a dark version; the dark one is <code>badge-dark.svg</code>.</p>
<p class="not-prose">
  <img src="/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="inline-block dark:hidden">
  <img src="/images/badge-dark.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="hidden dark:inline-block">
</p>
<p><strong>Markdown</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>[![We estimate with Open Planning Poker](https://openplanningpoker.com/images/badge-light.svg)](https://openplanningpoker.com/?utm_source=badge)</code></pre>
<p><strong>HTML</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>&lt;a href="https://openplanningpoker.com/?utm_source=badge"&gt;&lt;img src="https://openplanningpoker.com/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28"&gt;&lt;/a&gt;</code></pre>
""",
        },
        "sr": {
            "title": "Podrška",
            "nav": "Podrška",
            "description": "Pomoć za Open Planning Poker i njegove Slack, Microsoft Teams i Jira integracije: vodiči, česta pitanja i kako prijaviti problem.",
            "lead": "Open Planning Poker je besplatan i otvorenog koda. Pomoć stiže iz vodiča, zajednice i od autora na GitHubu.",
            "body": f"""
<h2>Kreni odavde</h2>
<ul>
  <li><a class="link" href="/sr/open-planning-poker-guide/">Kako koristiti Open Planning Poker</a>: sobe, uloge, glasanje, otkrivanje karata i izveštaji.</li>
  <li><a class="link" href="/sr/integrations/">Integracije</a>: Slack, Microsoft Teams i Jira.</li>
  <li><a class="link" href="/sr/changelog/">Spisak izmena</a>: šta se promenilo u svakom izdanju.</li>
</ul>

<h2>Česta pitanja</h2>
<ul>
  <li><strong>Komanda /poker ne odgovara.</strong> Ponovo instaliraj aplikaciju sa <a class="link" href="/sr/integrations/">stranice integracija</a>; možda ju je administrator radnog prostora uklonio.</li>
  <li><strong>Teams tab je prazan.</strong> Zamoli Teams administratora da dozvoli aplikaciju i proveri da mreža ne blokira app.openplanningpoker.com.</li>
  <li><strong>Nema dugmeta Save to Jira.</strong> Tiket mora biti uvezen iz Jire, a issue mora imati polje Story Points na ekranu za izmenu.</li>
</ul>

<h2>Prijavi problem</h2>
<p>Otvori issue na <a class="link" href="{ISSUES}">GitHubu</a>: šta si uradio, šta si očekivao i šta se desilo. Ne objavljuj tokene ni privatne podatke.</p>

<h2 id="badge">Dodaj bedž</h2>
<p>Vaš tim koristi Open Planning Poker? Stavite bedž u README ili timski wiki. Postoji svetla i tamna verzija; tamna je <code>badge-dark.svg</code>.</p>
<p class="not-prose">
  <img src="/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="inline-block dark:hidden">
  <img src="/images/badge-dark.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="hidden dark:inline-block">
</p>
<p><strong>Markdown</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>[![We estimate with Open Planning Poker](https://openplanningpoker.com/images/badge-light.svg)](https://openplanningpoker.com/?utm_source=badge)</code></pre>
<p><strong>HTML</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>&lt;a href="https://openplanningpoker.com/?utm_source=badge"&gt;&lt;img src="https://openplanningpoker.com/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28"&gt;&lt;/a&gt;</code></pre>
""",
        },
        "de": {
            "title": "Support",
            "nav": "Support",
            "description": "Hilfe zu Open Planning Poker und seinen Integrationen für Slack, Microsoft Teams und Jira: Anleitungen, häufige Fragen und wie du ein Problem meldest.",
            "lead": "Open Planning Poker ist kostenlos und Open Source. Hilfe gibt es in den Anleitungen, in der Community und beim Maintainer auf GitHub.",
            "body": f"""
<h2>Hier anfangen</h2>
<ul>
  <li><a class="link" href="/de/open-planning-poker-guide/">So nutzt du Open Planning Poker</a>: Räume, Rollen, Abstimmen, Aufdecken und Berichte.</li>
  <li><a class="link" href="/de/integrations/">Integrationen</a>: Slack, Microsoft Teams und Jira.</li>
  <li><a class="link" href="/de/changelog/">Änderungsprotokoll</a>: was sich in jedem Release geändert hat.</li>
</ul>

<h2>Häufige Fragen</h2>
<ul>
  <li><strong>Der Befehl /poker antwortet nicht.</strong> Installiere die App erneut über die <a class="link" href="/de/integrations/">Seite Integrationen</a>; vielleicht hat ein Workspace-Admin sie entfernt.</li>
  <li><strong>Der Teams-Tab ist leer.</strong> Bitte deinen Teams-Admin, die App zu erlauben, und prüfe, dass dein Netzwerk app.openplanningpoker.com nicht blockiert.</li>
  <li><strong>Save to Jira fehlt.</strong> Das Ticket muss aus Jira importiert sein, und das Issue braucht ein Story-Points-Feld in seiner Bearbeitungsmaske.</li>
</ul>

<h2>Problem melden</h2>
<p>Eröffne ein Issue auf <a class="link" href="{ISSUES}">GitHub</a>: was du getan hast, was du erwartet hast und was passiert ist. Poste keine Tokens oder privaten Daten.</p>

<h2 id="badge">Badge einbinden</h2>
<p>Ihr Team nutzt Open Planning Poker? Setzen Sie das Badge in ein README oder Team-Wiki. Es gibt eine helle und eine dunkle Version; die dunkle ist <code>badge-dark.svg</code>.</p>
<p class="not-prose">
  <img src="/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="inline-block dark:hidden">
  <img src="/images/badge-dark.svg" alt="We estimate with Open Planning Poker" width="236" height="28" class="hidden dark:inline-block">
</p>
<p><strong>Markdown</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>[![We estimate with Open Planning Poker](https://openplanningpoker.com/images/badge-light.svg)](https://openplanningpoker.com/?utm_source=badge)</code></pre>
<p><strong>HTML</strong></p>
<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>&lt;a href="https://openplanningpoker.com/?utm_source=badge"&gt;&lt;img src="https://openplanningpoker.com/images/badge-light.svg" alt="We estimate with Open Planning Poker" width="236" height="28"&gt;&lt;/a&gt;</code></pre>
""",
        },
    },
    "terms": {
        "en": {
            "title": "Terms of use",
            "nav": "Terms",
            "description": "The terms for using the hosted Open Planning Poker at openplanningpoker.com and its integrations.",
            "lead": "Open Planning Poker is free software under the GPL-3.0 license. These terms cover the hosted service at openplanningpoker.com.",
            "body": f"""
<h2>The service</h2>
<p>The hosted app is free and offered as it is, without warranty. We try to keep it running, but there is no guaranteed uptime. You can always run your own copy from the <a class="link" href="{GITHUB}">source code</a>.</p>

<h2>Fair use</h2>
<ul>
  <li>Use it for estimation and planning. Do not use it to send spam, attack the service or store content that is illegal.</li>
  <li>Do not put secrets (passwords, keys) in ticket names, descriptions, notes or chat.</li>
  <li>We may limit requests or remove games that break these rules.</li>
</ul>

<h2>Your content</h2>
<p>Tickets, votes, notes and chat belong to you and your team. We only use them to run the service. See the <a class="link" href="/privacy/">privacy policy</a> for what is stored and for how long.</p>

<h2>Integrations</h2>
<p>Slack, Microsoft Teams and Jira are products of their own companies, with their own terms. Installing an integration connects your workspace or site to Open Planning Poker as described on the <a class="link" href="/integrations/">integrations page</a>.</p>

<h2>Changes</h2>
<p>When these terms change, the date below changes and the change is listed in the <a class="link" href="/changelog/">changelog</a>.</p>
""",
        },
        "sr": {
            "title": "Uslovi korišćenja",
            "nav": "Uslovi",
            "description": "Uslovi za korišćenje hostovanog Open Planning Pokera na openplanningpoker.com i njegovih integracija.",
            "lead": "Open Planning Poker je slobodan softver pod GPL-3.0 licencom. Ovi uslovi važe za hostovani servis na openplanningpoker.com.",
            "body": f"""
<h2>Servis</h2>
<p>Hostovana aplikacija je besplatna i nudi se takva kakva je, bez garancije. Trudimo se da radi, ali dostupnost nije garantovana. Uvek možeš da pokreneš svoju kopiju iz <a class="link" href="{GITHUB}">izvornog koda</a>.</p>

<h2>Pošteno korišćenje</h2>
<ul>
  <li>Koristi ga za procene i planiranje. Ne koristi ga za spam, napade na servis ili nezakonit sadržaj.</li>
  <li>Ne stavljaj tajne (lozinke, ključeve) u nazive tiketa, opise, beleške ni chat.</li>
  <li>Možemo da ograničimo zahteve ili uklonimo igre koje krše ova pravila.</li>
</ul>

<h2>Tvoj sadržaj</h2>
<p>Tiketi, glasovi, beleške i chat pripadaju tebi i tvom timu. Koristimo ih samo da bi servis radio. U <a class="link" href="/sr/privacy/">politici privatnosti</a> piše šta se čuva i koliko dugo.</p>

<h2>Integracije</h2>
<p>Slack, Microsoft Teams i Jira su proizvodi svojih kompanija, sa svojim uslovima. Instaliranje integracije povezuje tvoj radni prostor ili sajt sa Open Planning Pokerom kako je opisano na <a class="link" href="/sr/integrations/">stranici integracija</a>.</p>

<h2>Izmene</h2>
<p>Kada se ovi uslovi promene, menja se datum ispod, a izmena se navodi u <a class="link" href="/sr/changelog/">spisku izmena</a>.</p>
""",
        },
        "de": {
            "title": "Nutzungsbedingungen",
            "nav": "Nutzungsbedingungen",
            "description": "Die Bedingungen für die Nutzung des gehosteten Open Planning Poker auf openplanningpoker.com und seiner Integrationen.",
            "lead": "Open Planning Poker ist freie Software unter der GPL-3.0-Lizenz. Diese Bedingungen gelten für den gehosteten Dienst auf openplanningpoker.com.",
            "body": f"""
<h2>Der Dienst</h2>
<p>Die gehostete App ist kostenlos und wird so angeboten, wie sie ist, ohne Gewährleistung. Wir bemühen uns, dass sie läuft, garantieren aber keine Verfügbarkeit. Du kannst jederzeit eine eigene Kopie aus dem <a class="link" href="{GITHUB}">Quellcode</a> betreiben.</p>

<h2>Faire Nutzung</h2>
<ul>
  <li>Nutze es zum Schätzen und Planen. Nutze es nicht für Spam, Angriffe auf den Dienst oder illegale Inhalte.</li>
  <li>Schreib keine Geheimnisse (Passwörter, Schlüssel) in Ticketnamen, Beschreibungen, Notizen oder den Chat.</li>
  <li>Wir können Anfragen begrenzen oder Spiele entfernen, die gegen diese Regeln verstoßen.</li>
</ul>

<h2>Deine Inhalte</h2>
<p>Tickets, Stimmen, Notizen und Chat gehören dir und deinem Team. Wir nutzen sie nur, um den Dienst zu betreiben. Die <a class="link" href="/de/privacy/">Datenschutzerklärung</a> beschreibt, was wie lange gespeichert wird.</p>

<h2>Integrationen</h2>
<p>Slack, Microsoft Teams und Jira sind Produkte ihrer eigenen Unternehmen, mit eigenen Bedingungen. Eine Integration verbindet deinen Workspace oder deine Site mit Open Planning Poker, wie auf der <a class="link" href="/de/integrations/">Seite Integrationen</a> beschrieben.</p>

<h2>Änderungen</h2>
<p>Wenn sich diese Bedingungen ändern, ändert sich das Datum unten, und die Änderung steht im <a class="link" href="/de/changelog/">Änderungsprotokoll</a>.</p>
""",
        },
    },
}

ORDER = ["integrations", "privacy", "support", "terms"]


def prefix(lang):
    return "/" if lang == "en" else f"/{lang}/"


def render(slug, lang):
    p = PAGES[slug][lang]
    ui = UI[lang]
    url = f"{SITE}{prefix(lang)}{slug}/"
    esc = html.escape
    ld = {
        "@context": "https://schema.org",
        "@graph": [
            {"@type": "BreadcrumbList", "itemListElement": [
                {"@type": "ListItem", "position": 1, "name": ui["home"], "item": f"{SITE}{prefix(lang)}"},
                {"@type": "ListItem", "position": 2, "name": p["nav"], "item": url},
            ]},
            {"@type": "WebPage", "name": p["title"], "description": p["description"], "url": url,
             "dateModified": DATE, "inLanguage": {"en": "en", "sr": "sr-Latn", "de": "de"}[lang],
             "publisher": {"@id": f"{SITE}/#org"}},
        ],
    }
    others = "\n".join(
        f'          <li><a href="{prefix(lang)}{other}/" class="link">{esc(PAGES[other][lang]["nav"])}</a></li>'
        for other in ORDER if other != slug)
    return f"""<!DOCTYPE html>
<html lang="{'sr-Latn' if lang == 'sr' else lang}">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{esc(p['title'])} | Open Planning Poker</title>
    <meta name="description" content="{esc(p['description'])}">
    <meta name="robots" content="index, follow, max-image-preview:large, max-snippet:-1">
    <link rel="canonical" href="{url}">
    <meta property="og:type" content="website">
    <meta property="og:url" content="{url}">
    <meta property="og:title" content="{esc(p['title'])}">
    <meta property="og:description" content="{esc(p['description'])}">
    <meta property="og:image" content="https://openplanningpoker.com/images/og-card.png">
    <meta property="og:image:width" content="1200">
    <meta property="og:image:height" content="630">
    <meta property="og:site_name" content="Open Planning Poker">
    <meta name="twitter:card" content="summary_large_image">
    <meta name="twitter:title" content="{esc(p['title'])}">
    <meta name="twitter:description" content="{esc(p['description'])}">
    <meta name="twitter:image" content="https://openplanningpoker.com/images/og-card.png">
    <!-- SITE_HEAD -->
    <script type="application/ld+json">
{json.dumps(ld, ensure_ascii=False, indent=2)}
    </script>
</head>
<body class="bg-white text-gray-600 font-sans antialiased dark:bg-gray-900 dark:text-gray-400">
    <!-- SITE_HEADER -->

    <main id="main">
      <article class="max-w-3xl px-4 pt-28 pb-16 mx-auto lg:pt-36">
        <nav aria-label="Breadcrumb" class="text-sm">
          <ol class="flex gap-2">
            <li><a href="{prefix(lang)}" class="link">{esc(ui['home'])}</a></li>
            <li aria-hidden="true">/</li>
            <li aria-current="page">{esc(p['nav'])}</li>
          </ol>
        </nav>
        <h1 class="mt-6 font-display text-4xl font-extrabold tracking-tight text-gray-900 sm:text-5xl dark:text-white">{esc(p['title'])}</h1>
        <p class="mt-5 text-lg leading-relaxed">{esc(p['lead'])}</p>

        <div class="guide mt-10">
{p['body'].strip()}
<p class="mt-10 text-sm">{esc(ui['updated'])}: <time datetime="{DATE}">{DATE}</time></p>
        </div>

        <aside class="mt-12 p-6 border border-gray-200 rounded-2xl dark:border-gray-800 bg-gray-50 dark:bg-gray-800/50" aria-labelledby="more-pages">
          <h2 id="more-pages" class="font-display text-lg font-extrabold text-gray-900 dark:text-white">{esc(ui['more'])}</h2>
          <ul class="mt-3 space-y-2">
{others}
          </ul>
        </aside>
      </article>

      <section class="px-4 pb-16 lg:pb-24">
        <div class="cta-band max-w-(--breakpoint-lg) mx-auto">
          <h2 class="font-display text-3xl font-extrabold tracking-tight text-white sm:text-4xl">{esc(ui['ctaTitle'])}</h2>
          <p class="mt-3 text-lg text-purple-100">{esc(ui['ctaText'])}</p>
          <a href="https://app.openplanningpoker.com" class="inline-flex items-center justify-center mt-8 px-6 py-3.5 font-bold rounded-full bg-white text-purple-700 shadow-lg hover:bg-purple-50 focus:outline-hidden focus-visible:ring-4 focus-visible:ring-white/60 transition">{esc(ui['cta'])}</a>
        </div>
      </section>
    </main>

    <!-- SITE_FOOTER -->
</body>
</html>
"""


for slug in ORDER:
    (ROOT / "pages" / f"{slug}.html").write_text(render(slug, "en"), encoding="utf-8", newline="\n")
    for lang in ("sr", "de"):
        (ROOT / "i18n" / lang / f"{slug}.html").write_text(render(slug, lang), encoding="utf-8", newline="\n")
    print("wrote", slug)
