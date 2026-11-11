# Changelog

All notable changes to Open Planning Poker are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Redesign — phase 3 (other screens)
- **Landing page**: a live demo table plays rounds on its own next to the pitch; "Join" accepts a pasted game link or code inline (no dialog) with a clear error for anything else
- **Room header**: game name and description as text, a labeled **Invite** button (link and QR code together) and a menu for copy link, PDF export and leave
- **Tickets list** replaces the history accordion: each ticket shows its revealed votes, the estimate and (for the host) a "vote again" button
- **Timer ring**: while the host's timer runs, a ring drains around the vote counter on the table and turns orange in the last 10 seconds
- **Phone**: your cards stick to the bottom of the screen and scroll sideways; the players panel is hidden (the table shows everyone) and chat starts collapsed

### Fixed
- Reloading a game with several tickets opened the first ticket and dropped the ticket from the URL; it now stays on the ticket in the URL

### Redesign — phase 2 (the table and the reveal)
- **Poker table**: every player has a seat on the edge of a felt table with their card; the current user sits at the bottom. The center shows a progress ring ("3/5 voted") and, after the reveal, the average
- **Card flight**: the chosen card flies from your hand to your seat (FLIP with the Web Animations API); other players' cards drop onto their seats
- **Staggered reveal**: cards flip one after another around the table; the lowest and highest card of a wide spread get an orange ring
- **Results panel**: distribution bars, average, median, vote count and a next step ("Consensus", "Close to consensus", or "Ask X and Y to explain, then vote again")
- After the reveal the hand is locked; "Vote Again" clears your chosen card
- "Reveal Votes" uses the brand violet instead of the grey/orange style; the "(Avg: ?)" next to the ticket title is gone (the table shows it)

### Fixed
- Card faces briefly showed through the back while a card dropped in (opacity on a 3D element flattened it)

### Redesign — phase 1 (foundation)
- **New visual foundation**: violet-tinted neutrals for both themes, a refined brand violet, and new roles for the coming table layout (felt, card face/back, consensus, spread) in `styles/_variables.scss`
- **Typography**: Bricolage Grotesque for headings and card numbers, Figtree for text, JetBrains Mono for keys and the timer (Roboto removed)
- **Poker card component** (`app-poker-card`): light face with corner indices, patterned back, flip animation (staggered on reveal), empty slot and a drop-in animation when a vote arrives
- **Your cards** lift on hover and stay raised when selected; the **players list** shows small cards (empty slot / face down / revealed) instead of "?" boxes
- **Pill buttons** across all Material button variants; motion tokens (durations, easing) and a global reduced-motion rule
- **Room skeleton** while a game loads instead of an empty screen

### Fixed
- Opening a link to a game that no longer exists showed an empty room and redirected to `/game/undefined`; it now says the game no longer exists

### Security (P1)
- **Rate limiting that actually applies** — per-client-IP token bucket on the Fusion gateway (the YARP policy was never attached) plus per-action limits for registration, game creation, reports, chat, tickets, votes and joins
- **Container ports bound to 127.0.0.1** — every service was also reachable directly over plain HTTP, bypassing nginx and TLS
- **GraphQL limits** — max depth 10, execution timeout, 256 KB request body; introspection, Nitro IDE and exception details disabled outside Development
- **Production environment** — services run with `ASPNETCORE_ENVIRONMENT=Production`; `/_health` hidden from public traffic
- **Validation runs** — FluentValidation validators were registered but never executed for GraphQL requests; a MediatR `ValidationBehavior` now enforces them. Deck setups and votes are validated
- **AutoMapper removed** (known high-severity vulnerability in 14.0.0) — replaced with explicit mapping code
- Single resilient Redis connection (`abortConnect=false`) instead of two blocking ones

### Fixed (P1)
- `deleteTicket` returned a mapping error on every call; it now returns `Boolean`
- `game`, `ticket` and `settings` queries return `GameNotFound` / `TicketNotFound` / `GameSettingsNotFound` instead of a null-mapping error

### Security
- **Server-side host role** — the game creator is the host (`Game.hostId`); only the host can create/update/delete tickets, change settings, reveal votes and start a re-vote
- **Membership checks** — only players who joined a game can vote, chat or export the PDF report; anonymous requests are rejected
- **Votes are hidden on the server** until the host reveals them (queries, subscriptions, averages and PDF report); re-vote hides them again
- **JWT secret rotated out of the repository** — read from `Authentication__Secret` only, minimum 32 characters, issuer and audience validated
- **Fusion Gateway CORS** limited to configured origins (was: any origin with credentials); query plans disabled
- **CI secrets at runtime** — DB password and JWT secret are passed as container env vars instead of being baked into images

### Added
- `deleteGame` mutation (host only) — deletes a game with its tickets, votes and players

### Fixed
- **Re-vote starts a clean round** — `reVoteTicket` deletes the ticket's previous votes on the server, so old votes no longer count after "Vote Again"
- **Reveal state is order-independent in the UI** — a late query result can no longer hide votes that were already revealed; only a re-vote hides them again
- **Gateway deploy uses the freshly composed schema** — CI now composes the `gateway.fgp` that the Docker image copies (with a pinned Fusion CLI) instead of an unused file
- Joining a game via link before the user identity resolved no longer skips the join
- `updateSettings` can no longer move settings to another game
- Chat messages limited to 1000 characters

## [1.2.1] — 2026-07-16

### Added
- **Re-vote syncs across all players** — new `reVoteTicket` GraphQL mutation broadcasts ticket re-activation to all connected clients via `onTicketReVoted` subscription; all players see the re-voted ticket, votes reset, URL updated

### Changed
- **Vote Again unified with Re-vote** — both buttons now use identical logic (URL update via `replaceState`, vote reset, subscription re-binding)
- Timer controls visible only to host with configurable duration (1-5 minutes)
- Smarter waiting message: distinguishes "no tickets" from "host not selecting a ticket"
- Arrows restore ticket browsing for all players; Re-vote action is host-only

### Fixed
- **Reveal votes** now syncs to all players — auto-selected tickets set up vote subscriptions
- **Join Game** extracts game ID from full URLs (e.g. `/game/UUID` or `https://...`) and supports UUIDs with dashes
- **URL now updates** when joining a game (`/game/:id`) or creating a ticket (`/game/:id/ticket/:ticketId`)
- **Page blink eliminated** — replaced `router.navigate` with `location.replaceState` for in-game navigation
- **Empty player** no longer joins — `joinGame` guarded with `currentUserId` check
- **Ticket auto-select** when joining a game with existing tickets (no more "Waiting for host..." when tickets exist)
- **Import tickets preserves input order** — tickets are now sorted by their original order in the import input
- **Timer duration selector** disabled instead of hidden while timer is running
- **SSR crashes** fixed — `document` access guarded in chat scroll, WebSocket client only created in browser
- **`--restart unless-stopped`** added to all 8 Docker containers in CI pipelines
- **Vote subscriptions** now properly set up on all code paths (host create, player join, re-vote)
- **Ticket navigation arrows** visible to all players for browsing; Re-vote in history restricted to host

## [1.2.0] — 2026-07-15

### Architecture — Standalone Components Migration
- **All 26 components converted to standalone** — removed 9 NgModule files (app, home, game, ticket, user, errors, shared, material, server)
- **Per-component Material imports** — each component imports only the Material modules it uses (no more 17-module `MaterialModule` barrel)
- **`bootstrapApplication`** replaces `platformBrowserDynamic` — modern Angular bootstrapping
- **`provideAnimations()`** replaces `BrowserAnimationsModule` in app config
- **Deleted `AppServerModule`** — SSR now uses default bootstrap export (`main.server.ts`)

### Performance — SSR & Optimizations
- **SSR enabled in production** — Node.js Express server replaces nginx for static file serving
- **Prerendered 2 static routes** (`/` and `/game`) — near-instant FCP via pre-rendered HTML
- **Critical CSS inlining** (`inlineCritical: true`) — Beasties extracts above-the-fold CSS, no render-blocking stylesheet
- **Font Awesome removed** — replaced with Material Icons + inline SVG for GitHub icon
- **CSS bundle reduced 63%** (29 kB → 11 kB gzipped)
- **Source maps disabled in production** (saved ~2-3 MB download)
- **Build budgets tightened** — 1.5 MB warning, 2.5 MB error (was 5/10 MB)
- **Google Tag Manager deferred** to `requestIdleCallback` — no longer in critical path
- **Preconnect hints optimized** — removed unused origins, fixed `crossorigin` attributes
- **Environment files introduced** (`environment.ts` / `environment.prod.ts`) with `fileReplacements`
- **Removed runtime `config.json` fetch** — build-time config via environment files eliminates blocking network call on bootstrap

### Performance — Landing Page Rendering
- **Landing UI visible immediately** — GameComponent renders in SSR HTML without waiting for user registration
- **`setupUser()` deferred** via `afterNextRender` — runs in background after hydration
- **ThemeService hardened for SSR** — `localStorage` access guarded with `isPlatformBrowser`
- **All `localStorage` calls guarded** for SSR compatibility (GameComponent, HomeComponent, ThemeService)

### Fixes
- **Duplicate meta tags** removed from `index.html` (charset, viewport, theme-color)
- **Font loading** — Google Fonts and Material Icons now loaded asynchronously (`preload` + `onload`)
- **Microsoft Clarity** moved to end of `<head>` to avoid blocking parsing
- **Allowed hosts** configured in `CommonEngine` constructor (SSR was falling back to CSR)
- **SSR hydration mismatch** fixed — `afterNextRender` prevents client/server DOM differences
- **Dockerfile updated** — production stage uses `node:22-alpine` with Express SSR server
- **CI pipeline** — gateway URL injection via `sed` replaced with Node.js script (handles URL characters safely) then `env:`-based injection

### Breaking Changes
- **Docker container port changed** — app now listens on port 4000 (was 80). Docker Compose port mapping updated (`10000:4000`).
- **`public/assets/config.json` deleted** — gateway URLs now configured via `environment.prod.ts`
- **All NgModule files deleted** — any external imports of these modules must be updated

### Changed
- **Voting history UI redesigned** — ticket title dominates panel space, AVG value minimal right-aligned, active chip badge
- **GitHub icon** now uses registered SVG via `MatIconRegistry` instead of Font Awesome

## [1.1.0] — 2026-07-15

### Added
- **Import Tickets** — bulk paste (one per line) or file upload (CSV/JSON) dialog
- **Footer component** with logo, links, theme toggle, About dialog, and version number
- **Ticket navigation arrows** (◀ ▶) for switching between tickets without page reload
- **Ticket description** — expandable panel below ticket name (multi-paragraph support)
- **Re-vote on past tickets** from voting history with `Re-vote this ticket` button
- **Active ticket badge** in voting history indicating current ticket
- **Average voting value** displayed inline next to ticket title (only when votes exist)
- **Chat link support** — URLs auto-converted to clickable links (`linkify`)
- **Fluid transitions** — fade/slide animations using `@angular/animations`
- **Website changelog page** — auto-generated from `CHANGELOG.md` using `marked` (build-time)
- **Website "What's New" section** — changelog preview on homepage with version badges
- **Cross-platform build script** (`build.js`) for the marketing website
- **`llms.txt`** for AI agent browsing compliance
- **`MatMenuModule`, `MatTabsModule`, `MatDividerModule`** added to Material module

### Changed
- **Header redesign**: hamburger menu on left, brand text smaller, username on right
- **Welcome/username** moved from dedicated row into header toolbar (saves vertical space)
- **Theme toggle** moved into header menu (no longer standalone icon)
- **Landing page** now uses hero CTA style matching "Create Ticket" (, big buttons)
- **All game actions** (New Ticket, Reveal Votes, Vote Again) unified below voting cards
- **Players & Chat panels** are now collapsible with smooth toggle
- **Timer** always visible (disabled/dimmed after votes revealed)
- **Chat messages**: own messages right-aligned with purple background, others left-aligned
- **Players list**: current user bolded with purple highlight background
- **Voting history** only visible after first ticket created
- **Join Game** field accepts full URL (auto-extracts game ID)
- **Edit username** — clicking username text or edit icon opens update modal
- **Ticket navigation** uses `Location.replaceState` instead of route navigation (no blink)
- **Home layout** refactored to flex-based (no vertical scroll on short pages)
- **Website redesign**: smooth scroll, fade-in animations, hover-lift effects, consistent nav/footer
- **Footer text contrast** improved (`text-muted` → `text-secondary`) for accessibility
- **Footer logo** upgraded to 64x64 PNG for retina displays
- **Website nav**: backdrop-blur header, gradient top border on What's New section

### Fixed
- Chat collapse showing white background (HostBinding + CSS fix)
- Chat messages not scrolling (max-height constraint + flex chain fixes)
- Dark mode chat text unreadable (added dark overrides for primary-50/100/200)
- Mobile sidebar overlapping vote history (responsive overflow fixes)
- Vote count showing wrong numbers when switching tickets (data leak fix)
- Re-vote navigation 404 (missing `/ticket/` path segment)
- Empty players-actions div when no buttons to show
- Footer double margin causing vertical scroll
- `mat-option` selected state low contrast (global CSS override)
- Hardcoded colors replaced with design tokens in create-game component
- **Lighthouse optimizations**: charset first in `<head>`, non-blocking CSS, image dimensions, preconnects
- **Hero image aspect ratio** corrected to natural dimensions (1064×832)
- **Social links** missing `aria-label` attributes (accessibility)
- **Changelog page 404** — now built as `/changelog/index.html` for nginx static serving
- **CI changelog build** — copy `CHANGELOG.md` to website dir before Docker build

## [1.0.0] — 2026-07-14

> First stable release. Previously the application existed as **beta [0.0.1]** which
> contained most of the same features but was unstable, had unpolished UI, and
> lacked production hardening.

### Added
- **Microservices architecture** with GraphQL Federation
  - API Gateway (YARP reverse proxy)
  - GraphQL Fusion Gateway (HotChocolate)
  - Game Engine (game rooms, tickets, voting, PDF reports)
  - User Management (anonymous usernames, JWT auth, Redis-backed)
- **Angular 19 Web App** with:
  - Real-time voting via GraphQL WebSocket subscriptions
  - Hidden votes mechanism (votes concealed until "Reveal" clicked)
  - Server-Side Rendering (SSR) with prerendering for `/` and `/game`
  - PWA support with service worker and installable manifest
  - Apollo Angular GraphQL client with typed codegen
  - Angular Material UI with azure-blue theme
- **Marketing Website** with Tailwind CSS + Flowbite
- **SEO Optimizations:**
  - Comprehensive meta tags (OG, Twitter, canonical, hreflang)
  - JSON-LD structured data (WebSite, SoftwareApplication, FAQPage, WebApplication, BreadcrumbList)
  - robots.txt and sitemap.xml for both domains
  - Dynamic per-route page titles and meta descriptions
  - Semantic HTML landmarks and skip-to-content link
  - Dynamic sitemap endpoint on Game Engine
- **CI/CD Pipeline** (GitHub Actions):
  - Multi-stage deployment workflow with dependency ordering
  - Docker image builds pushed to GitHub Container Registry (ghcr.io)
  - SSH deployment to production servers
  - Separate workflows for infrastructure (PostgreSQL, Redis, monitoring)
- **Monitoring Stack** (OpenTelemetry, Prometheus, Loki, Grafana)
- **Rate limiting** on API Gateway (100 req/min fixed window)
- **Security headers** on all Nginx configs (CSP, HSTS, X-Frame-Options, etc.)
- **Nginx reverse proxy** with SSL termination (Let's Encrypt)
- Infrastructure split: public application server + private database server
- Central .NET package management (`Directory.Packages.props`)
- Clean Architecture across all backend services
- QuestPDF for PDF game report generation
- EF Core with PostgreSQL (code-first migrations)
- Redis caching (HybridCache + StackExchange.Redis)
- Shared NuGet package (`OpenPlanningPoker.Shared`) published to NuGet.org
- Health checks with UI response writer
- Game report PDF download
- Custom 404 error pages
- Microsoft Clarity analytics integration (ID: `xmim6e317s`)
- Breadcrumb navigation with `BreadcrumbList` structured data
- Comprehensive documentation (`docs/`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`)

### Changed
- Upgraded Node.js Docker images to 22 LTS (from 23 non-LTS and 18 EOL)
- Replaced `apollo-client` v2 with `@apollo/client` v3
- Exported BreadcrumbComponent from GameModule for use in HomeComponent
- Moved breadcrumb above player name in home layout
- Tailwind primary color palette aligned to brand purple (#7e3af2)

### Fixed
- **CRITICAL: `Database.Migrate()` was commented out** — database never created on fresh deploy
- Double COPY in website Dockerfile (security: source tree was copied to production image)
- CORS middleware ordering (now executes before GraphQL endpoint)
- `HeaderPropagationHandler` fragile Bearer token parsing
- `ApplicationErrorContants` typo → `ApplicationErrorConstants`
- FAQ structured data mismatched with visible HTML content
- `site.webmanifest` had empty `name` and `short_name` fields
- Twitter card typo: `@openplanninpoker` → `@openplanningpoker`
- User Management nginx proxy port (9091 → 9090)
- Footer copyright year updated to 2026
- Duplicate player display on game table (race condition in `getGamePlayers`)
- Hidden vote card width mismatch (added `box-sizing: border-box`)
- Enter key now submits create game form
- Voting history now respects hidden votes state
- `generate-schema.ps1` paths corrected in both projects
- Game Engine GraphQL project: added `Domain.Games` to GlobalUsings

## [0.0.1] — 2026-06 (Beta)

> Initial beta release. The application was functional end-to-end but had
> rough UI edges, missing polish, and stability issues that were addressed in v1.0.0.

### Added
- Basic game room creation and joining
- Real-time voting with GraphQL subscriptions
- Ticket creation and management
- Vote reveal mechanism
- PDF game report generation
- Angular Material UI (pre-redesign)
- Initial microservices setup (Game Engine, User Management, API Gateway)
- Docker Compose for local development
- Basic nginx configuration
