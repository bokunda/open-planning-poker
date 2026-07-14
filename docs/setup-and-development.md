# Setup & Development

## Prerequisites

| Tool | Minimum Version | Purpose |
|------|----------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0 | Backend services |
| [Node.js](https://nodejs.org/) | 22 LTS | Frontend build |
| [Docker](https://www.docker.com/) | 24+ | PostgreSQL, Redis, containerized builds |
| [Angular CLI](https://angular.dev/cli) | 19.x | Frontend development server |
| [Git](https://git-scm.com/) | 2.40+ | Version control |
| [PowerShell](https://learn.microsoft.com/powershell/) | 7+ | Schema generation scripts |

## Quick Start (Local Development)

### 1. Clone the Repository

```bash
git clone https://github.com/bokunda/open-planning-poker.git
cd open-planning-poker
```

### 2. Start Infrastructure

```bash
docker compose up -d opp-db OpenPlanningPoker.Cache
```

This starts PostgreSQL (port 5432) and Redis (port 6379).

### 3. Start Backend Services

Open four terminals and run:

```bash
# Terminal 1 — API Gateway (YARP reverse proxy)
cd backend/open-planning-poker-api-gateway
dotnet run
# → http://localhost:11000

# Terminal 2 — Game Engine (GraphQL subgraph)
cd backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
dotnet run
# → http://localhost:9091/graphql

# Terminal 3 — User Management (GraphQL subgraph)
cd backend/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
dotnet run
# → http://localhost:9090/graphql

# Terminal 4 — Fusion Gateway (GraphQL schema stitching)
cd backend/open-planning-poker-graphql-gateway
dotnet run
# → http://localhost:10010/graphql
```

### 4. Start Frontend

```bash
cd frontend/open-planning-poker-web-app
npm install
npm start
# → http://localhost:4200
```

### 5. Generate GraphQL Types (Optional)

```bash
cd frontend/open-planning-poker-web-app
npm run generate-types
```

This uses `graphql-codegen` to generate TypeScript types from the Fusion Gateway schema.

## Project Structure

```
open-planning-poker/
├── backend/
│   ├── open-planning-poker-api-gateway/          # YARP reverse proxy
│   ├── open-planning-poker-graphql-gateway/      # HotChocolate Fusion Gateway
│   ├── open-planning-poker-game-engine/          # Core game logic
│   │   └── src/
│   │       ├── OpenPlanningPoker.GameEngine.Domain/        # Entities, interfaces
│   │       ├── OpenPlanningPoker.GameEngine.Application/   # MediatR commands/queries
│   │       ├── OpenPlanningPoker.GameEngine.Infrastructure/# EF Core, Redis, OTEL
│   │       ├── OpenPlanningPoker.GameEngine.GraphQL/       # GraphQL API layer
│   │       └── OpenPlanningPoker.GameEngine.Api.Models/    # Shared DTOs
│   ├── open-planning-poker-user-management/      # User identity
│   ├── open-planning-poker-shared/               # NuGet package (auth, CORS, OTEL, cache)
│   └── open-planning-poker-monitoring-services/  # Docker Compose (metrics stack)
├── frontend/
│   ├── open-planning-poker-web-app/              # Angular 19 SPA + SSR + PWA
│   └── open-planning-poker-website/              # Tailwind CSS landing page
├── nginx-config/                                 # Nginx reference configs for each subdomain
├── diagrams/                                     # Architecture and database diagrams
├── docs/                                         # Documentation
├── docker-compose.yml                            # Docker Compose configuration
├── docker-compose.override.yml                   # Development overrides (ports, volumes)
└── docker-compose.prod.yml                       # Production overrides (env, restart policy)
```

## Environment Variables

### Backend Services

| Variable | Service | Default | Description |
|----------|---------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | All | `Development` | ASP.NET environment |
| `ASPNETCORE_HTTP_PORTS` | All | `8080` | HTTP port inside container |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | All | — | OpenTelemetry collector |
| `POSTGRES_DB` | PostgreSQL | `postgres` | Database name |
| `POSTGRES_USER` | PostgreSQL | `postgres` | Database user |
| `POSTGRES_PASSWORD` | PostgreSQL | `postgres` | Database password |
| `DOCKER_REGISTRY` | Compose | — | Container registry prefix |

### Connection Strings

Game Engine `appsettings.json`:
```json
"ConnectionStrings": {
    "Database": "Host=opp-db;Port=5432;Database=opp-gameengine;Username=postgres;Password=postgres;",
    "Cache": "OpenPlanningPoker.Cache:6379"
}
```

User Management `appsettings.json`:
```json
"ConnectionStrings": {
    "Cache": "OpenPlanningPoker.Cache:6379"
}
```

> **⚠️ Production warning:** Do not use hardcoded credentials in production. Use environment variables or a secrets manager.

## Running Tests

### Backend Tests

```bash
# Game Engine — all test projects
cd backend/open-planning-poker-game-engine
dotnet test

# Specific test project
dotnet test test/OpenPlanningPoker.GameEngine.Domain.UnitTests
dotnet test test/OpenPlanningPoker.GameEngine.GraphQL.Tests
dotnet test test/OpenPlanningPoker.GameEngine.Architecture.Tests
```

Test types:
- **Domain Unit Tests** — Pure business logic
- **GraphQL Tests** — API integration with Testcontainers
- **Architecture Tests** — NetArchTest rules enforcing Clean Architecture boundaries

### Frontend Tests

```bash
cd frontend/open-planning-poker-web-app
npm test
```

## Schema Generation & Fusion Compose

When GraphQL types change, regenerate the composed schema:

```bash
# Option 1: Use the provided script
cd backend/open-planning-poker-graphql-gateway
./generate-schema.ps1

# Option 2: Manual steps per subgraph
cd backend/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

cd backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
dotnet run -- schema export --output schema.graphql
fusion subgraph pack

cd backend/open-planning-poker-graphql-gateway
fusion compose -p gateway.fgp -s ../open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL
fusion compose -p gateway.fgp -s ../open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL
```

## Database Migrations

```bash
cd backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL

# Create a migration
dotnet ef migrations add MigrationName --startup-project . --project ../OpenPlanningPoker.GameEngine.Infrastructure

# Apply migrations
dotnet ef database update
```
