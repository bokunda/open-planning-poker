# Open Planning Poker

🌍 [Project website.](https://openplanningpoker.com)

This document represents a project specification of the Open Planning Poker solution. Topics to cover are:
- What is Open Planning Poker?
- Technical implementation of Open Planning Poker.
- System design of the whole solution.
- How to run locally.
- How to access online.

## What is Open Planning Poker?

Open Planning Poker is a free and open-source solution that implements one of the ways how to plan and estimate effort.
The idea is simple, you have to import or write all the user stories/features/tasks and during the 'poker' sessions you have to discuss about the scope and effort of each item as a team.
The next step is **voting** where you have to assign a complexity value for the discussed story. When the voting is finished, an average value is calculated and then you can argue if the average value is acceptable or to vote again.

Open Planning Poker provides you with an easy option to:
- Create a poker room.
- Import or add stories/features/tasks manually.
- Voting mechanism.
- Export results.

## Screenshots

### Main screen
![Main screen](./images/open-planning-poker-game-demo.png)

### Game History
![History screen](./images/open-planning-poker-game-demo-history.png)


## System design

### High-Level Architecture

![Infrastructural HLA Diagram](diagrams/high_level_diagram.drawio.png "Infrastructual HLA Diagram")

### Data Model

![Game Engine DB](diagrams/Database/Open%20Planning%20Poker%20-%20Game%20Engine%20DB%20Schema.png "Game Engine Database Schema")

## Technical implementation

This product has a few services:
- [Presentational website](https://github.com/bokunda/open-planning-poker/tree/main/frontend/open-planning-poker-website)
- [Frontend Application](https://github.com/bokunda/open-planning-poker/tree/main/frontend/open-planning-poker-web-app)
- [GraphQL Gateway](https://github.com/bokunda/open-planning-poker/tree/main/backend/open-planning-poker-graphql-gateway)
- [User Management](https://github.com/bokunda/open-planning-poker/tree/main/backend/open-planning-poker-user-management)
- [Game Engine](https://github.com/bokunda/open-planning-poker/tree/main/backend/open-planning-poker-game-engine)
- [Shared NuGet](https://github.com/bokunda/open-planning-poker/tree/main/backend/open-planning-poker-shared)
- [Monitoring Services](https://github.com/bokunda/open-planning-poker/tree/main/backend/open-planning-poker-monitoring-services)

## How To

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 22 LTS](https://nodejs.org/)
- [Docker & Docker Compose](https://www.docker.com/)
- [Angular CLI](https://angular.dev/cli) (`npm install -g @angular/cli`)

### Run Open Planning Poker locally

```bash
# Start infrastructure (PostgreSQL + Redis)
docker compose up -d opp-db OpenPlanningPoker.Cache

# Backend services (each in a separate terminal)
cd backend/open-planning-poker-api-gateway && dotnet run
cd backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL && dotnet run
cd backend/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL && dotnet run
cd backend/open-planning-poker-graphql-gateway && dotnet run

# Frontend
cd frontend/open-planning-poker-web-app && npm install && npm start
```

Or use the convenience scripts:
- Windows: `.\run-game-locally.ps1`
- Linux: `./run-game-locally.sh`

### Production Deployment

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

For CI/CD deployment, use the GitHub Actions workflows in `.github/workflows/`:
- `deploy-opp.yaml` — Deploy all application services
- `deploy-postgresql.yaml` — Deploy PostgreSQL
- `deploy-redis.yaml` — Deploy Redis
- `deploy-opp-website.yaml` — Deploy marketing website
- `deploy-monitoring-services.yaml` — Deploy monitoring stack

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` |
| `POSTGRES_DB` | PostgreSQL database name |
| `POSTGRES_USER` | PostgreSQL user |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector URL |
| `DOCKER_REGISTRY` | Container registry prefix (optional) |

### Access Open Planning Poker online

You can access the app by visiting this [link.](https://openplanningpoker.com)

## License

Project is under [GNU General Public License](https://github.com/bokunda/open-planning-poker/blob/main/LICENSE)
