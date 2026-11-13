# Spisak izmena

Sve važne izmene u Open Planning Pokeru su zapisane u ovom fajlu. Ovo je prevod engleskog `CHANGELOG.md`; svako izdanje mora postojati u oba.

Format prati [Keep a Changelog](https://keepachangelog.com/sr/1.0.0/), a verzije [semantičko verzionisanje](https://semver.org/lang/sr/).

## [Neobjavljeno]

## [3.0.0] — Unreleased

Svaki deo Open Planning Pokera na aktuelnim, podržanim verzijama. Za igrače se ništa ne menja; za self-hosting trebaju nova okruženja.

### Izmenjeno
- **Backend na .NET 10 LTS**: svi servisi i Docker imidži, uz EF Core 10, Npgsql 10, Microsoft.Extensions 10, FluentValidation 12, OpenTelemetry 1.21, QuestPDF 2026.9 i Serilog 10
- **Aplikacija na Angularu 21** (sa 19) sa Angular Material 21, ugrađenim control flowom u svim šablonima, Apollo Client 4 uz apollo-angular 14, GraphQL 16, Express 5 za renderovanje na serveru i zone.js 0.16
- **Sajt na Tailwind CSS 4**; nekorišćeni paket Flowbite je uklonjen. Stranice izgledaju isto, a broj GitHub zvezdica sada ima razdelnu liniju

### Uklonjeno
- Nekorišćeni paketi: Duende.AccessTokenManagement, Entity Framework 6 i AutoMapper

### Za developere
- Za self-hosting treba .NET 10 runtime (Docker imidži ga sadrže) i Node.js 22.12 ili noviji za aplikaciju
- Namerno zadržano: **HotChocolate i Fusion 15** (Fusion 16 koristi nov format gatewaya i sastavljanja, to je posebna migracija), **Angular 22** (njegov CLI traži Node.js 22.22 / 24.15, to je sledeći korak), **MediatR 12** (novije verzije traže komercijalnu licencu), **xUnit 2** (xUnit v3 je nov model testova), **TypeScript 5.9** (Angular 21 ne podržava TypeScript 7) i deljeni NuGet paket na .NET 9 (servisi na .NET 10 ga koriste takav kakav je)

## [2.4.1] — Unreleased

Aplikacija staje na telefon, i dva nova vodiča o igri.

### Dodato
- **Novi vodiči**: „Kako se koristi Open Planning Poker”, obilazak cele igre (sobe, uloge, glasanje, otkrivanje, konačna procena, izveštaji, asinhrono glasanje i timske sobe), i „Asinhroni planning poker”, o proceni pre sastanka. Oba na engleskom, srpskom i nemačkom, i na početnoj strani

### Ispravljeno
- **Telefoni**: header više ne seče tvoje ime (ispod 420 px čip prikazuje inicijal), a linkovi u footeru prelaze u novi red umesto da izađu van ekrana. Nema skrolovanja u stranu na 360–390 px
- **Dijalog za novu igru**: dugme Napravi igru ostaje vidljivo pri dnu dok skroluješ kroz špilove


## [2.4.0] — Unreleased

Vodiči i spisak izmena na srpskom i nemačkom, i deploy bez lozinki.

### Dodato
- **Vodiči i spisak izmena na srpskom i nemačkom**: sva četiri vodiča i spisak izmena na `/sr/` i `/de/`, povezani preko `hreflang`. Izbor jezika u footeru ostaje na istoj stranici, a sitemap sadrži svaku stranicu na svakom jeziku
- **Poruke User Managementa na tvom jeziku**: greške za korisničko ime, previše zahteva i nepoznatog korisnika prate jezik aplikacije, kao i poruke Game Enginea

### Izmenjeno
- „Šta je novo” na srpskoj i nemačkoj početnoj strani dolazi iz prevedenog spiska izmena, sa datumom na jeziku stranice

### Ispravljeno
- Na `/sr/` i `/de/` link „English” u footeru je vodio na istu prevedenu stranicu

### Bezbednost
- Deploy se na servere prijavljuje posebnim SSH ključem umesto lozinkom; `appleboy/scp-action` 1.0.0

### Za developere
- Prevedeni spiskovi izmena su u `frontend/open-planning-poker-website/i18n/<lang>/CHANGELOG.md`: svako izdanje iz `CHANGELOG.md` mora da se doda i tu u istom PR-u, inače build sajta pada


## [2.3.1] — 2026-11-13

### Izmenjeno
- Footer sajta više ne vodi na vodiče „Šta je planning poker” i „Karte i skale”; i dalje su pod Vodičima u meniju i na početnoj strani

## [2.3.0] — 2026-11-13

Open Planning Poker na tvom jeziku, aplikacija koja se instalira na telefon i svaki pull request testiran od početka do kraja.

### Dodato
- **Srpski i nemački**: aplikacija je dostupna na engleskom, srpskom (latinica) i nemačkom. Prati jezik browsera, a možeš ga promeniti u meniju; izbor se pamti. Prevedeni su i datumi, CSV izvoz, poruke o greškama sa servera i PDF izveštaj
- **Sajt na srpskom i nemačkom**: početna strana na `/sr/` i `/de/`, povezana preko `hreflang` i navedena u sitemapu, sa izborom jezika u footeru
- **Aplikacija koja se instalira**: instaliraj Open Planning Poker na telefon ili računar. Aplikacija se otvara i bez interneta i prikazuje obaveštenje „Nisi na mreži” dok se veza ne vrati. Ponuda za instalaciju se pojavljuje najviše jednom nedeljno, a „Instaliraj aplikaciju” je u meniju
- **Ponuda za ponovno učitavanje** kad je spremna nova verzija aplikacije

### Izmenjeno
- Stranice se renderuju na serveru na jeziku svakog posetioca, umesto da se unapred generišu
- Meni sajta se prikazuje tek na širim ekranima, da bi duži nazivi stali

### Za developere
- Integracioni testovi za Game Engine nad pravim PostgreSQL-om i Redisom (Testcontainers): akcije koje sme samo host, skriveni glasovi, ponovno glasanje, tajmer, pristup četu, prevedene greške i PDF izveštaj
- Playwright end-to-end testovi nad celim sistemom (`docker-compose.e2e.yml`): pravljenje igre, ulazak drugog igrača, dodavanje i uvoz tiketa, glasanje, otkrivanje, ponovno glasanje, zajednički tajmer i PDF izvoz, uz axe provere pristupačnosti na računaru i telefonu
- CI workflow za svaki pull request: unit i integracioni testovi backenda, provera ranjivih paketa, build aplikacije i sajta, provera prevoda, end-to-end testovi i Lighthouse budžeti (performanse ≥ 90, pristupačnost 100, dobre prakse ≥ 95, SEO ≥ 92, uz posebnu proveru svakog SEO audita)

## [2.2.1] — 2026-11-13

### Bezbednost
- Bezbednosno ojačanje četa i WebSocket veze; čet se učitava posle ulaska u igru
- Bezbednosna ažuriranja zavisnosti: HotChocolate 15.1 u svim servisima, OpenTelemetry 1.19 u deljenom paketu

### Ispravljeno
- Prebacivanje igrača u posmatrača ili postavljanje za co-hosta padalo je sa greškom na serveru (igrači učitani iz baze gubili su id, pa izmena nije pogađala nijedan red)
- Renderovanje stranica igre na serveru više ne otvara WebSocket pretplate (Node 22 ima globalni `WebSocket`), što je moglo da sruši server aplikacije

### Izmenjeno
- Deljeni paket `OpenPlanningPoker.Shared` 1.1.0 više ne zavisi od prerelease paketa; uklonjeni su nekorišćeni Console, Prometheus i Zipkin exporteri, HTTP instrumentacija i metrike procesa

## [2.2.0] — 2026-11-13

Vodite sobu zajedno, procenjujte pre sastanka i čuvajte istoriju tima na jednom mestu.

### Dodato
- **Posmatrači**: uđi u igru samo da gledaš. Posmatrači nemaju karte, ne računaju se u „x/y glasalo” i ne mogu da glasaju ni na serveru. Menja se u meniju pored tvog imena
- **Co-hostovi i predaja igre**: host može da napravi igrače co-hostovima (dodaju tikete, otkrivaju karte, pokreću tajmer i menjaju opcije sobe) ili da nekog drugog postavi za hosta. Host i co-hostovi mogu da uklone igrača; njegovi glasovi ostaju na tiketima
- **Konačna procena**: posle otkrivanja host bira kartu oko koje se tim složio (najčešća karta je označena). Prikazuje se u listi tiketa i koristi u PDF izveštaju, CSV izvozu i velocity-ju tima
- **Automatsko otkrivanje**: opcija sobe koja okreće karte čim svi igrači (ne posmatrači) glasaju
- **Izmena, brisanje i redosled tiketa**: iz menija u listi tiketa, ili prevlačenjem tiketa. Opisi tiketa podržavaju mali deo Markdowna (podebljano, kurziv, kod, liste, linkovi)
- **CSV izvoz** rezultata, pored PDF izveštaja
- **Asinhrono glasanje**: host otvara period za glasanje (24 sata, 3 dana ili nedelju dana). Svako sam glasa za sve tikete; glasovi ostaju skriveni dok host na sastanku ne otkrije svaki tiket
- **Timske sobe** (`/team`): stalna soba za tim sa linkom za poziv, članovima, sesijama planiranja sprinta, istorijom sprintova i velocity grafikonom (poeni po sprintu, prosek poslednja tri sprinta, uz prikaz u tabeli)
- **Podrži projekat**: dugme „Buy me a coffee” (Ko-fi) u headeru i footeru aplikacije i na sajtu, i GitHub Sponsors u meniju aplikacije, About prozoru, delu sajta za podršku i README-u (`.github/FUNDING.yml`). Klikovi se broje kao GA4 događaj `donate_click` (platforma i mesto, bez ličnih podataka). About prozor i sajt predstavljaju autora

### Izmenjeno
- Igrači u sobi dobijaju izmene uživo kad neko promeni ulogu, postane co-host, izađe ili bude uklonjen, i kad se promene tiketi ili opcije sobe (`onGameChanged`)
- Brisanje igre sme samo host (ne i co-hostovi)
- Baza: nove kolone za hosta, uloge, redosled tiketa, konačnu procenu i opcije sobe, i tabele za timske sobe. Postojeće igre zadržavaju autora kao hosta, a tiketi svoj redosled

## [2.1.1] — 2026-11-13

Novi izgled PDF izveštaja, uz čiste CI runove i buildove.

### Izmenjeno
- **Novi izgled PDF izveštaja**, usklađen sa aplikacijom i sajtom: logo i ljubičasta boja brenda, fontovi Figtree i Bricolage Grotesque (pun skup znakova, pa se imena kao „Đorđe Čolić” ispravno štampaju), pločice sa sažetkom (tiketi, otkriveno, igrači, prosečna procena) i po jedna kartica za svaki tiket sa procenom, prosekom, medijanom, oznakom konsenzus / blizu / velika razlika, raspodelom glasova i ko je za šta glasao. Brojevi strana i link na openplanningpoker.com u footeru
- Procena na svakom tiketu je najčešća karta (kod nerešenog ide karta najbliža medijani), umesto običnog proseka
- QuestPDF 2025.1.7 → 2025.7.4 (zaobljeni uglovi)
- **GitHub Actions na Node 24**: `actions/checkout` v7.0.1 i `actions/setup-dotnet` v6.1.0 (stare verzije su radile na zastarelom Node 20)
- **Runneri fiksirani na `ubuntu-24.04`** umesto `ubuntu-latest`, da prelazak `ubuntu-latest` na Ubuntu 26 ne promeni build bez najave

### Ispravljeno
- **Glasovi na tiketu su mogli da padnu sa null referencom** kad bi upit za glasove vratio grešku; mapiranje sada vraća praznu listu
- **Upozorenja u buildu**: nullable upozorenja u GraphQL mapiranjima, nedostajuća XML dokumentacija za JWT podešavanje i dve verzije `Microsoft.EntityFrameworkCore.Relational` u Game Engineu (sada 9.0.3 svuda). Game Engine, User Management i Fusion gateway se builduju bez upozorenja
- **Kvačice na sajtu**: ikone u listi na vrhu strane („Bez registracije”, „Radi na telefonu”, ...) bile su 2px ispod teksta; sada su centrirane u redu pri svakoj veličini fonta

## [2.1.0] — 2026-11-12

Zajednički tajmer za ceo tim, brže prvo učitavanje i vodiči na sajtu.

### Najvažnije
- **Zajednički tajmer**: host ga pokreće, svi vide isto odbrojavanje, a karte se otkrivaju kad istekne
- **Brže**: oko 25% manje JavaScripta pri prvom učitavanju, fontovi sa našeg domena, soba za igru se učitava tek kad je igra otvorena
- **Akcije sobe su uvek vidljive**: kopiranje linka, PDF izvoz i izlazak su dugmad pored Pozovi, umesto menija
- **Vodiči na sajtu**: šta je planning poker, kako se vodi sesija, skale karata i self-hosting
- **Pretraga i AI agenti**: privatne sobe se više ne indeksiraju, prave 404 stranice i ARD manifest (`/.well-known/ard.json`) za AI agente

### Dodato
- **Zajednički tajmer za glasanje**: hostov tajmer sada radi na serveru, pa svaki igrač vidi isto odbrojavanje (i prsten na stolu), i oni koji uđu dok tajmer traje. Nove mutacije `startTimer` / `stopTimer` (samo host), upit `gameTimer` i pretplata `onTimerChanged`; stanje se čuva u Redisu. Kad dođe do nule, karte se otkrivaju, kao i ranije

### Izmenjeno
- **Kontrola tajmera**: jedna pilula sa vremenom, čipovi za trajanje (30s, 1m, 2m, 3m, 5m) i Start/Stop, umesto padajuće liste i tri dugmeta sa ikonama
- Veliko slovo samo na početku za preostale natpise: „Reveal cards”, „Vote again”, „New ticket”, „Chat”, „Paste a list”, „Upload a file”, „T-shirt sizes”
- **Header sobe**: „Kopiraj link”, „Izvezi PDF” i „Izađi” su uvek vidljivi pored „Pozovi” (na telefonu samo ikone); meni ⋮ je uklonjen
- **Opis tiketa** se podrazumevano prikazuje ispod naziva tiketa; dugme za info ga sakriva
- **„Modified Fibonacci” je sada „Fibonacci v2”**, uz objašnjenje u tooltipu
- **Putanja (breadcrumb)** je poravnata sa headerom na širokim ekranima

### Projekat
- **Izdanja**: kad verzija u `package.json` još nema tag, workflow pravi tag `vX.Y.Z` i objavljuje GitHub Release sa delom CHANGELOG-a za tu verziju
- **Dependabot**: nedeljna grupisana ažuriranja za npm (aplikacija, sajt), NuGet (svi servisi), Docker imidže i GitHub Actions
- **Provera dostupnosti** na svakih 15 minuta (sajt, changelog, aplikacija, ai-catalog, GraphQL gateway); kvar otvara GitHub issue sa oznakom `uptime`, a oporavak ga zatvara
- Build izlaz sajta (`dist/`) i četiri nekorišćene slike više nisu u repozitorijumu

### Performanse
- **Manje prvo učitavanje**: početni JavaScript 1148 → 857 KB (nekompresovano). Soba za igru je poseban deo koji se učitava kad se igra otvori (`@defer`); dijalozi (nova igra, ulazak, tiket, uvoz, poziv, korisničko ime, about) se učitavaju pri prvom otvaranju; engine za animacije se učitava posle pokretanja (`provideAnimationsAsync`)
- **Demo sto se hidrira kad je browser slobodan**: i dalje se renderuje na serveru, a njegov JS se učitava kad browser nema posla (inkrementalna hidracija)
- **Ikone**: preuzima se samo 25 Material Symbols ikona koje aplikacija koristi, umesto celog Material Icons fonta
- **Globalni CSS** 168 → 74 KB: Material teme samo za komponente koje aplikacija koristi
- **Analitika** (GA4, Clarity) se učitava posle stranice, kad je browser slobodan; skoro sva prinudna čitanja layouta na početnoj strani dolazila su od Clarity-ja
- **Fontovi sa našeg domena** (aplikacija i sajt): bez zahteva ka Google Fonts, latin + latin-ext podskupovi, Bricolage Grotesque fiksiran na optičku veličinu 48 (42 KB umesto 77 KB), unapred učitan
- **Aplikacija direktno poziva Fusion gateway**; YARP API gateway je uklonjen (jedan mrežni skok manje za svaki zahtev)
- **Keširanje sajta**: stylesheet ima hash sadržaja u imenu, pa novo izdanje nikad nije sakriveno iza keša od godinu dana; slike se keširaju nedelju dana

### SEO
- **Vodiči na sajtu**: četiri nove stranice (šta je planning poker, kako se vodi sesija, karte i skale, self-hosting) sa strukturiranim podacima Article, FAQ, HowTo i breadcrumb, povezane sa početne strane, iz headera i footera
- **Prave 404 stranice** na sajtu i u aplikaciji (obe su na nepoznate adrese odgovarale sa 200 i početnom stranom); 404 stranica sajta koristi dizajn sajta
- **Aplikacija**: sobe i tiketi su `noindex` (privatni su i kratko traju); `/game` kao canonical upućuje na `/`; naslov početne strane je `h1` stranice; novi opis i kartica za društvene mreže 1200×630
- Sitemap sa vodičima i snimcima ekrana; `robots.txt` bez `Crawl-delay`; `llms.txt` za sajt
- Zajednički head, header i footer delovi za svaku stranicu sajta
- **AI agenti**: ARD manifest se služi na `/.well-known/ard.json` (lokacija koju koriste aktuelna specifikacija i Lighthouse), najavljen sa `<link rel="ard">` i redom `Agentmap:` u `robots.txt`; `ai-catalog.json` ostaje za starije alate. Ranije je `/.well-known/ard.json` vraćao HTML stranicu, što je Lighthouse prijavljivao kao neispravan JSON
- **Brzina sajta**: stylesheet je ugrađen u stranicu (bez zahteva koji blokira renderovanje), snimci ekrana imaju veličine 480/800/1200 px za manje ekrane, a GitHub dugme za zvezdicu je običan link sa brojem preuzetim pri buildu, umesto skripte treće strane

## [2.0.0] — 2026-11-11

Novi izgled cele aplikacije, napravljen oko pravog pokerskog stola, uz bezbednosno i stabilnosno sređivanje backenda.

### Najvažnije
- **Pravi pokerski sto**: svaki igrač ima mesto, tvoja karta leti na sto, a pri otkrivanju se karte okreću jedna za drugom
- **Rezultati koji vode razgovor**: raspodela, prosek, medijana i sledeći korak kad su glasovi daleko jedan od drugog
- **Biraš špil kad praviš igru**, uz pregled karata
- **Podrška za tastaturu i čitače ekrana**: ukucaj vrednost karte da glasaš; WCAG AA kontrast u svetlom i tamnom režimu
- **Napravljeno za telefone**: tvoje karte su zalepljene za dno ekrana, a sto prikazuje sve
- **Bezbednije od starta**: uloga hosta i skriveni glasovi se proveravaju na serveru, uz ograničenja broja zahteva i strože GraphQL limite

### Završno doterivanje
- **Header**: svetla traka koja ostaje na vrhu, sa logom, nazivom u display fontu i tvojim imenom kao čipom sa inicijalom
- **Dijalozi**: jedan stil za svaki dijalog (nova igra, ulazak, novi tiket, uvoz, poziv, tvoje ime, about): zamućena pozadina, red koji kaže čemu dijalog služi, glavno dugme pune širine
- **Dijalog za novu igru**: izbor špila sa malim kartama, pregled uživo za sopstveni špil, opis nije obavezan, a Enter u polju za ime pravi igru
- **Verzija**: footer prikazuje verziju iz `package.json` (bila je zaglavljena na 1.2.1)
- **Otkrivanje za agente**: `/.well-known/ai-catalog.json` u aplikaciji i na sajtu, služen kao JSON (ranije je vraćao HTML stranicu)
- **Sajt**: novi snimci ekrana, fontovi i boje aplikacije, a „Šta je novo” i verzija se generišu iz ovog spiska izmena pri svakom deployu

### Ispravljeno
- Okvir fokusa tastature na karti u tvojoj ruci crtao se na ravnom dugmetu iza karte; sada prati zaobljene uglove karte i podiže je kao pri hoveru

### Redizajn — faza 4 (pristupačnost i doterivanje)
- **Tastatura**: ruka je jedan Tab korak; strelice, Home i End pomeraju između karata, a Enter/Space glasa. Kucanje vrednosti karte glasa za nju („5”, „13”, „0.5”, „?”) kad ne kucaš u polju
- **Čitači ekrana**: live region najavljuje ko je glasao („Ana je glasala. Glasalo 3 od 5.”), novu rundu i sledeći tiket; panel sa rezultatima najavljuje ishod
- **Kontrast (WCAG AA)**: tamniji prigušeni tekst, boje za konsenzus/razliku/opasnost u svetlom režimu; tamni tekst na svetloljubičastoj u tamnom režimu (zaglavlja panela, čipovi); svetlija ljubičasta za tekstualna dugmad u tamnom režimu
- **Struktura**: jedan `main` landmark sa naslovom stranice unutra; kontrola korisničkog imena u headeru je jedno dugme (bez dugmeta u dugmetu); zaglavlja panela više nemaju dugme u dugmetu i izlažu `aria-expanded`
- Kontrole tajmera se posle otkrivanja sakrivaju, umesto da budu posivljene
- axe-core (WCAG 2 A/AA + dobre prakse): 0 prekršaja na početnoj strani i u sobi za igru, u svetlom i tamnom režimu, pre i posle otkrivanja

### Redizajn — faza 3 (ostali ekrani)
- **Početna strana**: demo sto uživo sam igra runde pored opisa; „Uđi” prihvata nalepljen link ili kod igre direktno (bez dijaloga), uz jasnu grešku za sve ostalo
- **Header sobe**: naziv i opis igre kao tekst, dugme **Pozovi** sa natpisom (link i QR kod zajedno) i meni za kopiranje linka, PDF izvoz i izlazak
- **Lista tiketa** zamenjuje harmoniku sa istorijom: svaki tiket prikazuje otkrivene glasove, procenu i (za hosta) dugme „glasaj ponovo”
- **Prsten tajmera**: dok hostov tajmer radi, prsten se prazni oko brojača glasova na stolu i postaje narandžast u poslednjih 10 sekundi
- **Telefon**: tvoje karte su zalepljene za dno ekrana i skroluju se u stranu; panel sa igračima je sakriven (sto prikazuje sve), a čet počinje skupljen

### Ispravljeno
- Ponovno učitavanje igre sa više tiketa otvaralo je prvi tiket i brisalo tiket iz adrese; sada ostaje na tiketu iz adrese

### Redizajn — faza 2 (sto i otkrivanje)
- **Pokerski sto**: svaki igrač ima mesto na ivici stola od čoje, sa svojom kartom; trenutni korisnik sedi na dnu. Na sredini je prsten napretka („3/5 glasalo”) i, posle otkrivanja, prosek
- **Let karte**: izabrana karta leti iz tvoje ruke na tvoje mesto (FLIP preko Web Animations API-ja); karte drugih igrača padaju na njihova mesta
- **Otkrivanje redom**: karte se okreću jedna za drugom oko stola; najniža i najviša karta kod velike razlike dobijaju narandžasti prsten
- **Panel sa rezultatima**: trake raspodele, prosek, medijana, broj glasova i sledeći korak („Konsenzus”, „Blizu konsenzusa” ili „Zamoli X i Y da objasne, pa glasajte ponovo”)
- Posle otkrivanja ruka je zaključana; „Vote Again” briše tvoju izabranu kartu
- „Reveal Votes” koristi ljubičastu boju brenda umesto sivo-narandžastog stila; „(Avg: ?)” pored naslova tiketa je uklonjen (sto ga prikazuje)

### Ispravljeno
- Lica karata su se nakratko videla kroz poleđinu dok je karta padala (prozirnost na 3D elementu ga je spljoštila)

### Redizajn — faza 1 (osnova)
- **Nova vizuelna osnova**: neutralne boje sa ljubičastim tonom za obe teme, doterana ljubičasta brenda i nove uloge za raspored sa stolom koji dolazi (čoja, lice/poleđina karte, konsenzus, razlika) u `styles/_variables.scss`
- **Tipografija**: Bricolage Grotesque za naslove i brojeve na kartama, Figtree za tekst, JetBrains Mono za tastere i tajmer (Roboto uklonjen)
- **Komponenta pokerske karte** (`app-poker-card`): svetlo lice sa oznakama u uglovima, poleđina sa šarom, animacija okretanja (redom pri otkrivanju), prazno mesto i animacija pada kad stigne glas
- **Tvoje karte** se podižu na hover i ostaju podignute kad su izabrane; **lista igrača** prikazuje male karte (prazno mesto / licem nadole / otkrivena) umesto kutija sa „?”
- **Dugmad u obliku pilule** za sve varijante Material dugmadi; tokeni za pokret (trajanja, easing) i globalno pravilo za smanjen pokret
- **Kostur sobe** dok se igra učitava, umesto praznog ekrana

### Ispravljeno
- Otvaranje linka ka igri koja više ne postoji prikazivalo je praznu sobu i preusmeravalo na `/game/undefined`; sada piše da igra više ne postoji

### Bezbednost (P1)
- **Ograničenje broja zahteva koje zaista radi**: token bucket po IP adresi klijenta na Fusion gatewayu (YARP politika nikad nije bila povezana) plus ograničenja po akciji za registraciju, pravljenje igre, izveštaje, čet, tikete, glasove i ulazak
- **Portovi kontejnera vezani za 127.0.0.1**: svaki servis je bio dostupan i direktno preko običnog HTTP-a, mimo nginxa i TLS-a
- **GraphQL ograničenja**: najveća dubina 10, vremensko ograničenje izvršavanja, telo zahteva do 256 KB; introspekcija, Nitro IDE i detalji izuzetaka isključeni van Developmenta
- **Produkciono okruženje**: servisi rade sa `ASPNETCORE_ENVIRONMENT=Production`; `/_health` sakriven od javnog saobraćaja
- **Validacija se izvršava**: FluentValidation validatori su bili registrovani, ali se nikad nisu izvršavali za GraphQL zahteve; MediatR `ValidationBehavior` ih sada sprovodi. Podešavanja špila i glasovi se validiraju
- **AutoMapper uklonjen** (poznata ranjivost visokog nivoa u 14.0.0), zamenjen eksplicitnim kodom za mapiranje
- Jedna otporna Redis veza (`abortConnect=false`) umesto dve blokirajuće

### Ispravljeno (P1)
- `deleteTicket` je pri svakom pozivu vraćao grešku mapiranja; sada vraća `Boolean`
- Upiti `game`, `ticket` i `settings` vraćaju `GameNotFound` / `TicketNotFound` / `GameSettingsNotFound` umesto greške null mapiranja

### Bezbednost
- **Uloga hosta na serveru**: autor igre je host (`Game.hostId`); samo host može da pravi, menja i briše tikete, menja podešavanja, otkriva glasove i pokreće ponovno glasanje
- **Provera članstva**: samo igrači koji su ušli u igru mogu da glasaju, pišu u četu ili izvoze PDF izveštaj; anonimni zahtevi se odbijaju
- **Glasovi su skriveni na serveru** dok ih host ne otkrije (upiti, pretplate, proseci i PDF izveštaj); ponovno glasanje ih opet sakriva
- **JWT tajna uklonjena iz repozitorijuma**: čita se samo iz `Authentication__Secret`, najmanje 32 znaka, uz proveru issuera i audiencea
- **CORS na Fusion Gatewayu** ograničen na podešene origine (ranije: bilo koji origin sa kredencijalima); planovi upita isključeni
- **CI tajne u toku rada**: lozinka baze i JWT tajna se prosleđuju kao env promenljive kontejnera, umesto da se ugrađuju u imidže

### Dodato
- Mutacija `deleteGame` (samo host): briše igru sa tiketima, glasovima i igračima

### Ispravljeno
- **Ponovno glasanje počinje čistu rundu**: `reVoteTicket` na serveru briše prethodne glasove tiketa, pa se stari glasovi posle „Vote Again” više ne računaju
- **Stanje otkrivanja u interfejsu ne zavisi od redosleda**: zakasneli rezultat upita više ne može da sakrije već otkrivene glasove; sakriva ih samo ponovno glasanje
- **Deploy gatewaya koristi sveže sastavljenu šemu**: CI sada sastavlja `gateway.fgp` koji Docker imidž kopira (uz fiksiranu verziju Fusion CLI-ja), umesto nekorišćenog fajla
- Ulazak u igru preko linka pre nego što je identitet korisnika učitan više ne preskače ulazak
- `updateSettings` više ne može da prebaci podešavanja u drugu igru
- Poruke u četu ograničene na 1000 znakova

## [1.2.1] — 2026-07-16

### Dodato
- **Ponovno glasanje se sinhronizuje za sve igrače**: nova GraphQL mutacija `reVoteTicket` šalje ponovno aktiviranje tiketa svim povezanim klijentima preko pretplate `onTicketReVoted`; svi igrači vide tiket za ponovno glasanje, glasovi se resetuju, adresa se ažurira

### Izmenjeno
- **Vote Again objedinjen sa Re-vote**: oba dugmeta sada koriste istu logiku (ažuriranje adrese preko `replaceState`, reset glasova, ponovno povezivanje pretplata)
- Kontrole tajmera vidi samo host, uz podesivo trajanje (1–5 minuta)
- Pametnija poruka za čekanje: razlikuje „nema tiketa” od „host nije izabrao tiket”
- Strelice vraćaju pregledanje tiketa za sve igrače; Re-vote sme samo host

### Ispravljeno
- **Otkrivanje glasova** se sada sinhronizuje za sve igrače: automatski izabrani tiketi postavljaju pretplate na glasove
- **Ulazak u igru** izvlači ID igre iz punih adresa (npr. `/game/UUID` ili `https://...`) i podržava UUID-ove sa crticama
- **Adresa se sada ažurira** pri ulasku u igru (`/game/:id`) ili pravljenju tiketa (`/game/:id/ticket/:ticketId`)
- **Treperenje stranice uklonjeno**: `router.navigate` zamenjen sa `location.replaceState` za navigaciju u igri
- **Prazan igrač** više ne ulazi: `joinGame` je zaštićen proverom `currentUserId`
- **Automatski izbor tiketa** pri ulasku u igru sa postojećim tiketima (nema više „Čeka se host...” kad tiketi postoje)
- **Uvoz tiketa čuva redosled unosa**: tiketi se sada sortiraju po originalnom redosledu iz uvoza
- **Izbor trajanja tajmera** je onemogućen umesto sakriven dok tajmer radi
- **SSR padovi** ispravljeni: pristup `document` zaštićen u skrolovanju četa, WebSocket klijent se pravi samo u browseru
- **`--restart unless-stopped`** dodat za svih 8 Docker kontejnera u CI pipelineovima
- **Pretplate na glasove** se sada pravilno postavljaju na svim putanjama (host pravi, igrač ulazi, ponovno glasanje)
- **Strelice za navigaciju tiketa** vidljive su svim igračima za pregledanje; Re-vote u istoriji je ograničen na hosta

## [1.2.0] — 2026-07-15

### Arhitektura — prelazak na standalone komponente
- **Svih 26 komponenti prebačeno u standalone**: uklonjeno 9 NgModule fajlova (app, home, game, ticket, user, errors, shared, material, server)
- **Material importi po komponenti**: svaka komponenta uvozi samo Material module koje koristi (nema više zbirnog `MaterialModule` sa 17 modula)
- **`bootstrapApplication`** zamenjuje `platformBrowserDynamic`: moderno pokretanje Angulara
- **`provideAnimations()`** zamenjuje `BrowserAnimationsModule` u konfiguraciji aplikacije
- **Obrisan `AppServerModule`**: SSR sada koristi podrazumevani bootstrap export (`main.server.ts`)

### Performanse — SSR i optimizacije
- **SSR uključen u produkciji**: Node.js Express server zamenjuje nginx za statične fajlove
- **Unapred generisane 2 statične rute** (`/` i `/game`): skoro trenutni FCP preko unapred generisanog HTML-a
- **Ugrađivanje kritičnog CSS-a** (`inlineCritical: true`): Beasties izdvaja CSS za vidljivi deo stranice, bez stylesheeta koji blokira renderovanje
- **Font Awesome uklonjen**: zamenjen Material Icons i ugrađenim SVG-om za GitHub ikonu
- **CSS paket manji za 63%** (29 kB → 11 kB gzip)
- **Source mape isključene u produkciji** (ušteda od ~2–3 MB preuzimanja)
- **Pooštreni budžeti builda**: upozorenje na 1,5 MB, greška na 2,5 MB (bilo je 5/10 MB)
- **Google Tag Manager odložen** na `requestIdleCallback`: više nije na kritičnoj putanji
- **Preconnect nagoveštaji optimizovani**: uklonjeni nekorišćeni origini, ispravljeni `crossorigin` atributi
- **Uvedeni fajlovi okruženja** (`environment.ts` / `environment.prod.ts`) sa `fileReplacements`
- **Uklonjeno preuzimanje `config.json` u toku rada**: konfiguracija iz fajlova okruženja pri buildu uklanja mrežni poziv koji je blokirao pokretanje

### Performanse — renderovanje početne strane
- **Interfejs početne strane vidljiv odmah**: GameComponent se renderuje u SSR HTML-u bez čekanja na registraciju korisnika
- **`setupUser()` odložen** preko `afterNextRender`: radi u pozadini posle hidracije
- **ThemeService ojačan za SSR**: pristup `localStorage` zaštićen sa `isPlatformBrowser`
- **Svi pozivi `localStorage` zaštićeni** zbog SSR kompatibilnosti (GameComponent, HomeComponent, ThemeService)

### Ispravke
- **Dupli meta tagovi** uklonjeni iz `index.html` (charset, viewport, theme-color)
- **Učitavanje fontova**: Google Fonts i Material Icons se sada učitavaju asinhrono (`preload` + `onload`)
- **Microsoft Clarity** premešten na kraj `<head>` da ne blokira parsiranje
- **Dozvoljeni hostovi** podešeni u konstruktoru `CommonEngine` (SSR je padao nazad na CSR)
- **Neslaganje pri SSR hidraciji** ispravljeno: `afterNextRender` sprečava razlike između DOM-a klijenta i servera
- **Dockerfile ažuriran**: produkciona faza koristi `node:22-alpine` sa Express SSR serverom
- **CI pipeline**: ubacivanje adrese gatewaya preko `sed` zamenjeno Node.js skriptom (bezbedno radi sa znakovima u adresi), a zatim ubacivanjem preko `env:`

### Izmene koje lome kompatibilnost
- **Promenjen port Docker kontejnera**: aplikacija sada sluša na portu 4000 (bio je 80). Mapiranje portova u Docker Compose-u je ažurirano (`10000:4000`).
- **`public/assets/config.json` obrisan**: adrese gatewaya se sada podešavaju preko `environment.prod.ts`
- **Svi NgModule fajlovi obrisani**: spoljni importi tih modula moraju da se ažuriraju

### Izmenjeno
- **Novi izgled istorije glasanja**: naslov tiketa zauzima veći deo panela, AVG vrednost je mala i poravnata desno, bedž za aktivni čip
- **GitHub ikona** sada koristi registrovani SVG preko `MatIconRegistry` umesto Font Awesomea

## [1.1.0] — 2026-07-15

### Dodato
- **Uvoz tiketa**: dijalog za masovno lepljenje (jedan po redu) ili otpremanje fajla (CSV/JSON)
- **Footer komponenta** sa logom, linkovima, promenom teme, About dijalogom i brojem verzije
- **Strelice za navigaciju tiketa** (◀ ▶) za prelazak između tiketa bez ponovnog učitavanja stranice
- **Opis tiketa**: panel koji se širi ispod naziva tiketa (podržava više pasusa)
- **Ponovno glasanje za prethodne tikete** iz istorije glasanja, dugmetom `Re-vote this ticket`
- **Bedž aktivnog tiketa** u istoriji glasanja označava trenutni tiket
- **Prosečna vrednost glasanja** prikazana pored naslova tiketa (samo kad postoje glasovi)
- **Linkovi u četu**: adrese se automatski pretvaraju u linkove (`linkify`)
- **Glatki prelazi**: animacije fade/slide preko `@angular/animations`
- **Stranica sa izmenama na sajtu**: automatski generisana iz `CHANGELOG.md` uz `marked` (pri buildu)
- **Deo „Šta je novo” na sajtu**: pregled izmena na početnoj strani sa bedževima verzija
- **Build skripta za sve platforme** (`build.js`) za sajt
- **`llms.txt`** za AI agente koji pretražuju sajt
- **`MatMenuModule`, `MatTabsModule`, `MatDividerModule`** dodati u Material modul

### Izmenjeno
- **Novi header**: hamburger meni levo, manji naziv brenda, korisničko ime desno
- **Pozdrav/korisničko ime** premešteno iz posebnog reda u traku headera (štedi vertikalni prostor)
- **Promena teme** premeštena u meni headera (više nije zasebna ikona)
- **Početna strana** sada koristi istaknuti CTA stil kao „Create Ticket” (velika dugmad)
- **Sve akcije u igri** (New Ticket, Reveal Votes, Vote Again) objedinjene ispod karata za glasanje
- **Paneli Igrači i Čet** se sada skupljaju uz glatki prelaz
- **Tajmer** uvek vidljiv (onemogućen/prigušen posle otkrivanja glasova)
- **Poruke u četu**: sopstvene poruke desno sa ljubičastom pozadinom, ostale levo
- **Lista igrača**: trenutni korisnik podebljan, sa ljubičastom pozadinom
- **Istorija glasanja** vidljiva tek posle prvog napravljenog tiketa
- **Polje za ulazak u igru** prihvata punu adresu (automatski izvlači ID igre)
- **Izmena korisničkog imena**: klik na korisničko ime ili ikonu za izmenu otvara prozor za izmenu
- **Navigacija tiketa** koristi `Location.replaceState` umesto navigacije po rutama (bez treperenja)
- **Raspored početne strane** prebačen na flex (bez vertikalnog skrola na kratkim stranicama)
- **Novi izgled sajta**: glatko skrolovanje, fade-in animacije, podizanje na hover, ujednačena navigacija i footer
- **Kontrast teksta u footeru** poboljšan (`text-muted` → `text-secondary`) zbog pristupačnosti
- **Logo u footeru** zamenjen PNG-om 64x64 za retina ekrane
- **Navigacija sajta**: header sa backdrop-blur efektom, gradijentna gornja ivica na delu „Šta je novo”

### Ispravljeno
- Skupljanje četa prikazivalo je belu pozadinu (HostBinding + CSS ispravka)
- Poruke u četu se nisu skrolovale (ograničenje max-height + ispravke flex lanca)
- Tekst u četu nečitljiv u tamnom režimu (dodate tamne vrednosti za primary-50/100/200)
- Bočna traka na mobilnom preklapala je istoriju glasanja (ispravke responsive prelivanja)
- Broj glasova bio je pogrešan pri promeni tiketa (ispravka curenja podataka)
- 404 pri navigaciji na ponovno glasanje (nedostajao je deo putanje `/ticket/`)
- Prazan div za akcije igrača kad nema dugmadi za prikaz
- Dupla margina u footeru izazivala je vertikalni skrol
- Nizak kontrast izabranog stanja `mat-option` (globalna CSS izmena)
- Fiksne boje zamenjene dizajn tokenima u komponenti za pravljenje igre
- **Lighthouse optimizacije**: charset prvi u `<head>`, CSS koji ne blokira, dimenzije slika, preconnect
- **Odnos stranica glavne slike** ispravljen na prirodne dimenzije (1064×832)
- **Linkovima za društvene mreže** nedostajali su `aria-label` atributi (pristupačnost)
- **404 na stranici sa izmenama**: sada se builduje kao `/changelog/index.html` za statično služenje preko nginxa
- **CI build izmena**: kopira `CHANGELOG.md` u folder sajta pre Docker builda

## [1.0.0] — 2026-07-14

> Prvo stabilno izdanje. Aplikacija je ranije postojala kao **beta [0.0.1]**, koja je
> imala većinu istih funkcija, ali je bila nestabilna, sa nedoteranim interfejsom i
> bez pripreme za produkciju.

### Dodato
- **Mikroservisna arhitektura** sa GraphQL federacijom
  - API Gateway (YARP reverse proxy)
  - GraphQL Fusion Gateway (HotChocolate)
  - Game Engine (sobe za igru, tiketi, glasanje, PDF izveštaji)
  - User Management (anonimna korisnička imena, JWT autentifikacija, na Redisu)
- **Angular 19 web aplikacija** sa:
  - Glasanjem u realnom vremenu preko GraphQL WebSocket pretplata
  - Mehanizmom skrivenih glasova (glasovi su skriveni dok se ne klikne „Reveal”)
  - Renderovanjem na serveru (SSR) uz unapred generisanje za `/` i `/game`
  - PWA podrškom sa service workerom i manifestom za instalaciju
  - Apollo Angular GraphQL klijentom sa tipiziranim codegenom
  - Angular Material interfejsom sa azure-blue temom
- **Marketinški sajt** sa Tailwind CSS + Flowbite
- **SEO optimizacije:**
  - Potpuni meta tagovi (OG, Twitter, canonical, hreflang)
  - JSON-LD strukturirani podaci (WebSite, SoftwareApplication, FAQPage, WebApplication, BreadcrumbList)
  - robots.txt i sitemap.xml za oba domena
  - Dinamički naslovi i meta opisi po ruti
  - Semantički HTML landmarkovi i link za preskakanje na sadržaj
  - Dinamički sitemap endpoint na Game Engineu
- **CI/CD pipeline** (GitHub Actions):
  - Deploy workflow u više faza sa redosledom zavisnosti
  - Docker imidži koji se šalju u GitHub Container Registry (ghcr.io)
  - SSH deploy na produkcione servere
  - Posebni workflowovi za infrastrukturu (PostgreSQL, Redis, monitoring)
- **Monitoring** (OpenTelemetry, Prometheus, Loki, Grafana)
- **Ograničenje broja zahteva** na API Gatewayu (100 zahteva/min, fiksni prozor)
- **Bezbednosni headeri** u svim Nginx konfiguracijama (CSP, HSTS, X-Frame-Options itd.)
- **Nginx reverse proxy** sa SSL terminacijom (Let's Encrypt)
- Podela infrastrukture: javni aplikacioni server + privatni server baze
- Centralno upravljanje .NET paketima (`Directory.Packages.props`)
- Clean Architecture u svim backend servisima
- QuestPDF za generisanje PDF izveštaja igre
- EF Core sa PostgreSQL-om (code-first migracije)
- Redis keširanje (HybridCache + StackExchange.Redis)
- Deljeni NuGet paket (`OpenPlanningPoker.Shared`) objavljen na NuGet.org
- Health checkovi sa UI response writerom
- Preuzimanje PDF izveštaja igre
- Prilagođene 404 stranice
- Integracija Microsoft Clarity analitike (ID: `xmim6e317s`)
- Navigacija putanjom (breadcrumb) sa `BreadcrumbList` strukturiranim podacima
- Detaljna dokumentacija (`docs/`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`)

### Izmenjeno
- Node.js Docker imidži podignuti na 22 LTS (sa 23 koja nije LTS i 18 kojoj je istekla podrška)
- `apollo-client` v2 zamenjen sa `@apollo/client` v3
- BreadcrumbComponent izvezen iz GameModule za upotrebu u HomeComponent
- Putanja (breadcrumb) premeštena iznad imena igrača na početnoj strani
- Tailwind primarna paleta usklađena sa ljubičastom brenda (#7e3af2)

### Ispravljeno
- **KRITIČNO: `Database.Migrate()` je bio zakomentarisan**: baza se nikad nije pravila pri novom deployu
- Dupli COPY u Dockerfileu sajta (bezbednost: izvorni kod je kopiran u produkcioni imidž)
- Redosled CORS middlewarea (sada se izvršava pre GraphQL endpointa)
- Krhko parsiranje Bearer tokena u `HeaderPropagationHandler`
- Greška u kucanju `ApplicationErrorContants` → `ApplicationErrorConstants`
- FAQ strukturirani podaci nisu se slagali sa vidljivim HTML sadržajem
- `site.webmanifest` je imao prazna polja `name` i `short_name`
- Greška u kucanju na Twitter kartici: `@openplanninpoker` → `@openplanningpoker`
- Port za User Management u nginx proxyju (9091 → 9090)
- Godina u copyrightu u footeru ažurirana na 2026
- Dupli prikaz igrača na stolu (race condition u `getGamePlayers`)
- Neslaganje širine skrivene karte (dodat `box-sizing: border-box`)
- Taster Enter sada šalje formu za pravljenje igre
- Istorija glasanja sada poštuje stanje skrivenih glasova
- Ispravljene putanje u `generate-schema.ps1` u oba projekta
- Game Engine GraphQL projekat: dodat `Domain.Games` u GlobalUsings

## [0.0.1] — 2026-06 (Beta)

> Prvo beta izdanje. Aplikacija je radila od početka do kraja, ali je imala
> grube ivice u interfejsu, nedostatak doterivanja i probleme sa stabilnošću koji su rešeni u v1.0.0.

### Dodato
- Osnovno pravljenje sobe za igru i ulazak u nju
- Glasanje u realnom vremenu preko GraphQL pretplata
- Pravljenje i upravljanje tiketima
- Mehanizam otkrivanja glasova
- Generisanje PDF izveštaja igre
- Angular Material interfejs (pre redizajna)
- Početna mikroservisna postavka (Game Engine, User Management, API Gateway)
- Docker Compose za lokalni razvoj
- Osnovna nginx konfiguracija
