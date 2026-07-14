# Architecture

## Design Principles

Open Planning Poker follows these architectural principles:

- **Microservices with GraphQL Federation** — Each bounded context (Game Engine, User Management) is an independent service. The Fusion Gateway stitches their GraphQL schemas into a unified API.
- **Clean Architecture** — Domain → Application → Infrastructure → Presentation. Business logic is isolated from infrastructure concerns.
- **CQRS-lite via MediatR** — Commands and queries are separated using the Mediator pattern.
- **Backend-for-Frontend (BFF)** — The Fusion Gateway acts as a BFF, composing data from multiple subgraphs for the Angular frontend.
- **API Gateway (YARP)** — Cross-cutting concerns (rate limiting, routing) are handled at the edge.

## High-Level Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Nginx Reverse Proxy                    │
│                  (SSL Termination, HSTS)                  │
└──┬──────┬──────────┬──────────┬────────────┬─────────────┘
   │      │          │          │            │
   ▼      ▼          ▼          ▼            ▼
┌──────┐ ┌────┐ ┌────────┐ ┌──────┐ ┌──────────────┐
│Website│ │App │ │Gateway │ │  GE  │ │ User Mgmt    │
│:9010  │ │:10K│ │:10010  │ │:9091 │ │ :9090        │
│static │ │SSR │ │Fusion  │ │Game  │ │ User Auth    │
└──────┘ └────┘ └───┬────┘ └──┬───┘ └──────┬───────┘
                    │          │            │
                    └──────────┼────────────┘
                               │
                    ┌──────────┴──────────┐
                    │   PostgreSQL :5432  │
                    │   Redis      :6379  │
                    └─────────────────────┘
```

## Service Responsibility Matrix

| Service | Port | Framework | Responsibility |
|---------|------|-----------|---------------|
| **API Gateway** | 11000 | .NET 9 + YARP 2.3 | Reverse proxy, rate limiting, OpenAPI docs |
| **Fusion Gateway** | 10010 | .NET 9 + HotChocolate 15 | GraphQL schema stitching, header propagation, CORS |
| **Game Engine** | 9091 | .NET 9 + EF Core + MediatR | Game rooms, tickets, voting, PDF reports, WebSocket subscriptions |
| **User Management** | 9090 | .NET 9 + HotChocolate + FluentValidation | Registration, anonymous usernames, JWT auth (Redis-backed) |
| **Web App** | 10000 | Angular 19 + SSR + PWA | SPA with server-side rendering, service worker, Apollo GraphQL client |
| **Website** | 9010 | Tailwind CSS + Flowbite | Static landing/marketing page |
| **PostgreSQL** | 5432 | PostgreSQL 15.5 | Game data persistence |
| **Redis** | 6379 | Redis 7 | User session cache |
| **Monitoring** | 3000–9050 | OpenTelemetry, Prometheus, Loki, Grafana | Metrics, logs, dashboards |

## Data Flow

### HTTP Request Flow
```
Browser → Nginx (SSL) → API Gateway (YARP) → Fusion Gateway → Subgraphs (GameEngine / UserMgmt)
```

### WebSocket Flow (GraphQL Subscriptions)
```
Browser → Nginx (WS upgrade) → Fusion Gateway → Game Engine (Redis pub/sub)
```

### Telemetry Flow
```
Services → OTel Collector (:4317) → Prometheus (metrics) + Loki (logs) → Grafana (dashboards)
```

## Database Schema

The Game Engine uses PostgreSQL with the following core entities:

| Table | Description |
|-------|-------------|
| **Game** | Game room with name and description |
| **GameSettings** | Voting configuration per game (deck setup, voting time, break allowed) |
| **GamePlayer** | Many-to-many: players in a game (referencing User Management's `Player`) |
| **Ticket** | User story / task to estimate |
| **Vote** | Player vote on a ticket (value + timestamp) |
| **Audit** | General-purpose audit log |

See `diagrams/Database/` for the full schema diagram.

## Clean Architecture Layers

```
┌─────────────────────────────────────┐
│          Presentation Layer          │
│  (GraphQL API, Health Checks, REST)  │
├─────────────────────────────────────┤
│          Application Layer           │
│  (Commands, Queries, Use Cases, DTOs)│
├─────────────────────────────────────┤
│            Domain Layer              │
│  (Entities, Value Objects, Events)   │
├─────────────────────────────────────┤
│         Infrastructure Layer         │
│  (EF Core, Redis, Auth, OTEL)       │
└─────────────────────────────────────┘
```

Each backend service follows this layered architecture with the **Dependency Inversion Principle**: the Domain layer has zero external dependencies.
