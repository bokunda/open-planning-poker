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
| Web App | 4200/10000 | Angular 19, Apollo, Angular Material, SSR, PWA |
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
- `frontend/open-planning-poker-web-app/src/app/app.config.ts` — Apollo setup, SSR-safe guards
- `frontend/open-planning-poker-web-app/src/app/app.routes.ts` — Routes with SEO titles
- `frontend/open-planning-poker-web-app/public/assets/config.json` — **Locale URLs for dev. CI/CD overwrites for prod.**
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
- Node 22-alpine for both Dockerfiles
- Apollo client uses `--legacy-peer-deps` due to version conflicts

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

## Naming Conventions
- Docker containers: PascalCase with dots (OpenPlanningPoker.GameEngine.GraphQL)
- Docker DNS: lowercase with dots (openplanningpoker.gameengine.graphql)
- Commits: Conventional Commits (feat:, fix:, docs:, chore:)
- Branches: feature/, fix/, docs/, chore/
