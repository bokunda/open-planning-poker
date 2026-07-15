# Changelog

All notable changes to Open Planning Poker are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

### Changed

### Fixed

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
- **Landing page** now uses hero CTA style matching "Create Ticket" (🎯, big buttons)
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
