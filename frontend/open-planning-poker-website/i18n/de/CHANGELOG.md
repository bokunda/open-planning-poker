# Änderungsprotokoll

Alle wichtigen Änderungen an Open Planning Poker sind in dieser Datei festgehalten. Dies ist die Übersetzung der englischen `CHANGELOG.md`; jedes Release muss in beiden stehen.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/), die Versionen folgen der [semantischen Versionierung](https://semver.org/lang/de/).

## [Unveröffentlicht]

## [2.4.1] — 2026-11-13

Die App passt aufs Handy, und zwei neue Anleitungen zum Spiel.

### Hinzugefügt
- **Neue Anleitungen**: „So nutzt du Open Planning Poker“, ein Rundgang durch das ganze Spiel (Räume, Rollen, Abstimmen, Aufdecken, endgültige Schätzung, Berichte, asynchrones Abstimmen und Teamräume), und „Asynchrones Planning Poker“ zum Schätzen vor dem Meeting. Beide auf Englisch, Serbisch und Deutsch und auf der Startseite

### Behoben
- **Handys**: Der Header schneidet deinen Namen nicht mehr ab (unter 420 px zeigt der Chip deine Initiale), und die Links im Footer brechen um, statt über den Bildschirm hinauszulaufen. Kein seitliches Scrollen bei 360–390 px
- **Dialog „Neues Spiel“**: Der Button „Spiel erstellen“ bleibt unten sichtbar, während du durch die Decks scrollst


## [2.4.0] — 2026-11-13

Die Anleitungen und das Änderungsprotokoll auf Serbisch und Deutsch, und Deploys ohne Passwörter.

### Hinzugefügt
- **Anleitungen und Änderungsprotokoll auf Serbisch und Deutsch**: alle vier Anleitungen und das Änderungsprotokoll unter `/sr/` und `/de/`, verknüpft über `hreflang`. Die Sprachauswahl im Footer bleibt auf derselben Seite, und die Sitemap listet jede Seite in jeder Sprache
- **Meldungen des User Management in deiner Sprache**: Fehler zum Benutzernamen, zu vielen Anfragen und einem unbekannten Benutzer folgen der Sprache der App, wie die der Game Engine

### Geändert
- „Neuigkeiten“ auf der serbischen und deutschen Startseite kommen aus dem übersetzten Änderungsprotokoll, mit dem Datum in der Sprache der Seite

### Behoben
- Auf `/sr/` und `/de/` führte der Link „English“ im Footer auf dieselbe übersetzte Seite

### Sicherheit
- Deploys melden sich mit einem eigenen SSH-Schlüssel statt mit einem Passwort auf den Servern an; `appleboy/scp-action` 1.0.0

### Für Entwickler
- Die übersetzten Änderungsprotokolle liegen in `frontend/open-planning-poker-website/i18n/<lang>/CHANGELOG.md`: Jedes Release aus `CHANGELOG.md` muss im selben PR dort ergänzt werden, sonst schlägt der Build der Website fehl


## [2.3.1] — 2026-11-13

### Geändert
- Der Footer der Website verlinkt nicht mehr auf die Anleitungen „Was ist Planning Poker“ und „Karten und Skalen“; sie stehen weiterhin unter Anleitungen im Menü und auf der Startseite

## [2.3.0] — 2026-11-13

Open Planning Poker in deiner Sprache, als App auf dem Handy installierbar, und jeder Pull Request von Anfang bis Ende getestet.

### Hinzugefügt
- **Serbisch und Deutsch**: Die App gibt es auf Englisch, Serbisch (lateinische Schrift) und Deutsch. Sie folgt der Sprache des Browsers, du kannst sie im Menü ändern, und die Wahl wird gespeichert. Auch Datumsangaben, der CSV-Export, Fehlermeldungen des Servers und der PDF-Bericht sind übersetzt
- **Website auf Serbisch und Deutsch**: die Startseite unter `/sr/` und `/de/`, verknüpft über `hreflang` und in der Sitemap aufgeführt, mit einer Sprachauswahl im Footer
- **Installierbare App**: Installiere Open Planning Poker auf deinem Handy oder Computer. Die App öffnet sich auch offline und zeigt den Hinweis „Du bist offline“, bis die Verbindung wieder da ist. Das Installationsangebot erscheint höchstens einmal pro Woche, und „App installieren“ steht im Menü
- **Angebot zum Neuladen**, wenn eine neue Version der App bereit ist

### Geändert
- Seiten werden auf dem Server in der Sprache jedes Besuchers gerendert, statt vorab erzeugt zu werden
- Das Menü der Website erscheint erst auf breiteren Bildschirmen, damit längere Bezeichnungen passen

### Für Entwickler
- Integrationstests für die Game Engine mit echtem PostgreSQL und Redis (Testcontainers): Aktionen nur für den Host, verdeckte Stimmen, erneut abstimmen, der Timer, Zugriff auf den Chat, übersetzte Fehler und der PDF-Bericht
- Playwright-End-to-End-Tests über den ganzen Stack (`docker-compose.e2e.yml`): Spiel erstellen, ein zweiter Spieler tritt bei, Tickets hinzufügen und importieren, abstimmen, aufdecken, erneut abstimmen, der gemeinsame Timer und der PDF-Export, mit axe-Prüfungen der Barrierefreiheit auf Desktop und Handy
- Ein CI-Workflow für jeden Pull Request: Unit- und Integrationstests des Backends, eine Prüfung auf verwundbare Pakete, Builds von App und Website, die Übersetzungsprüfung, End-to-End-Tests und Lighthouse-Budgets (Leistung ≥ 90, Barrierefreiheit 100, Best Practices ≥ 95, SEO ≥ 92, wobei jedes SEO-Audit einzeln geprüft wird)

## [2.2.1] — 2026-11-13

### Sicherheit
- Sicherheitshärtung des Chats und der WebSocket-Verbindung; der Chat lädt nach dem Beitritt zum Spiel
- Sicherheitsupdates von Abhängigkeiten: HotChocolate 15.1 in allen Diensten, OpenTelemetry 1.19 im gemeinsamen Paket

### Behoben
- Einen Spieler zum Beobachter oder Co-Host zu machen, schlug mit einem Serverfehler fehl (aus der Datenbank geladene Spieler verloren ihre ID, sodass die Änderung keine Zeile traf)
- Das serverseitige Rendern von Spielseiten öffnet keine WebSocket-Subscriptions mehr (Node 22 hat ein globales `WebSocket`), was den App-Server zum Absturz bringen konnte

### Geändert
- Das gemeinsame Paket `OpenPlanningPoker.Shared` 1.1.0 hängt nicht mehr von Prerelease-Paketen ab; die ungenutzten Console-, Prometheus- und Zipkin-Exporter, die HTTP-Instrumentierung und die Prozessmetriken wurden entfernt

## [2.2.0] — 2026-11-13

Leitet den Raum gemeinsam, schätzt vor dem Meeting und behaltet die Geschichte eines Teams an einem Ort.

### Hinzugefügt
- **Beobachter**: Tritt einem Spiel nur zum Zuschauen bei. Beobachter haben keine Karten, zählen nicht bei „x/y abgestimmt“ und können auch auf dem Server nicht abstimmen. Umschalten im Menü neben deinem Namen
- **Co-Hosts und Übergabe des Spiels**: Der Host kann Spieler zu Co-Hosts machen (sie fügen Tickets hinzu, decken auf, steuern den Timer und die Raumoptionen) oder jemand anderen zum Host machen. Hosts und Co-Hosts können einen Spieler entfernen; seine Stimmen bleiben an den Tickets
- **Endgültige Schätzung**: Nach dem Aufdecken wählt der Host die Karte, auf die sich das Team geeinigt hat (die häufigste Karte ist markiert). Sie wird in der Ticketliste angezeigt und im PDF-Bericht, im CSV-Export und in der Velocity des Teams verwendet
- **Automatisches Aufdecken**: eine Raumoption, die die Karten umdreht, sobald jeder Spieler (keine Beobachter) abgestimmt hat
- **Tickets bearbeiten, löschen und sortieren**: über das Menü der Ticketliste oder per Drag-and-drop. Ticketbeschreibungen unterstützen eine kleine Markdown-Teilmenge (fett, kursiv, Code, Listen, Links)
- **CSV-Export** der Ergebnisse, neben dem PDF-Bericht
- **Asynchrones Abstimmen**: Der Host öffnet ein Abstimmungsfenster (24 Stunden, 3 Tage oder eine Woche). Alle stimmen selbstständig über alle Tickets ab; die Stimmen bleiben verdeckt, bis der Host jedes Ticket im Meeting aufdeckt
- **Teamräume** (`/team`): ein dauerhafter Raum pro Team mit Einladungslink, Mitgliedern, Sprintplanungen, der Sprint-Historie und einem Velocity-Diagramm (Punkte pro Sprint, Durchschnitt der letzten drei Sprints, mit Tabellenansicht)
- **Das Projekt unterstützen**: ein „Buy me a coffee“-Button (Ko-fi) im Header und Footer der App und auf der Website, und GitHub Sponsors im App-Menü, im Über-Dialog, im Unterstützungsbereich der Website und in der README (`.github/FUNDING.yml`). Klicks werden als GA4-Ereignis `donate_click` gezählt (Plattform und Platzierung, keine personenbezogenen Daten). Der Über-Dialog und die Website stellen den Entwickler vor

### Geändert
- Spieler in einem Raum erhalten Live-Updates, wenn jemand die Rolle wechselt, Co-Host wird, den Raum verlässt oder entfernt wird, und wenn sich Tickets oder Raumoptionen ändern (`onGameChanged`)
- Ein Spiel löschen darf nur der Host (keine Co-Hosts)
- Datenbank: neue Spalten für den Host, Rollen, die Ticketreihenfolge, die endgültige Schätzung und Raumoptionen sowie Tabellen für Teamräume. Bestehende Spiele behalten ihren Ersteller als Host, und ihre Tickets behalten ihre Reihenfolge

## [2.1.1] — 2026-11-13

Ein neu gestalteter PDF-Bericht, dazu saubere CI-Läufe und Builds.

### Geändert
- **PDF-Bericht neu gestaltet**, passend zu App und Website: das Logo und das Markenviolett, Figtree und Bricolage Grotesque (voller Zeichensatz, damit Namen wie „Đorđe Čolić“ richtig gedruckt werden), Übersichtskacheln (Tickets, aufgedeckt, Spieler, durchschnittliche Schätzung) und eine Karte pro Ticket mit Schätzung, Durchschnitt, Median, einer Kennzeichnung Konsens / fast Konsens / große Streuung, einer Stimmverteilung und wer was gewählt hat. Seitenzahlen und ein Link zu openplanningpoker.com im Footer
- Die Schätzung jedes Tickets ist die häufigste Karte (bei Gleichstand die Karte, die dem Median am nächsten liegt), statt eines einfachen Durchschnitts
- QuestPDF 2025.1.7 → 2025.7.4 (abgerundete Ecken)
- **GitHub Actions auf Node 24**: `actions/checkout` v7.0.1 und `actions/setup-dotnet` v6.1.0 (die alten Versionen liefen auf dem veralteten Node 20)
- **Runner auf `ubuntu-24.04` festgelegt** statt `ubuntu-latest`, damit der Wechsel von `ubuntu-latest` auf Ubuntu 26 den Build nicht unangekündigt ändert

### Behoben
- **Ticketstimmen konnten mit einer Null-Referenz fehlschlagen**, wenn die Stimmenabfrage einen Fehler zurückgab; das Mapping liefert jetzt eine leere Liste
- **Build-Warnungen**: Nullable-Warnungen in den GraphQL-Mappings, fehlende XML-Dokumentation beim JWT-Setup und zwei Versionen von `Microsoft.EntityFrameworkCore.Relational` in der Game Engine (jetzt überall 9.0.3). Game Engine, User Management und das Fusion-Gateway bauen ohne Warnungen
- **Häkchen auf der Website**: Die Symbole in der Liste oben („Ohne Anmeldung“, „Läuft auf dem Handy“, ...) saßen 2px unter dem Text; jetzt sind sie bei jeder Schriftgröße auf der Zeile zentriert

## [2.1.0] — 2026-11-12

Ein gemeinsamer Timer für das ganze Team, ein schnellerer erster Start und Anleitungen auf der Website.

### Highlights
- **Gemeinsamer Timer**: Der Host startet ihn, alle sehen denselben Countdown, und die Karten werden aufgedeckt, wenn er abläuft
- **Schneller**: etwa 25 % weniger JavaScript beim ersten Laden, Schriften von unserer eigenen Domain, der Spielraum lädt erst, wenn ein Spiel offen ist
- **Raumaktionen immer sichtbar**: Link kopieren, PDF exportieren und Verlassen sind Buttons neben Einladen statt eines Menüs
- **Anleitungen auf der Website**: was Planning Poker ist, wie man eine Sitzung leitet, Kartenskalen und Selbsthosting
- **Suche und KI-Agenten**: Private Räume werden nicht mehr indexiert, echte 404-Seiten und ein ARD-Manifest (`/.well-known/ard.json`) für KI-Agenten

### Hinzugefügt
- **Gemeinsamer Abstimmungs-Timer**: Der Timer des Hosts läuft jetzt auf dem Server, sodass jeder Spieler denselben Countdown sieht (und den Ring auf dem Tisch), auch wer beitritt, während er läuft. Neue Mutationen `startTimer` / `stopTimer` (nur Host), Abfrage `gameTimer` und Subscription `onTimerChanged`; der Zustand liegt in Redis. Bei null werden die Karten aufgedeckt, wie bisher

### Geändert
- **Timer-Steuerung**: eine Pille mit der Zeit, Chips für die Dauer (30s, 1m, 2m, 3m, 5m) und Start/Stop, statt eines Dropdowns und dreier Symbolbuttons
- Großschreibung nur am Satzanfang für die übrigen Beschriftungen: „Reveal cards“, „Vote again“, „New ticket“, „Chat“, „Paste a list“, „Upload a file“, „T-shirt sizes“
- **Raum-Header**: „Link kopieren“, „PDF exportieren“ und „Verlassen“ sind immer neben „Einladen“ sichtbar (auf Handys nur als Symbole); das ⋮-Menü ist weg
- **Ticketbeschreibung** wird standardmäßig unter dem Ticketnamen angezeigt; der Info-Button blendet sie aus
- **„Modified Fibonacci“ heißt jetzt „Fibonacci v2“**, mit einem erklärenden Tooltip
- **Brotkrümelnavigation** ist auf breiten Bildschirmen am Header ausgerichtet

### Projekt
- **Releases**: Hat die Version in `package.json` noch kein Tag, setzt ein Workflow das Tag `vX.Y.Z` und veröffentlicht ein GitHub-Release mit dem CHANGELOG-Abschnitt dieser Version
- **Dependabot**: wöchentliche, gruppierte Updates für npm (App, Website), NuGet (alle Dienste), Docker-Images und GitHub Actions
- **Verfügbarkeitsprüfung** alle 15 Minuten (Website, Changelog, App, ai-catalog, GraphQL-Gateway); ein Ausfall öffnet ein GitHub-Issue mit dem Label `uptime`, die Erholung schließt es
- Die Build-Ausgabe der Website (`dist/`) und vier ungenutzte Bilder sind nicht mehr im Repository

### Leistung
- **Kleinerer erster Start**: initiales JavaScript 1148 → 857 KB (unkomprimiert). Der Spielraum ist ein eigener Chunk, der geladen wird, sobald ein Spiel offen ist (`@defer`); Dialoge (neues Spiel, Beitreten, Ticket, Import, Einladen, Benutzername, Über) laden beim ersten Öffnen; die Animations-Engine lädt nach dem Start (`provideAnimationsAsync`)
- **Demo-Tisch hydriert im Leerlauf**: weiterhin auf dem Server gerendert, sein JS lädt, wenn der Browser nichts zu tun hat (inkrementelle Hydration)
- **Symbole**: Nur die 25 Material Symbols, die die App nutzt, werden geladen, statt der ganzen Material-Icons-Schrift
- **Globales CSS** 168 → 74 KB: Material-Themes nur für die Komponenten, die die App nutzt
- **Analytics** (GA4, Clarity) laden, nachdem die Seite geladen ist und der Browser im Leerlauf ist; fast alle erzwungenen Layout-Lesevorgänge auf der Startseite kamen von Clarity
- **Schriften von unserer eigenen Domain** (App und Website): keine Anfragen an Google Fonts, Teilmengen latin + latin-ext, Bricolage Grotesque fest auf optische Größe 48 (42 KB statt 77 KB), vorab geladen
- **Die App ruft das Fusion-Gateway direkt auf**; das YARP-API-Gateway ist entfernt (ein Netzwerk-Hop weniger bei jeder Anfrage)
- **Caching der Website**: Das Stylesheet hat einen Inhalts-Hash im Namen, sodass ein neues Release nie hinter einem einjährigen Cache verborgen bleibt; Bilder werden eine Woche gecacht

### SEO
- **Anleitungen auf der Website**: vier neue Seiten (was Planning Poker ist, wie man eine Sitzung leitet, Karten und Skalen, Selbsthosting) mit strukturierten Daten für Article, FAQ, HowTo und Brotkrümel, verlinkt von der Startseite, dem Header und dem Footer
- **Echte 404-Seiten** auf Website und App (beide beantworteten unbekannte URLs mit 200 und der Startseite); die 404-Seite der Website nutzt das Design der Website
- **App**: Spielräume und Tickets sind `noindex` (sie sind privat und kurzlebig); `/game` verweist per Canonical auf `/`; die Überschrift der Startseite ist das `h1` der Seite; neue Beschreibung und eine Social-Media-Karte in 1200×630
- Sitemap mit den Anleitungen und Screenshots; `robots.txt` ohne `Crawl-delay`; `llms.txt` für die Website
- Gemeinsame Head-, Header- und Footer-Teile für jede Seite der Website
- **KI-Agenten**: Das ARD-Manifest wird unter `/.well-known/ard.json` ausgeliefert (der Ort, den die aktuelle Spezifikation und Lighthouse verwenden), angekündigt mit `<link rel="ard">` und einer `Agentmap:`-Zeile in `robots.txt`; `ai-catalog.json` bleibt für ältere Tools. Vorher lieferte `/.well-known/ard.json` die HTML-Seite, was Lighthouse als ungültiges JSON meldete
- **Geschwindigkeit der Website**: Das Stylesheet ist eingebettet (keine render-blockierende Anfrage), Screenshots gibt es in 480/800/1200 px für kleinere Bildschirme, und der GitHub-Stern-Button ist ein einfacher Link mit der beim Build abgerufenen Zahl statt eines Drittanbieter-Skripts

## [2.0.0] — 2026-11-11

Ein neues Aussehen für die ganze App, rund um einen echten Pokertisch, dazu eine Runde Sicherheit und Zuverlässigkeit im Backend.

### Highlights
- **Ein echter Pokertisch**: Jeder Spieler hat einen Platz, deine Karte fliegt auf den Tisch, und beim Aufdecken drehen sich die Karten nacheinander um
- **Ergebnisse, die das Gespräch leiten**: Verteilung, Durchschnitt, Median und ein nächster Schritt, wenn die Stimmen weit auseinander liegen
- **Wähle dein Deck beim Erstellen eines Spiels**, mit einer Vorschau der Karten
- **Unterstützung für Tastatur und Screenreader**: Tippe den Wert einer Karte, um abzustimmen; WCAG-AA-Kontrast im hellen und dunklen Modus
- **Fürs Handy gemacht**: Deine Karten bleiben am unteren Bildschirmrand, und der Tisch zeigt alle
- **Sicherer von Anfang an**: Die Host-Rolle und verdeckte Stimmen werden auf dem Server durchgesetzt, mit Anfragelimits und strengeren GraphQL-Grenzen

### Letzter Schliff
- **Header**: helle Leiste, die oben bleibt, mit dem Logo, dem Markennamen in der Display-Schrift und deinem Namen als Chip mit Initiale
- **Dialoge**: ein Stil für jeden Dialog (neues Spiel, Beitreten, neues Ticket, Import, Einladen, dein Name, Über): verschwommener Hintergrund, eine Zeile, die sagt, wofür der Dialog da ist, ein Hauptbutton in voller Breite
- **Dialog für ein neues Spiel**: Deck-Auswahl mit Mini-Karten, eine Live-Vorschau für ein eigenes Deck, die Beschreibung ist optional, und Enter im Namensfeld erstellt das Spiel
- **Version**: Der Footer zeigt die Version aus `package.json` (sie hing bei 1.2.1 fest)
- **Erkennung durch Agenten**: `/.well-known/ai-catalog.json` in App und Website, als JSON ausgeliefert (vorher kam die HTML-Seite zurück)
- **Website**: neue Screenshots, die Schriften und Farben der App, und „Neuigkeiten“ sowie die Version werden bei jedem Deploy aus diesem Änderungsprotokoll erzeugt

### Behoben
- Der Tastaturfokus-Rahmen auf einer Karte in deiner Hand wurde auf den flachen Button hinter der Karte gezeichnet; jetzt folgt er den abgerundeten Ecken der Karte und hebt sie an wie beim Hovern

### Neugestaltung — Phase 4 (Barrierefreiheit und Schliff)
- **Tastatur**: Die Hand ist ein einziger Tab-Stopp; Pfeiltasten, Pos1 und Ende wechseln zwischen den Karten, Enter/Leertaste stimmt ab. Den Wert einer Karte zu tippen, stimmt für sie ab („5“, „13“, „0.5“, „?“), wenn du nicht in einem Feld schreibst
- **Screenreader**: Ein Live-Bereich kündigt an, wer abgestimmt hat („Ana hat abgestimmt. 3 von 5 haben abgestimmt.“), eine neue Runde und das nächste Ticket; das Ergebnisfeld kündigt das Ergebnis an
- **Kontrast (WCAG AA)**: dunklerer gedämpfter Text, Farben für Konsens/Streuung/Gefahr im hellen Modus; dunkler Text auf hellem Violett im dunklen Modus (Panel-Köpfe, Chips); helleres Violett für Text-Buttons im dunklen Modus
- **Struktur**: ein einziger `main`-Landmark mit der Seitenüberschrift darin; das Benutzernamen-Steuerelement im Header ist ein Button (kein Button im Button); Panel-Köpfe verschachteln keinen Button mehr in einem Button und stellen `aria-expanded` bereit
- Die Timer-Steuerung wird nach dem Aufdecken ausgeblendet, statt ausgegraut angezeigt
- axe-core (WCAG 2 A/AA + Best Practices): 0 Verstöße auf der Startseite und im Spielraum, hell und dunkel, vor und nach dem Aufdecken

### Neugestaltung — Phase 3 (übrige Ansichten)
- **Startseite**: Ein Live-Demotisch spielt neben dem Einführungstext selbstständig Runden; „Beitreten“ nimmt einen eingefügten Spiellink oder -code direkt an (ohne Dialog), mit einem klaren Fehler für alles andere
- **Raum-Header**: Spielname und Beschreibung als Text, ein beschrifteter **Einladen**-Button (Link und QR-Code zusammen) und ein Menü für Link kopieren, PDF-Export und Verlassen
- **Ticketliste** ersetzt das Verlaufs-Akkordeon: Jedes Ticket zeigt seine aufgedeckten Stimmen, die Schätzung und (für den Host) einen Button „erneut abstimmen“
- **Timer-Ring**: Während der Timer des Hosts läuft, leert sich ein Ring um den Stimmenzähler auf dem Tisch und wird in den letzten 10 Sekunden orange
- **Handy**: Deine Karten bleiben am unteren Bildschirmrand und scrollen seitlich; das Spielerfeld ist ausgeblendet (der Tisch zeigt alle), und der Chat startet eingeklappt

### Behoben
- Ein Spiel mit mehreren Tickets neu zu laden, öffnete das erste Ticket und entfernte das Ticket aus der URL; jetzt bleibt es beim Ticket in der URL

### Neugestaltung — Phase 2 (der Tisch und das Aufdecken)
- **Pokertisch**: Jeder Spieler hat einen Platz am Rand eines Filztischs mit seiner Karte; der aktuelle Benutzer sitzt unten. In der Mitte ist ein Fortschrittsring („3/5 abgestimmt“) und nach dem Aufdecken der Durchschnitt
- **Kartenflug**: Die gewählte Karte fliegt aus deiner Hand auf deinen Platz (FLIP mit der Web Animations API); die Karten anderer Spieler fallen auf ihre Plätze
- **Gestaffeltes Aufdecken**: Die Karten drehen sich nacheinander rund um den Tisch um; die niedrigste und höchste Karte einer großen Streuung bekommen einen orangen Ring
- **Ergebnisfeld**: Verteilungsbalken, Durchschnitt, Median, Anzahl der Stimmen und ein nächster Schritt („Konsens“, „Fast Konsens“ oder „Bitte X und Y um eine Erklärung, dann stimmt erneut ab“)
- Nach dem Aufdecken ist die Hand gesperrt; „Vote Again“ löscht deine gewählte Karte
- „Reveal Votes“ nutzt das Markenviolett statt des grau-orangen Stils; das „(Avg: ?)“ neben dem Tickettitel ist weg (der Tisch zeigt es)

### Behoben
- Die Kartenvorderseiten schienen kurz durch die Rückseite, während eine Karte einflog (Deckkraft auf einem 3D-Element hat es abgeflacht)

### Neugestaltung — Phase 1 (Grundlage)
- **Neue visuelle Grundlage**: violett getönte Neutraltöne für beide Themes, ein verfeinertes Markenviolett und neue Rollen für das kommende Tischlayout (Filz, Kartenvorder-/-rückseite, Konsens, Streuung) in `styles/_variables.scss`
- **Typografie**: Bricolage Grotesque für Überschriften und Kartenzahlen, Figtree für Text, JetBrains Mono für Tasten und den Timer (Roboto entfernt)
- **Pokerkarten-Komponente** (`app-poker-card`): helle Vorderseite mit Eckindizes, gemusterte Rückseite, Dreh-Animation (gestaffelt beim Aufdecken), leerer Platz und eine Einflug-Animation, wenn eine Stimme ankommt
- **Deine Karten** heben sich beim Hovern und bleiben angehoben, wenn sie gewählt sind; die **Spielerliste** zeigt kleine Karten (leerer Platz / verdeckt / aufgedeckt) statt „?“-Kästchen
- **Pillen-Buttons** für alle Varianten der Material-Buttons; Bewegungs-Tokens (Dauern, Easing) und eine globale Regel für reduzierte Bewegung
- **Raum-Skelett**, während ein Spiel lädt, statt eines leeren Bildschirms

### Behoben
- Ein Link zu einem Spiel, das es nicht mehr gibt, zeigte einen leeren Raum und leitete auf `/game/undefined` weiter; jetzt steht da, dass das Spiel nicht mehr existiert

### Sicherheit (P1)
- **Anfragelimits, die wirklich greifen**: Token-Bucket pro Client-IP am Fusion-Gateway (die YARP-Richtlinie war nie angebunden) plus Limits pro Aktion für Registrierung, Spielerstellung, Berichte, Chat, Tickets, Stimmen und Beitritte
- **Container-Ports an 127.0.0.1 gebunden**: Jeder Dienst war auch direkt über einfaches HTTP erreichbar, an nginx und TLS vorbei
- **GraphQL-Grenzen**: maximale Tiefe 10, Zeitlimit für die Ausführung, 256 KB Anfragekörper; Introspection, Nitro-IDE und Ausnahmedetails außerhalb von Development abgeschaltet
- **Produktionsumgebung**: Die Dienste laufen mit `ASPNETCORE_ENVIRONMENT=Production`; `/_health` ist vor öffentlichem Zugriff verborgen
- **Validierung läuft**: FluentValidation-Validatoren waren registriert, wurden aber nie für GraphQL-Anfragen ausgeführt; ein MediatR-`ValidationBehavior` setzt sie jetzt durch. Deck-Einstellungen und Stimmen werden validiert
- **AutoMapper entfernt** (bekannte Schwachstelle mit hohem Schweregrad in 14.0.0), ersetzt durch expliziten Mapping-Code
- Eine einzige ausfallsichere Redis-Verbindung (`abortConnect=false`) statt zwei blockierender

### Behoben (P1)
- `deleteTicket` gab bei jedem Aufruf einen Mapping-Fehler zurück; jetzt liefert es `Boolean`
- Die Abfragen `game`, `ticket` und `settings` liefern `GameNotFound` / `TicketNotFound` / `GameSettingsNotFound` statt eines Null-Mapping-Fehlers

### Sicherheit
- **Host-Rolle auf dem Server**: Der Ersteller des Spiels ist der Host (`Game.hostId`); nur der Host kann Tickets erstellen, ändern und löschen, Einstellungen ändern, Stimmen aufdecken und eine neue Abstimmung starten
- **Mitgliedschaftsprüfung**: Nur Spieler, die einem Spiel beigetreten sind, können abstimmen, chatten oder den PDF-Bericht exportieren; anonyme Anfragen werden abgelehnt
- **Stimmen bleiben auf dem Server verdeckt**, bis der Host sie aufdeckt (Abfragen, Subscriptions, Durchschnitte und PDF-Bericht); eine neue Abstimmung verdeckt sie wieder
- **JWT-Secret aus dem Repository entfernt**: Es wird nur aus `Authentication__Secret` gelesen, mindestens 32 Zeichen, Issuer und Audience werden geprüft
- **CORS am Fusion-Gateway** auf konfigurierte Origins beschränkt (vorher: jeder Origin mit Anmeldedaten); Query-Pläne abgeschaltet
- **CI-Secrets zur Laufzeit**: Datenbankpasswort und JWT-Secret werden als Umgebungsvariablen an die Container übergeben, statt in die Images eingebaut zu werden

### Hinzugefügt
- Mutation `deleteGame` (nur Host): löscht ein Spiel samt Tickets, Stimmen und Spielern

### Behoben
- **Eine neue Abstimmung beginnt eine saubere Runde**: `reVoteTicket` löscht die bisherigen Stimmen des Tickets auf dem Server, sodass alte Stimmen nach „Vote Again“ nicht mehr zählen
- **Der Aufdeck-Zustand in der Oberfläche hängt nicht von der Reihenfolge ab**: Ein verspätetes Abfrageergebnis kann bereits aufgedeckte Stimmen nicht mehr verdecken; das tut nur eine neue Abstimmung
- **Der Gateway-Deploy nutzt das frisch zusammengesetzte Schema**: Die CI setzt jetzt die `gateway.fgp` zusammen, die das Docker-Image kopiert (mit fest gewählter Fusion-CLI-Version), statt einer ungenutzten Datei
- Einem Spiel per Link beizutreten, bevor die Identität des Benutzers feststand, überspringt den Beitritt nicht mehr
- `updateSettings` kann Einstellungen nicht mehr in ein anderes Spiel verschieben
- Chatnachrichten auf 1000 Zeichen begrenzt

## [1.2.1] — 2026-07-16

### Hinzugefügt
- **Neue Abstimmung wird bei allen Spielern synchronisiert**: Die neue GraphQL-Mutation `reVoteTicket` sendet die Reaktivierung eines Tickets über die Subscription `onTicketReVoted` an alle verbundenen Clients; alle Spieler sehen das neu abzustimmende Ticket, die Stimmen werden zurückgesetzt, die URL aktualisiert

### Geändert
- **Vote Again mit Re-vote vereinheitlicht**: Beide Buttons nutzen jetzt dieselbe Logik (URL-Aktualisierung über `replaceState`, Zurücksetzen der Stimmen, neues Binden der Subscriptions)
- Die Timer-Steuerung sieht nur der Host, mit einstellbarer Dauer (1–5 Minuten)
- Klügere Wartemeldung: unterscheidet „keine Tickets“ von „Host hat kein Ticket gewählt“
- Pfeile ermöglichen allen Spielern wieder das Blättern durch Tickets; Re-vote darf nur der Host

### Behoben
- **Stimmen aufdecken** wird jetzt bei allen Spielern synchronisiert: automatisch gewählte Tickets richten Stimmen-Subscriptions ein
- **Spiel beitreten** liest die Spiel-ID aus vollständigen URLs (z. B. `/game/UUID` oder `https://...`) und unterstützt UUIDs mit Bindestrichen
- **Die URL wird jetzt aktualisiert** beim Beitritt zu einem Spiel (`/game/:id`) oder beim Erstellen eines Tickets (`/game/:id/ticket/:ticketId`)
- **Flackern der Seite beseitigt**: `router.navigate` wurde für die Navigation im Spiel durch `location.replaceState` ersetzt
- **Leerer Spieler** tritt nicht mehr bei: `joinGame` ist mit einer Prüfung von `currentUserId` abgesichert
- **Automatische Ticketauswahl** beim Beitritt zu einem Spiel mit vorhandenen Tickets (kein „Warte auf den Host...“ mehr, wenn es Tickets gibt)
- **Ticketimport behält die Eingabereihenfolge**: Tickets werden jetzt nach ihrer ursprünglichen Reihenfolge im Import sortiert
- **Auswahl der Timer-Dauer** ist deaktiviert statt ausgeblendet, während der Timer läuft
- **SSR-Abstürze** behoben: Zugriff auf `document` beim Chat-Scrollen abgesichert, WebSocket-Client wird nur im Browser erstellt
- **`--restart unless-stopped`** für alle 8 Docker-Container in den CI-Pipelines ergänzt
- **Stimmen-Subscriptions** werden jetzt auf allen Wegen richtig eingerichtet (Host erstellt, Spieler tritt bei, neue Abstimmung)
- **Pfeile zur Ticketnavigation** sind für alle Spieler zum Blättern sichtbar; Re-vote im Verlauf ist auf den Host beschränkt

## [1.2.0] — 2026-07-15

### Architektur — Umstieg auf Standalone-Komponenten
- **Alle 26 Komponenten auf Standalone umgestellt**: 9 NgModule-Dateien entfernt (app, home, game, ticket, user, errors, shared, material, server)
- **Material-Importe pro Komponente**: Jede Komponente importiert nur die Material-Module, die sie nutzt (kein Sammel-`MaterialModule` mit 17 Modulen mehr)
- **`bootstrapApplication`** ersetzt `platformBrowserDynamic`: moderner Start von Angular
- **`provideAnimations()`** ersetzt `BrowserAnimationsModule` in der App-Konfiguration
- **`AppServerModule` gelöscht**: SSR nutzt jetzt den Standard-Bootstrap-Export (`main.server.ts`)

### Leistung — SSR und Optimierungen
- **SSR in der Produktion aktiviert**: Ein Node.js-Express-Server ersetzt nginx für statische Dateien
- **2 statische Routen vorab gerendert** (`/` und `/game`): nahezu sofortiger FCP durch vorab gerendertes HTML
- **Kritisches CSS eingebettet** (`inlineCritical: true`): Beasties extrahiert das CSS für den sichtbaren Bereich, kein render-blockierendes Stylesheet
- **Font Awesome entfernt**: ersetzt durch Material Icons und eingebettetes SVG für das GitHub-Symbol
- **CSS-Bundle um 63 % kleiner** (29 kB → 11 kB gzip)
- **Source Maps in der Produktion deaktiviert** (spart ~2–3 MB Download)
- **Build-Budgets verschärft**: Warnung bei 1,5 MB, Fehler bei 2,5 MB (vorher 5/10 MB)
- **Google Tag Manager verzögert** auf `requestIdleCallback`: nicht mehr im kritischen Pfad
- **Preconnect-Hinweise optimiert**: ungenutzte Origins entfernt, `crossorigin`-Attribute korrigiert
- **Umgebungsdateien eingeführt** (`environment.ts` / `environment.prod.ts`) mit `fileReplacements`
- **Abruf von `config.json` zur Laufzeit entfernt**: Konfiguration aus Umgebungsdateien zur Build-Zeit beseitigt einen blockierenden Netzwerkaufruf beim Start

### Leistung — Rendern der Startseite
- **Oberfläche der Startseite sofort sichtbar**: GameComponent wird im SSR-HTML gerendert, ohne auf die Registrierung des Benutzers zu warten
- **`setupUser()` verzögert** über `afterNextRender`: läuft nach der Hydration im Hintergrund
- **ThemeService für SSR gehärtet**: Zugriff auf `localStorage` mit `isPlatformBrowser` abgesichert
- **Alle Aufrufe von `localStorage` abgesichert** für SSR-Kompatibilität (GameComponent, HomeComponent, ThemeService)

### Korrekturen
- **Doppelte Meta-Tags** aus `index.html` entfernt (charset, viewport, theme-color)
- **Laden der Schriften**: Google Fonts und Material Icons laden jetzt asynchron (`preload` + `onload`)
- **Microsoft Clarity** ans Ende von `<head>` verschoben, damit es das Parsen nicht blockiert
- **Erlaubte Hosts** im Konstruktor von `CommonEngine` konfiguriert (SSR fiel auf CSR zurück)
- **SSR-Hydration-Abweichung** behoben: `afterNextRender` verhindert Unterschiede zwischen Client- und Server-DOM
- **Dockerfile aktualisiert**: Die Produktionsstufe nutzt `node:22-alpine` mit dem Express-SSR-Server
- **CI-Pipeline**: Das Einsetzen der Gateway-URL per `sed` wurde durch ein Node.js-Skript ersetzt (sicher mit URL-Zeichen) und dann durch Einsetzen über `env:`

### Inkompatible Änderungen
- **Port des Docker-Containers geändert**: Die App lauscht jetzt auf Port 4000 (vorher 80). Das Port-Mapping in Docker Compose ist aktualisiert (`10000:4000`).
- **`public/assets/config.json` gelöscht**: Gateway-URLs werden jetzt über `environment.prod.ts` konfiguriert
- **Alle NgModule-Dateien gelöscht**: Externe Importe dieser Module müssen angepasst werden

### Geändert
- **Abstimmungsverlauf neu gestaltet**: Der Tickettitel nimmt den Großteil des Felds ein, der AVG-Wert ist klein und rechtsbündig, Badge für den aktiven Chip
- **GitHub-Symbol** nutzt jetzt ein registriertes SVG über `MatIconRegistry` statt Font Awesome

## [1.1.0] — 2026-07-15

### Hinzugefügt
- **Tickets importieren**: Dialog zum Einfügen in großer Menge (eines pro Zeile) oder Hochladen einer Datei (CSV/JSON)
- **Footer-Komponente** mit Logo, Links, Theme-Umschalter, Über-Dialog und Versionsnummer
- **Pfeile zur Ticketnavigation** (◀ ▶), um ohne Neuladen der Seite zwischen Tickets zu wechseln
- **Ticketbeschreibung**: aufklappbares Feld unter dem Ticketnamen (mit mehreren Absätzen)
- **Neue Abstimmung über frühere Tickets** aus dem Abstimmungsverlauf mit dem Button `Re-vote this ticket`
- **Badge für das aktive Ticket** im Abstimmungsverlauf zeigt das aktuelle Ticket
- **Durchschnittlicher Abstimmungswert** neben dem Tickettitel (nur wenn es Stimmen gibt)
- **Links im Chat**: URLs werden automatisch zu anklickbaren Links (`linkify`)
- **Flüssige Übergänge**: Fade-/Slide-Animationen mit `@angular/animations`
- **Changelog-Seite auf der Website**: automatisch aus `CHANGELOG.md` mit `marked` erzeugt (zur Build-Zeit)
- **Bereich „Neuigkeiten“ auf der Website**: Vorschau des Änderungsprotokolls auf der Startseite mit Versions-Badges
- **Plattformübergreifendes Build-Skript** (`build.js`) für die Website
- **`llms.txt`** für KI-Agenten, die die Website lesen
- **`MatMenuModule`, `MatTabsModule`, `MatDividerModule`** zum Material-Modul hinzugefügt

### Geändert
- **Header neu gestaltet**: Hamburger-Menü links, kleinerer Markenname, Benutzername rechts
- **Begrüßung/Benutzername** aus einer eigenen Zeile in die Header-Leiste verschoben (spart vertikalen Platz)
- **Theme-Umschalter** ins Header-Menü verschoben (kein eigenes Symbol mehr)
- **Startseite** nutzt jetzt den Hero-CTA-Stil wie „Create Ticket“ (große Buttons)
- **Alle Spielaktionen** (New Ticket, Reveal Votes, Vote Again) unter den Abstimmungskarten zusammengefasst
- **Spieler- und Chatfeld** lassen sich jetzt mit einem flüssigen Übergang einklappen
- **Timer** immer sichtbar (nach dem Aufdecken der Stimmen deaktiviert/abgedunkelt)
- **Chatnachrichten**: eigene Nachrichten rechtsbündig mit violettem Hintergrund, andere linksbündig
- **Spielerliste**: aktueller Benutzer fett mit violett hervorgehobenem Hintergrund
- **Abstimmungsverlauf** erst nach dem ersten erstellten Ticket sichtbar
- **Feld zum Beitreten** nimmt die vollständige URL an (liest die Spiel-ID automatisch aus)
- **Benutzernamen bearbeiten**: Ein Klick auf den Benutzernamen oder das Bearbeiten-Symbol öffnet den Dialog zum Ändern
- **Ticketnavigation** nutzt `Location.replaceState` statt Routen-Navigation (kein Flackern)
- **Layout der Startseite** auf Flex umgebaut (kein vertikales Scrollen auf kurzen Seiten)
- **Website neu gestaltet**: weiches Scrollen, Einblend-Animationen, Anheben beim Hovern, einheitliche Navigation und Footer
- **Textkontrast im Footer** verbessert (`text-muted` → `text-secondary`) für die Barrierefreiheit
- **Footer-Logo** auf ein PNG in 64x64 für Retina-Displays umgestellt
- **Navigation der Website**: Header mit Backdrop-Blur, Verlaufsrand oben am Bereich „Neuigkeiten“

### Behoben
- Eingeklappter Chat zeigte einen weißen Hintergrund (HostBinding- und CSS-Korrektur)
- Chatnachrichten scrollten nicht (max-height-Begrenzung und Korrekturen der Flex-Kette)
- Chattext im dunklen Modus unlesbar (dunkle Werte für primary-50/100/200 ergänzt)
- Mobile Seitenleiste überdeckte den Abstimmungsverlauf (Korrekturen beim responsiven Überlauf)
- Stimmenzahl zeigte beim Ticketwechsel falsche Werte (Datenleck behoben)
- 404 bei der Navigation zur neuen Abstimmung (fehlendes Pfadsegment `/ticket/`)
- Leeres div für Spieleraktionen, wenn keine Buttons angezeigt werden
- Doppelter Rand im Footer verursachte vertikales Scrollen
- Geringer Kontrast des ausgewählten Zustands von `mat-option` (globale CSS-Anpassung)
- Fest codierte Farben in der Komponente zum Erstellen eines Spiels durch Design-Tokens ersetzt
- **Lighthouse-Optimierungen**: charset zuerst in `<head>`, nicht blockierendes CSS, Bildabmessungen, Preconnects
- **Seitenverhältnis des Hero-Bilds** auf die natürlichen Maße korrigiert (1064×832)
- **Social-Media-Links** fehlten `aria-label`-Attribute (Barrierefreiheit)
- **404 auf der Changelog-Seite**: wird jetzt als `/changelog/index.html` für die statische Auslieferung über nginx gebaut
- **CI-Build des Changelogs**: kopiert `CHANGELOG.md` vor dem Docker-Build in das Verzeichnis der Website

## [1.0.0] — 2026-07-14

> Erstes stabiles Release. Vorher gab es die Anwendung als **Beta [0.0.1]**, die
> die meisten der gleichen Funktionen hatte, aber instabil war, eine unfertige Oberfläche hatte und
> nicht für die Produktion gehärtet war.

### Hinzugefügt
- **Microservice-Architektur** mit GraphQL-Föderation
  - API Gateway (YARP Reverse Proxy)
  - GraphQL Fusion Gateway (HotChocolate)
  - Game Engine (Spielräume, Tickets, Abstimmung, PDF-Berichte)
  - User Management (anonyme Benutzernamen, JWT-Authentifizierung, auf Redis)
- **Angular-19-Web-App** mit:
  - Abstimmung in Echtzeit über GraphQL-WebSocket-Subscriptions
  - Mechanismus für verdeckte Stimmen (Stimmen bleiben verborgen, bis „Reveal“ geklickt wird)
  - Serverseitigem Rendern (SSR) mit Vorab-Rendern für `/` und `/game`
  - PWA-Unterstützung mit Service Worker und installierbarem Manifest
  - Apollo-Angular-GraphQL-Client mit typisiertem Codegen
  - Angular-Material-Oberfläche mit dem Theme azure-blue
- **Marketing-Website** mit Tailwind CSS + Flowbite
- **SEO-Optimierungen:**
  - Vollständige Meta-Tags (OG, Twitter, canonical, hreflang)
  - Strukturierte JSON-LD-Daten (WebSite, SoftwareApplication, FAQPage, WebApplication, BreadcrumbList)
  - robots.txt und sitemap.xml für beide Domains
  - Dynamische Seitentitel und Meta-Beschreibungen pro Route
  - Semantische HTML-Landmarks und ein Link zum Überspringen zum Inhalt
  - Dynamischer Sitemap-Endpunkt in der Game Engine
- **CI/CD-Pipeline** (GitHub Actions):
  - Mehrstufiger Deploy-Workflow mit Reihenfolge der Abhängigkeiten
  - Docker-Images, die in die GitHub Container Registry (ghcr.io) gepusht werden
  - SSH-Deploy auf die Produktionsserver
  - Eigene Workflows für die Infrastruktur (PostgreSQL, Redis, Monitoring)
- **Monitoring-Stack** (OpenTelemetry, Prometheus, Loki, Grafana)
- **Anfragelimit** am API Gateway (100 Anfragen/Min., festes Fenster)
- **Sicherheits-Header** in allen Nginx-Konfigurationen (CSP, HSTS, X-Frame-Options usw.)
- **Nginx Reverse Proxy** mit SSL-Terminierung (Let's Encrypt)
- Aufgeteilte Infrastruktur: öffentlicher Anwendungsserver + privater Datenbankserver
- Zentrale .NET-Paketverwaltung (`Directory.Packages.props`)
- Clean Architecture in allen Backend-Diensten
- QuestPDF für die Erstellung von PDF-Spielberichten
- EF Core mit PostgreSQL (Code-First-Migrationen)
- Redis-Caching (HybridCache + StackExchange.Redis)
- Gemeinsames NuGet-Paket (`OpenPlanningPoker.Shared`), veröffentlicht auf NuGet.org
- Health Checks mit UI-Response-Writer
- Download des PDF-Spielberichts
- Eigene 404-Fehlerseiten
- Integration von Microsoft Clarity Analytics (ID: `xmim6e317s`)
- Brotkrümelnavigation mit strukturierten Daten `BreadcrumbList`
- Ausführliche Dokumentation (`docs/`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`)

### Geändert
- Node.js-Docker-Images auf 22 LTS aktualisiert (von 23 ohne LTS und 18 ohne Support)
- `apollo-client` v2 durch `@apollo/client` v3 ersetzt
- BreadcrumbComponent aus GameModule exportiert, zur Nutzung in HomeComponent
- Brotkrümelnavigation im Home-Layout über den Spielernamen verschoben
- Tailwind-Primärpalette an das Markenviolett angepasst (#7e3af2)

### Behoben
- **KRITISCH: `Database.Migrate()` war auskommentiert**: Die Datenbank wurde bei einem frischen Deploy nie angelegt
- Doppeltes COPY im Dockerfile der Website (Sicherheit: der Quellcode wurde ins Produktions-Image kopiert)
- Reihenfolge der CORS-Middleware (läuft jetzt vor dem GraphQL-Endpunkt)
- Fehleranfälliges Parsen des Bearer-Tokens in `HeaderPropagationHandler`
- Tippfehler `ApplicationErrorContants` → `ApplicationErrorConstants`
- Strukturierte FAQ-Daten passten nicht zum sichtbaren HTML-Inhalt
- `site.webmanifest` hatte leere Felder `name` und `short_name`
- Tippfehler in der Twitter-Karte: `@openplanninpoker` → `@openplanningpoker`
- Port des User Management im nginx-Proxy (9091 → 9090)
- Copyright-Jahr im Footer auf 2026 aktualisiert
- Doppelte Anzeige von Spielern am Tisch (Race Condition in `getGamePlayers`)
- Breite der verdeckten Karte stimmte nicht (`box-sizing: border-box` ergänzt)
- Die Eingabetaste sendet jetzt das Formular zum Erstellen eines Spiels
- Der Abstimmungsverlauf beachtet jetzt den Zustand der verdeckten Stimmen
- Pfade in `generate-schema.ps1` in beiden Projekten korrigiert
- GraphQL-Projekt der Game Engine: `Domain.Games` zu GlobalUsings hinzugefügt

## [0.0.1] — 2026-06 (Beta)

> Erstes Beta-Release. Die Anwendung funktionierte von Anfang bis Ende, hatte aber
> raue Kanten in der Oberfläche, fehlenden Schliff und Stabilitätsprobleme, die in v1.0.0 behoben wurden.

### Hinzugefügt
- Einfaches Erstellen von Spielräumen und Beitreten
- Abstimmung in Echtzeit mit GraphQL-Subscriptions
- Erstellen und Verwalten von Tickets
- Mechanismus zum Aufdecken der Stimmen
- Erstellung von PDF-Spielberichten
- Angular-Material-Oberfläche (vor der Neugestaltung)
- Erste Microservice-Einrichtung (Game Engine, User Management, API Gateway)
- Docker Compose für die lokale Entwicklung
- Einfache nginx-Konfiguration
