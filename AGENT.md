# Open Planning Poker — Agent Context

## Project Overview
Open Planning Poker is a free, open-source agile estimation tool for Scrum teams. Microservices architecture with GraphQL Federation, Angular 19 frontend, .NET 9 backend.

- **Website**: https://openplanningpoker.com
- **App**: https://app.openplanningpoker.com
- **Repo**: https://github.com/bokunda/open-planning-poker
- **Owner**: Bojan Piskulic (bokunda)

## Architecture

### Backend Services (Docker, .NET 9)
| Service | Port | Purpose |
|---------|------|---------|
| API Gateway (YARP) | 11000 | Reverse proxy, rate limiting (100 req/min) |
| Fusion Gateway (HotChocolate) | 10010 | GraphQL schema stitching, header propagation |
| Game Engine | 9091 | Core: games, tickets, voting, PDF reports (PostgreSQL) |
| User Management | 9090 | Auth, anonymous usernames (Redis) |
| Shared NuGet | — | CORS, Auth, OTEL, HybridCache |

### Frontend
| App | Port | Tech |
|-----|------|------|
| Web App | 4200/10000 (ext) / 4000 (container) | Angular 19, Apollo, Angular Material, SSR, PWA |
| Website | 9010 | Tailwind CSS, Flowbite |

### Infrastructure
- PostgreSQL 15.5 (private server 91.99.28.247)
- Redis 7 (private server)
- Nginx reverse proxy with SSL (public server 162.55.213.9)
- Monitoring: OpenTelemetry, Prometheus, Loki, Grafana
- CI/CD: GitHub Actions (11 workflows)

## Key Files & Paths

### Critical Source Files
- `backend/open-planning-poker-game-engine/src/.../Program.cs` — Game Engine startup, sitemap endpoint
- `backend/open-planning-poker-game-engine/src/.../Extensions/ApplicationBuilderExtensions.cs` — **Database.Migrate() was commented out! Fixed.**
- `backend/open-planning-poker-graphql-gateway/Program.cs` — Fusion Gateway setup
- `backend/open-planning-poker-graphql-gateway/gateway.fgp` — **Binary pack file — CI/CD regenerates with production URLs**
- `backend/open-planning-poker-game-engine/src/.../subgraph-config.json` — Docker DNS: `http://OpenPlanningPoker.GameEngine.GraphQL:8080/graphql`
- `backend/open-planning-poker-user-management/src/.../subgraph-config.json` — Docker DNS: `http://OpenPlanningPoker.UserManagement.GraphQL:8080/graphql`

### Frontend Critical Files
- `frontend/open-planning-poker-web-app/src/app/features/home/game/game.component.ts` — Main game logic, voting state
- `frontend/open-planning-poker-web-app/src/app/features/home/home.component.ts` — Home + breadcrumb
- `frontend/open-planning-poker-web-app/src/app/app.config.ts` — Apollo setup, SSR-safe guards, GitHub icon registration
- `frontend/open-planning-poker-web-app/src/app/app.routes.ts` — Routes with SEO titles
- `frontend/open-planning-poker-web-app/src/environments/environment.ts` — Dev gateway URLs (localhost)
- `frontend/open-planning-poker-web-app/src/environments/environment.prod.ts` — Production gateway URLs (CI/CD may override)
- `frontend/open-planning-poker-website/index.html` — Landing page with structured data

### Config Files
- `docker-compose.yml` — Build contexts point to `./backend/...` and `./frontend/...`
- `docker-compose.override.yml` — Dev ports, OTEL endpoint (162.55.213.9)
- `docker-compose.prod.yml` — Production: restart policies, named volumes
- `.github/workflows/deploy-opp.yaml` — Orchestrates full deploy
- `nginx-config/*.md` — Reference nginx configs (HSTS, gzip, www redirect)

## Database Schema (PostgreSQL — opp-gameengine)
7 tables: Games, GameSettings, GamePlayer, Tickets, Votes, Audits, __EFMigrationsHistory
- **Migration was commented out** — `Database.Migrate()` now uncommented in ApplicationBuilderExtensions.cs

## Voting Feature (Latest Implementation)
- **Hidden votes**: `votesRevealed` flag in game.component.ts
- Votes show "?" (purple badge) until "Reveal Votes" clicked
- Average voting value also hidden until reveal
- Auto-reset on new ticket: `votesRevealed = false`, `votes = []`
- History accordion respects hidden state for current ticket
- `getVoteDisplay()` in players.component.ts returns "?" or value

## SEO Features Implemented
- Structured data: WebSite, SoftwareApplication, FAQPage, BreadcrumbList, WebApplication
- Dynamic sitemap from Game Engine (`/sitemap.xml`)
- SSR + prerendering for / and /game routes
- PWA with service worker and manifest
- Security headers: CSP, HSTS, X-Frame-Options, X-Content-Type-Options
- Meta tags: OG, Twitter cards, canonical, hreflang, keywords
- robots.txt + sitemap.xml for both domains
- Breadcrumb navigation with JSON-LD BreadcrumbList
- Microsoft Clarity: xmim6e317s
- GA4: Website G-03VCK0K926, App G-DEXZ4JTESJ

## Known Limitations
- .NET SDK 10 installed locally, projects target net9.0 — need .NET 9 runtime for `dotnet run`
- `gateway.fgp` must be regenerated for local dev (use `regenerate-gateway.sh`)
- Fusion Gateway HTTPS port removed in local override (no dev cert in Docker)
- `AllowQueryPlan = true` in Fusion Gateway (info disclosure)
- JWT: no issuer/audience validation
- CORS only localhost in appsettings (CI/CD overwrites)
- `ASPNETCORE_ENVIRONMENT=Development` in CI/CD workflows
- Node 22-alpine for both Docker stages
- Apollo client uses `--legacy-peer-deps` due to version conflicts
- App container runs Node.js SSR server on port 4000 (not nginx on 80)
- All components are standalone (NgModules removed in 1.2.0)
- Font Awesome removed (1.2.0); replaced with Material Icons + inline SVG
- Gateway URLs configured via environment.ts/environment.prod.ts (not config.json)

## Local Development Setup
```bash
# Start infrastructure
docker compose up -d opp-db OpenPlanningPoker.Cache

# Create database
docker exec OpenPlanningPoker.Db createdb -U postgres opp-gameengine

# Build & start all services
docker compose up -d --build

# Start Angular dev server
cd frontend/open-planning-poker-web-app
npm install --legacy-peer-deps
npm start  # http://localhost:4200
```

## CI/CD Info
- Registry: ghcr.io/bokunda/*
- Deploy via SSH to 162.55.213.9
- gateway.fgp regenerated during deploy-opp-gateway workflow
- config.json overwritten during deploy-opp-ui workflow
- Private server 91.99.28.247: PostgreSQL + Redis only
- CI/CD uses GitHub Variables (SSH_HOST, POSTGRES_PASSWORD, etc.)
- Branch protection: requires PR (bypassed for admin)

## Common Pitfalls
1. **Database not created** — `Database.Migrate()` was commented out. Run `createdb opp-gameengine` manually if needed.
2. **gateway.fgp has production URLs** — Must regenerate for local dev. CI/CD handles production.
3. **Fusion Gateway HTTPS crash** — Exit code 139. Fixed by removing ASPNETCORE_HTTPS_PORTS from override.
4. **User Management / Game Engine hang** — Serilog Loki sink blocks startup if Loki unavailable. Added console sink to Dev configs.
5. **Duplicate players** — getGamePlayers() was overwriting list. Fixed with merge logic.
6. **config.json localhost** — CI/CD overwrites during build. Safe to keep localhost for dev.
7. **Angular Material imports** — `MatSelectModule`, `MatProgressSpinnerModule`, `MatSlideToggleModule`, `MatBadgeModule` added to MaterialModule.
8. **Chat/Reveal require gateway.fgp regeneration** — New types added to GameEngine schema. Run `.\generate-schema.ps1` then `docker compose build` + `up -d`.
9. **takeUntilDestroyed context error (NG0203)** — Must inject `DestroyRef` and pass to `takeUntilDestroyed(this.destroyRef)` when used outside injection context (e.g., in paramMap subscription callbacks).
10. **RedisValue ambiguity** — `JsonSerializer.Deserialize<T>(RedisValue)` is ambiguous; cast to `(string)value` explicitly.
11. **Shared project is a NuGet package** — `OpenPlanningPoker.Shared` v1.0.1-preview1. Local changes to `backend/open-planning-poker-shared/` are **ignored by Docker**. Register services directly in the consuming project's `Program.cs` instead.
12. **Gateway regeneration workflow** — After schema/C# changes to GameEngine GraphQL types, always run: `generate-schema.ps1` → `docker compose build` → `docker compose up -d --force-recreate`.
13. **Nginx: `location ~*` blocks without `proxy_pass`** — These capture static asset requests but don't forward to containers. Remove them when proxying to Docker containers; use only `location /` with `proxy_pass`.
14. **Nginx: use `127.0.0.1` not public IP** — Proxy to `127.0.0.1:PORT` for Docker containers on the same host. Public IP loops through external network.
15. **Website container shows default nginx page** — Build script (`package.json`) must copy HTML, images, robots.txt etc. to `dist/`. Fixed: `cp` commands added to `npm run build`.
16. **Sitemap entity property: `CreatedOn`** — Domain entity uses `DateTimeOffset CreatedOn`, column in PostgreSQL is `"CreatedOn"`. Not `CreatedAt`.
17. **DNS propagation after nameserver change** — Use `nslookup domain.com 8.8.8.8` for global check. Local ISP DNS may lag 24-48h. Google DNS can be set manually as workaround.

## Technical Notes

### Redis & Chat Infrastructure
- `IConnectionMultiplexer` registered as singleton in GameEngine `Program.cs` (not in Shared NuGet pkg)
- Chat messages stored in Redis list with key `chat:{gameId}`, 100 msg max, 24h TTL
- Pub-sub via `ITopicEventSender` (HotChocolate) for real-time delivery
- History query `chatMessages(gameId)` reads from Redis via `IDatabase.ListRangeAsync`

### HotChocolate Mutation Conventions
- Payload field name = camelCase of **return type** (e.g., `VotesRevealed` → `votesRevealed`), NOT method name
- `[MutationType]` + `[SubscriptionType]` attributes auto-generate schema types
- Topic format: `{nameof(MutationMethod)}_{parameterValue}`

### Frontend Subscription Lifecycle
- Use `takeUntilDestroyed(this.destroyRef)` (with injected `DestroyRef`) when subscribing outside injection context
- `ngOnChanges` for inputs that arrive after init (e.g., `gameId` via `@Input()`)
- Apollo subscriptions via `this.apollo.subscribe()`, queries via `this.apollo.watchQuery().valueChanges`

## Features Implemented (2026-07-15)

### Custom Purple Material Theme
- `src/theme.scss` — #7e3af2 purple palette (m2-define-palette)
- Dark theme variant: `mat.m2-define-dark-theme` applied via `.dark-theme` class
- `angular.json` references `src/theme.scss` instead of prebuilt CSS

### Dark Mode
- `ThemeService` (`src/app/shared/theme.service.ts`) — localStorage persistence, prefers-color-scheme detection
- Toggle button in header with sun/moon icon
- Material dark theme colors via `.dark-theme` class
- CSS custom properties for non-Material elements in `styles.scss`

### Host/Admin Role
- `isHost` getter in `game.component.ts` checks `localStorage.getItem('host_' + game.id)`
- Host stored on `createGame()`: `localStorage.setItem('host_' + gameId, userId)`
- Host-only: Reveal Votes, Vote Again, New Ticket buttons, Timer controls

### Multiple Voting Decks
- Presets in `src/app/shared/deck-presets.ts`: Fibonacci, Modified Fibonacci, T-Shirt Sizes, Powers of 2
- Backend: `CreateGameInput.deckSetup` optional string param in `GameMutations.CreateGameAsync`
- Frontend: Deck selector dropdown in create-game dialog, defaults to Fibonacci
- Custom deck editor: select "Custom Deck" and enter comma-separated values with live preview

### Game Timer
- `VotingComponent` — 60-second countdown timer (host-only)
- Play/Stop/Reset controls, auto-reveal votes on expiry
- Pulse animation when ≤10 seconds remaining (`timerWarning` class)

### Voting Consensus Indicator
- "X/Y voted" displayed on the poker table (center) and mobile view
- `votedCount`, `totalPlayers`, `consensusLabel` getters in `PlayersComponent`

### QR Code Sharing
- `QrShareDialogComponent` — QR code via `api.qrserver.com` (zero dependencies)
- Purple-colored QR code (#7e3af2), copy link button with clipboard API
- Share button (qr_code icon) in game details next to copy button

### Chat/Discussion (Redis, 24h TTL)
- Backend: `ChatMessage` GraphQL type, `ChatMutations.SendChatMessageAsync`, `ChatSubscriptions.OnChatMessage`
- Uses `ICurrentUserProvider` for player name, `ITopicEventSender` for Redis pub-sub
- **Persistence**: Messages stored in Redis list (`chat:{gameId}`) with 24h TTL (max 100 messages)
- **History**: `ChatQueries.GetChatMessagesAsync` — `chatMessages(gameId)` query fetches on page refresh
- Frontend: `ChatComponent` — FAB toggle, slide-up panel, `ngOnChanges` for gameId binding
- Real-time delivery via WebSocket subscription + history via watchQuery
- Dark mode: black message bubbles (#000), white text (#fff), purple author names

### Reveal Votes (Broadcast)
- Backend: `RevealVotesAsync` mutation + `OnVotesRevealed` subscription
- `VotesRevealed` type: `{ ticketId, revealedBy }`
- When host clicks "Reveal Votes": mutation publishes event, all clients receive subscription → `votesRevealed = true`
- Replaces previous client-side-only flag

### Consensus Counter Fix
- `subscribeToPlayerJoined` now increments `totalCount` when adding new players
- `consensusLabel` properly updates to reflect "X/Y voted" after players join

### Performance Optimizations
- `takeUntilDestroyed()` on all subscription-based methods
- `trackBy` on all 9 `*ngFor` loops: breadcrumb, players (6), voting-history (2), voting cards
- Mobile responsive: `max-width`/`width:100%` on table-container, game, voting cards

### Loading & Error States
- `LoadingComponent` with `mat-spinner` and configurable message
- Apollo error link in `app.config.ts` for global error handling
- Loading state in game component while game data loads

### SEO Optimizations (2026-07-15)
- **Deleted static `public/sitemap.xml`** — dynamic Game Engine endpoint serves game URLs (`/game/{id}`, `/game/{id}/ticket/{ticketId}`)
- **Dynamic canonical URL** per route — `HomeComponent.updatePageMeta()` updates `og:title`, `og:description`, `og:url`, `twitter:title`, `twitter:description`
- **Nginx cache headers**: `expires 1y` + `Cache-Control: public, immutable` for static assets (JS, CSS, images, fonts)
- **App `index.html`**: added `hreflang`, `apple-touch-icon`, `twitter:image:width/height`, granular `robots` meta
- **Website**: aligned FAQ structured data with visible HTML, uncommented footer logo, PNG logo instead of SVG
- **`font/woff2 font/woff`** added to `gzip_types` in all nginx configs

### CI/CD Notes
- **GitHub Actions**: `actions/checkout@v4.2.2`, `actions/setup-dotnet@v4.3.0` (Node 24 support)
- **Lazy Redis connection**: `IConnectionMultiplexer` uses factory lambda to avoid connect during `schema export` in CI
- **Angular 19 schema**: `serviceWorker` must be string path (`"ngsw-config.json"`), not boolean `true`
- **Website Dockerfile fix**: Build script copies ALL assets (CSS, HTML, images, robots.txt, sitemap) to `dist/` before container packaging. Uses `cp` commands in `npm run build`.
- **Nginx proxy**: Both website and app use `127.0.0.1` for Docker container proxy. No `location ~*` blocks — all traffic goes through `location /` with `proxy_pass`. Sitemap endpoint on app proxies to GameEngine on port 9091.

## Naming Conventions
- Docker containers: PascalCase with dots (OpenPlanningPoker.GameEngine.GraphQL)
- Docker DNS: lowercase with dots (openplanningpoker.gameengine.graphql)
- Commits: Conventional Commits (feat:, fix:, docs:, chore:)
- Branches: feature/, fix/, docs/, chore/
