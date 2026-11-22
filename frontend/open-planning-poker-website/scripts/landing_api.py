"""The public API documentation page (OPP-161), in English, Serbian and German. The code samples are the same in every
language; only the text around them is translated."""
import html

BASE = "https://ge.openplanningpoker.com/api/v1"


def code(text, lang=""):
    return f'<pre class="overflow-x-auto rounded-lg bg-gray-100 p-3 text-sm dark:bg-gray-800"><code>{html.escape(text.strip())}</code></pre>'


CURL = f"""curl -H "Authorization: Bearer opp_…" {BASE}/sessions?take=10"""
SESSIONS = """{
  "items": [
    { "id": "01a1f644-…", "name": "Sprint 12", "createdOn": "2026-11-20T09:00:00Z",
      "ticketCount": 14, "estimatedCount": 12, "points": 41,
      "firstRoundConsensusShare": 0.58, "reVotesPerTicket": 0.3, "averageDiscussionSeconds": 95, "needsInfo": 1 }
  ],
  "total": 8, "skip": 0, "take": 10
}"""
TICKETS = f"""curl -H "Authorization: Bearer opp_…" {BASE}/sessions/01a1f644-…/tickets

{{
  "items": [
    {{ "id": "…", "sessionId": "01a1f644-…", "name": "Login with SSO", "externalKey": "PAY-42",
      "externalUrl": "https://acme.atlassian.net/browse/PAY-42", "provider": "jira",
      "estimate": "5", "estimated": true, "needsInfo": false, "estimatedAt": "2026-11-20T09:14:03Z" }}
  ],
  "total": 14, "skip": 0, "take": 50
}}"""
EVENT = """POST https://your-endpoint.example.com/opp
X-OPP-Event: ticket.estimated
X-OPP-Delivery: 5f0c…            (the same on every retry of this event)
X-OPP-Signature: t=1795291800,v1=9c1d…

{
  "id": "5f0c…", "type": "ticket.estimated", "createdAt": "2026-11-21T20:10:00Z", "teamId": "…",
  "data": {
    "sessionId": "…", "sessionName": "Sprint 12",
    "ticket": { "id": "…", "name": "Login with SSO", "externalKey": "PAY-42", "externalUrl": "…", "estimate": "5" },
    "changed": false
  }
}"""
FINISHED = """{ "type": "session.finished", "data": { "sessionId": "…", "sessionName": "Sprint 12", "ticketCount": 14, "points": 41 } }"""
NODE = """import crypto from 'node:crypto';

// body: the raw request body as a string, header: the X-OPP-Signature value
export function verify(secret, header, body, toleranceSeconds = 300) {
  const parts = Object.fromEntries(header.split(',').map((p) => p.split('=')));
  if (Math.abs(Date.now() / 1000 - Number(parts.t)) > toleranceSeconds) return false;
  const expected = crypto.createHmac('sha256', secret).update(`${parts.t}.${body}`).digest('hex');
  return crypto.timingSafeEqual(Buffer.from(expected), Buffer.from(parts.v1 ?? ''));
}"""
PYTHON = """import hashlib, hmac, time

def verify(secret: str, header: str, body: str, tolerance: int = 300) -> bool:
    parts = dict(p.split("=", 1) for p in header.split(","))
    if abs(time.time() - int(parts["t"])) > tolerance:
        return False
    expected = hmac.new(secret.encode(), f"{parts['t']}.{body}".encode(), hashlib.sha256).hexdigest()
    return hmac.compare_digest(expected, parts.get("v1", ""))"""

TEXT = {
    "en": {
        "title": "Public API and webhooks", "nav": "Public API",
        "description": "Read your team's planning poker sessions, tickets and estimates with a token, and get signed webhooks when a ticket is estimated or a session finishes. Free, for scripts, Zapier and n8n.",
        "lead": "Every team room has a small read-only API and outgoing webhooks. Use them to build your own reports, connect a tool we do not support yet, or practise integrations against a real API.",
        "auth": "Authentication",
        "authText": "The team owner creates a token in the team room under <strong>API and webhooks</strong>. It is shown once; we store only its hash. Send it as a bearer token. A token reads only its own team, and the owner can revoke it at any time.",
        "limits": "Limits",
        "limitsText": "60 requests a minute per token, and the usual limit per address. Above it the API answers <code>429</code> with <code>Retry-After: 60</code>. Lists are paged with <code>skip</code> and <code>take</code> (up to 100).",
        "endpoints": "Endpoints",
        "ep": [("GET /api/v1/team", "The team's name and description."),
               ("GET /api/v1/sessions", "The planning sessions (sprints), newest first, with points and how each session went."),
               ("GET /api/v1/sessions/{id}/tickets", "The tickets of a session with the agreed estimate and the link to the tracker.")],
        "privacy": "The API never returns single votes or player names: only the numbers the team room already shows.",
        "errors": "Errors are JSON: <code>{\"error\": {\"code\": \"unauthorized\", \"message\": \"…\"}}</code> with status 401, 404 or 429.",
        "webhooks": "Webhooks",
        "webhooksText": "Add an https address and pick the events. <code>ticket.estimated</code> fires when a ticket gets a final estimate (<code>changed</code> is true when an estimate was changed). <code>session.finished</code> fires when the last open ticket of a session gets one.",
        "retries": "Answer with any 2xx within 10 seconds. Failed deliveries are retried after 1, 5 and 30 minutes; the last 20 deliveries are listed in the team room. Use <code>X-OPP-Delivery</code> to drop duplicates. Internal addresses are refused.",
        "verify": "Check the signature",
        "verifyText": "<code>X-OPP-Signature</code> is <code>t=&lt;unix time&gt;,v1=&lt;HMAC-SHA256 of \"t.body\"&gt;</code> with your webhook secret. Reject requests whose time is more than five minutes off.",
        "nocode": "Zapier and n8n",
        "nocodeText": "<strong>n8n</strong>: add a <em>Webhook</em> node (POST), copy its production URL into the team room, then a <em>Code</em> node with the check above, and send the estimate on to Slack, a sheet or your tracker. <strong>Zapier</strong>: use <em>Webhooks by Zapier → Catch Raw Hook</em> as the trigger so the body stays as sent, then a <em>Code by Zapier</em> step for the check. To read data on a schedule, use an HTTP request node or <em>Webhooks by Zapier → GET</em> with the <code>Authorization</code> header.",
        "faq": [("Is the API free?", "Yes. The API and webhooks are part of every team room, without a paid plan."),
                ("Can I write data through the API?", "Not yet: the public API is read-only. Estimates are written by the app itself, which can save them to Jira, GitHub, Linear or Azure DevOps.")],
    },
    "sr": {
        "title": "Javni API i webhookovi", "nav": "Javni API",
        "description": "Čitaj planning poker sesije, tikete i procene svog tima pomoću tokena i primaj potpisane webhookove kad je tiket procenjen ili se sesija završi. Besplatno, za skripte, Zapier i n8n.",
        "lead": "Svaka timska soba ima mali API samo za čitanje i odlazne webhookove. Iskoristi ih za svoje izveštaje, za povezivanje alata koji još ne podržavamo ili za vežbanje integracija na pravom API-ju.",
        "auth": "Autentikacija",
        "authText": "Vlasnik tima pravi token u timskoj sobi pod <strong>API i webhookovi</strong>. Prikazuje se jednom; čuvamo samo njegov hash. Šalji ga kao bearer token. Token čita samo svoj tim, a vlasnik može da ga opozove u bilo kom trenutku.",
        "limits": "Ograničenja",
        "limitsText": "60 zahteva u minuti po tokenu, uz uobičajeno ograničenje po adresi. Iznad toga API odgovara sa <code>429</code> i <code>Retry-After: 60</code>. Liste se straniče sa <code>skip</code> i <code>take</code> (najviše 100).",
        "endpoints": "Endpointi",
        "ep": [("GET /api/v1/team", "Naziv i opis tima."),
               ("GET /api/v1/sessions", "Sesije planiranja (sprintovi), najnovije prve, sa poenima i tokom svake sesije."),
               ("GET /api/v1/sessions/{id}/tickets", "Tiketi sesije sa dogovorenom procenom i linkom ka alatu za tikete.")],
        "privacy": "API nikad ne vraća pojedinačne glasove ni imena igrača: samo brojke koje timska soba već prikazuje.",
        "errors": "Greške su JSON: <code>{\"error\": {\"code\": \"unauthorized\", \"message\": \"…\"}}</code> sa statusom 401, 404 ili 429.",
        "webhooks": "Webhookovi",
        "webhooksText": "Dodaj https adresu i izaberi događaje. <code>ticket.estimated</code> stiže kad tiket dobije konačnu procenu (<code>changed</code> je true kad je procena izmenjena). <code>session.finished</code> stiže kad poslednji otvoreni tiket sesije dobije procenu.",
        "retries": "Odgovori bilo kojim 2xx u roku od 10 sekundi. Neuspele isporuke se ponavljaju posle 1, 5 i 30 minuta; poslednjih 20 isporuka se vidi u timskoj sobi. Koristi <code>X-OPP-Delivery</code> da odbaciš duplikate. Interne adrese se odbijaju.",
        "verify": "Proveri potpis",
        "verifyText": "<code>X-OPP-Signature</code> je <code>t=&lt;unix vreme&gt;,v1=&lt;HMAC-SHA256 od \"t.body\"&gt;</code> sa tajnom tvog webhooka. Odbaci zahteve čije vreme odstupa više od pet minuta.",
        "nocode": "Zapier i n8n",
        "nocodeText": "<strong>n8n</strong>: dodaj <em>Webhook</em> čvor (POST), kopiraj njegov produkcioni URL u timsku sobu, zatim <em>Code</em> čvor sa gornjom proverom, i prosledi procenu u Slack, tabelu ili alat za tikete. <strong>Zapier</strong>: koristi <em>Webhooks by Zapier → Catch Raw Hook</em> kao okidač da telo ostane kakvo je poslato, pa korak <em>Code by Zapier</em> za proveru. Za periodično čitanje podataka koristi HTTP čvor ili <em>Webhooks by Zapier → GET</em> sa zaglavljem <code>Authorization</code>.",
        "faq": [("Da li je API besplatan?", "Da. API i webhookovi su deo svake timske sobe, bez plaćenog plana."),
                ("Mogu li da upisujem podatke preko API-ja?", "Još ne: javni API je samo za čitanje. Procene upisuje sama aplikacija, koja može da ih sačuva u Jiru, GitHub, Linear ili Azure DevOps.")],
    },
    "de": {
        "title": "Öffentliche API und Webhooks", "nav": "Öffentliche API",
        "description": "Lies die Planning-Poker-Sitzungen, Tickets und Schätzungen deines Teams mit einem Token und erhalte signierte Webhooks, wenn ein Ticket geschätzt ist oder eine Sitzung endet. Kostenlos, für Skripte, Zapier und n8n.",
        "lead": "Jeder Team-Raum hat eine kleine API zum Lesen und ausgehende Webhooks. Nutze sie für eigene Berichte, um ein Tool anzubinden, das wir noch nicht unterstützen, oder um Integrationen an einer echten API zu üben.",
        "auth": "Authentifizierung",
        "authText": "Der Inhaber des Teams erstellt im Team-Raum unter <strong>API und Webhooks</strong> ein Token. Es wird einmal angezeigt; wir speichern nur seinen Hash. Sende es als Bearer-Token. Ein Token liest nur sein eigenes Team, und der Inhaber kann es jederzeit widerrufen.",
        "limits": "Grenzen",
        "limitsText": "60 Anfragen pro Minute und Token, dazu die übliche Grenze pro Adresse. Darüber antwortet die API mit <code>429</code> und <code>Retry-After: 60</code>. Listen sind mit <code>skip</code> und <code>take</code> (bis 100) seitenweise.",
        "endpoints": "Endpunkte",
        "ep": [("GET /api/v1/team", "Name und Beschreibung des Teams."),
               ("GET /api/v1/sessions", "Die Planungssitzungen (Sprints), die neuesten zuerst, mit Punkten und dem Verlauf jeder Sitzung."),
               ("GET /api/v1/sessions/{id}/tickets", "Die Tickets einer Sitzung mit der vereinbarten Schätzung und dem Link zum Tracker.")],
        "privacy": "Die API liefert nie einzelne Stimmen oder Spielernamen: nur die Zahlen, die der Team-Raum ohnehin zeigt.",
        "errors": "Fehler sind JSON: <code>{\"error\": {\"code\": \"unauthorized\", \"message\": \"…\"}}</code> mit Status 401, 404 oder 429.",
        "webhooks": "Webhooks",
        "webhooksText": "Füge eine https-Adresse hinzu und wähle die Ereignisse. <code>ticket.estimated</code> kommt, wenn ein Ticket eine endgültige Schätzung bekommt (<code>changed</code> ist true, wenn eine Schätzung geändert wurde). <code>session.finished</code> kommt, wenn das letzte offene Ticket einer Sitzung eine bekommt.",
        "retries": "Antworte innerhalb von 10 Sekunden mit einem 2xx. Fehlgeschlagene Zustellungen werden nach 1, 5 und 30 Minuten wiederholt; die letzten 20 stehen im Team-Raum. Nutze <code>X-OPP-Delivery</code>, um Duplikate zu verwerfen. Interne Adressen werden abgelehnt.",
        "verify": "Signatur prüfen",
        "verifyText": "<code>X-OPP-Signature</code> ist <code>t=&lt;Unix-Zeit&gt;,v1=&lt;HMAC-SHA256 von \"t.body\"&gt;</code> mit dem Secret deines Webhooks. Lehne Anfragen ab, deren Zeit mehr als fünf Minuten abweicht.",
        "nocode": "Zapier und n8n",
        "nocodeText": "<strong>n8n</strong>: Füge einen <em>Webhook</em>-Knoten (POST) hinzu, kopiere seine Produktions-URL in den Team-Raum, dann einen <em>Code</em>-Knoten mit der Prüfung oben, und leite die Schätzung an Slack, eine Tabelle oder deinen Tracker weiter. <strong>Zapier</strong>: Nutze <em>Webhooks by Zapier → Catch Raw Hook</em> als Auslöser, damit der Body unverändert bleibt, dann einen Schritt <em>Code by Zapier</em> für die Prüfung. Zum regelmäßigen Lesen nimm einen HTTP-Knoten oder <em>Webhooks by Zapier → GET</em> mit dem Header <code>Authorization</code>.",
        "faq": [("Ist die API kostenlos?", "Ja. API und Webhooks gehören zu jedem Team-Raum, ohne bezahlten Plan."),
                ("Kann ich über die API Daten schreiben?", "Noch nicht: Die öffentliche API ist nur zum Lesen. Schätzungen schreibt die App selbst, die sie in Jira, GitHub, Linear oder Azure DevOps speichern kann.")],
    },
}


def api_page(lang):
    t = TEXT[lang]
    eps = "\n".join(f"  <li><code>{html.escape(p)}</code>: {html.escape(d)}</li>" for p, d in t["ep"])
    body = f"""
<h2>{t['auth']}</h2>
<p>{t['authText']}</p>
{code(CURL)}
<h2>{t['limits']}</h2>
<p>{t['limitsText']}</p>
<h2>{t['endpoints']}</h2>
<ul>
{eps}
</ul>
{code(SESSIONS)}
{code(TICKETS)}
<p>{html.escape(t['privacy'])}</p>
<p>{t['errors']}</p>
<h2>{t['webhooks']}</h2>
<p>{t['webhooksText']}</p>
{code(EVENT)}
{code(FINISHED)}
<p>{t['retries']}</p>
<h2>{t['verify']}</h2>
<p>{t['verifyText']}</p>
{code(NODE)}
{code(PYTHON)}
<h2>{t['nocode']}</h2>
<p>{t['nocodeText']}</p>
"""
    return {"title": t["title"], "nav": t["nav"], "description": t["description"], "lead": t["lead"], "body": body, "faq": t["faq"]}
