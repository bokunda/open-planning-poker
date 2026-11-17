# Listing the integrations in the stores (OPP-113)

The code and the public pages for the three listings are in the repository. The submissions themselves need the
owner's accounts, so they are done by hand. This page is the checklist for each store, with the listing texts.

Public pages used by every listing (built from `frontend/open-planning-poker-website/pages/`, in English, Serbian and
German):

| Page | URL |
|------|-----|
| Integrations | https://openplanningpoker.com/integrations/ |
| Privacy policy | https://openplanningpoker.com/privacy/ |
| Support | https://openplanningpoker.com/support/ |
| Terms of use | https://openplanningpoker.com/terms/ |

Before submitting, read the privacy policy and the terms once more. They describe the service as it runs today
(data kept, retention, analytics, hosting at Hetzner in Germany), and the owner is responsible for them.

## Slack App Directory (OPP-118)

1. In https://api.slack.com/apps → Open Planning Poker → **Manage Distribution**, turn on public distribution. The
   OAuth install (`/integrations/slack/install`) already stores one bot token per workspace.
2. **Basic Information**: icon (512×512, `integrations/store-assets/` once added), short and long description from
   the manifest (`integrations/slack/manifest.yml`), background colour `#6a3ae8`.
3. Scopes: `commands`, `chat:write`, `chat:write.public`. These are the minimum: the app answers `/poker` and posts
   the room and the results. It does not read messages.
4. **Submit to the App Directory**: privacy policy, support page and terms (links above), a test workspace or the
   steps for Slack's reviewers ("type `/poker Sprint 1` in any channel, open the room, vote, reveal").
5. After approval, link the directory page from the integrations page and `docs/integrations.md`.

## Microsoft Teams Store (OPP-119)

Depends on OPP-23 (the tab tested in a real Teams meeting).

1. Build the package: `integrations/teams/build-package.sh` (manifest 2.1.0, links to the new privacy and terms pages).
2. Check it in the Teams Developer Portal → **Apps** → **Import** → **Validate**. The tab is framed by Teams, so
   `app.openplanningpoker.com` sends `Content-Security-Policy: frame-ancestors` for Teams, Outlook and Microsoft 365.
3. Partner Center (https://partner.microsoft.com) → **Marketplace offers** → **Microsoft 365 and Copilot** → new Teams
   app. Upload the package, add the listing texts below and the screenshots (1366×768).
4. Test notes for reviewers: "Add the app to a meeting as a tab, choose *Start a new game*, save. Open the side panel
   and vote; open the meeting stage and reveal. No sign-in is needed."

## Atlassian Marketplace: Jira issue panel (OPP-120)

The Forge app is in `integrations/jira-forge/`. Its panel reads the issue as the current user (key, summary, story
points) and opens `https://app.openplanningpoker.com/estimate?key=…&title=…&url=…`. That page creates a game with the
issue as the first ticket, linked to the issue, so the host can **Save to KEY** after the reveal.

1. `npm install -g @forge/cli`, `forge login`, then in `integrations/jira-forge/`: `npm install`, `forge register`
   (replaces the app id in `manifest.yml`), `forge deploy -e production`, `forge install` on a test site.
2. Try it: open an issue, panel **Estimate with Open Planning Poker** → **Estimate in Open Planning Poker**. If Forge
   refuses to open the external link, add `permissions.external.fetch.client: [https://app.openplanningpoker.com]`
   to the manifest. Without it the app can keep the "Runs on Atlassian" badge, which is why it is not there now.
3. Marketplace: create the vendor (Bitcrafta), list the app as **free**, link the privacy, support and terms pages,
   and answer the security self-assessment and the data-residency questions: the app stores no data; Open Planning
   Poker stores the ticket name, key and link of the issue (see the privacy policy).

## Listing texts

### English

**Short:** Free planning poker for agile teams, right where you work.

**Long:** Estimate together without anchoring. Everyone votes in secret, then the cards are turned over at once.
Open Planning Poker shows the spread, asks the lowest and highest voters why, times the discussion and records the
agreed estimate. Start from Slack with /poker, add it as a tab in a Teams meeting, or estimate a Jira issue from its
panel and save the story points back. Free, open source, no sign-up.

### Srpski

**Kratko:** Besplatan planning poker za agilne timove, tamo gde već radite.

**Dugo:** Procenjujte zajedno bez sidrenja. Svi glasaju tajno, a onda se karte otkrivaju odjednom. Open Planning
Poker prikazuje raspon, pita najniži i najviši glas za razlog, meri vreme diskusije i beleži dogovorenu procenu.
Pokrenite ga iz Slacka komandom /poker, dodajte ga kao tab u Teams sastanak, ili procenite Jira issue iz njegovog
panela i sačuvajte story points nazad. Besplatno, otvorenog koda, bez registracije.

### Deutsch

**Kurz:** Kostenloses Planning Poker für agile Teams, direkt dort, wo ihr arbeitet.

**Lang:** Schätzt gemeinsam, ohne Ankereffekt. Alle stimmen geheim ab, dann werden die Karten gleichzeitig
aufgedeckt. Open Planning Poker zeigt die Streuung, fragt die niedrigste und höchste Stimme nach dem Grund, misst die
Diskussionszeit und hält die vereinbarte Schätzung fest. Startet es in Slack mit /poker, fügt es als Tab in ein
Teams-Meeting ein oder schätzt ein Jira-Issue direkt im Panel und speichert die Story Points zurück. Kostenlos,
Open Source, ohne Anmeldung.

## Screenshots

Take them from production after the release, so they show the current app: the room after a reveal (results,
notes, confidence), the Slack thread, the Teams side panel and stage, and the Jira panel. Sizes: Slack 1600×1000,
Teams 1366×768, Atlassian 1840×900. Keep them in `integrations/store-assets/`. The integrations promo video
(`promo-video`, `npm run render:integrations`) can be uploaded to YouTube and linked from the listings and the
integrations page.
