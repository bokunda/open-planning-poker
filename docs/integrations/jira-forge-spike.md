# Spike: an Atlassian Forge app with an issue panel (OPP-13)

**Question:** is a Forge app (an issue panel "Estimate with Open Planning Poker") a better long-term path than the
OAuth 2.0 (3LO) integration from our own backend that shipped in v3.2.0?

**Prototype:** [`integrations/jira-forge/`](../../integrations/jira-forge/). It has:

- an issue panel with UI Kit;
- a resolver that reads the issue as the current user (`api.asUser().requestJira`);
- a button that opens Open Planning Poker.

It is not deployed. Running it needs `npm install -g @forge/cli`, then `forge register`, `forge deploy` and
`forge install` on a test site.

## What Forge gives us

- **No tokens to store.** Forge calls Jira as the current user, so there is no OAuth app, no refresh tokens and no
  encrypted token table. Atlassian handles consent and revocation.
- **Inside the issue.** The team starts a round from the issue they are looking at. There is no import step for a
  single issue.
- **Marketplace distribution.** Forge apps install from the Atlassian Marketplace, and Jira admins trust them more
  than an external OAuth app.

## What it costs

- **A second codebase.** The app needs UI Kit or Custom UI (React) plus resolvers on the Forge runtime (Node.js 22).
  It cannot reuse the Angular app, so we would maintain two clients.
- **Egress.** Calling Open Planning Poker from Forge needs `permissions.external.fetch`. External egress puts the
  app outside "Runs on Atlassian", so it loses that Marketplace badge, and admins see the external domain at install.
- **Limits.** Resolvers time out after 25 s and have memory and request-size quotas. Real-time voting would stay in
  our app anyway, because Forge has no WebSockets for us. The panel can only start a round and show its result.
- **Marketplace listing requirements:**
  - a privacy policy;
  - a security self-assessment;
  - a data-residency answer;
  - a support contact and an SLA;
  - a vendor account;
  - a review of the listing.
  Cloud Fortified status (for larger customers) adds a bug bounty and reliability monitoring.
- **Identity.** A Forge user is an Atlassian account, while Open Planning Poker players are anonymous. Mapping
  between the two needs its own small API, for example "create a game for issue X, return the host link".

## Effort

About 1–2 weeks for an issue panel that creates a room for the issue, shows the agreed estimate when the room
closes and saves it to the issue's story points. Add the Marketplace work on top: about one more week of listing
and security forms, then Atlassian's review.

## Recommendation

**Keep the OAuth integration as the main path for now**, and do Forge later, only if a Marketplace listing becomes a
goal.

- OAuth already covers what teams do in a planning session: import the sprint, estimate, save every estimate back.
  It works from the app that runs the game.
- Forge would remove token storage, but the whole voting experience would stay in our app. Forge also adds a second
  client and the Marketplace overhead.
- If we build it, build it on top of a small "game for issue" API in the Game Engine, so it creates rooms with the
  same host-link mechanism that Slack and Teams use (OPP-22, OPP-23). The panel then stays a thin launcher.
