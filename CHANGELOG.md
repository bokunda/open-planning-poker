# Changelog

All notable changes to Open Planning Poker are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Dynamic sitemap endpoint on Game Engine (serves active game URLs)
- Microsoft Clarity analytics integration (ID: `xmim6e317s`)
- Breadcrumb navigation with `BreadcrumbList` structured data
- Comprehensive documentation (`docs/`, `CONTRIBUTING.md`, `SECURITY.md`, `CODE_OF_CONDUCT.md`)
- **Hidden votes mechanism** — votes concealed behind "?" until "Reveal Votes" clicked
- **Hidden average voting value** — average hidden until votes revealed (voting + history)
- Auto-reset voting state on new ticket creation
- PWA support (service worker, manifest, installable)
- Security headers in nginx (CSP, HSTS, X-Frame-Options, X-Content-Type-Options)
- Rate limiting on API Gateway (100 req/min fixed window)
- Console logging for local development (Serilog console sink in Dev configs)

### Changed
- Upgraded Node.js Docker images to 22 LTS (from 23 non-LTS and 18 EOL)
- Replaced `apollo-client` v2 with `@apollo/client` v3
- Exported BreadcrumbComponent from GameModule for use in HomeComponent
- Moved breadcrumb above player name in home layout

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
- Tailwind primary color matched to brand purple (#7e3af2)
- Duplicate player display on game table (race condition in `getGamePlayers`)
- Hidden vote card width mismatch (added `box-sizing: border-box`)
- Enter key now submits create game form
- Voting history now respects hidden votes state
- `generate-schema.ps1` paths corrected in both projects
- Game Engine GraphQL project: added `Domain.Games` to GlobalUsings
- `site.webmanifest` had empty `name` and `short_name` fields
- Twitter card typo: `@openplanninpoker` → `@openplanningpoker`
- User Management nginx proxy port (9091 → 9090)
- Footer copyright year updated to 2026
- Tailwind primary color palette aligned to brand purple (#7e3af2)

## [1.0.0] — 2026-07-14

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
