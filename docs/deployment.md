# Deployment

## Infrastructure Overview

Open Planning Poker runs on two Hetzner Cloud servers:

| Server | IP | Role | Services |
|--------|-----|------|----------|
| **Public** | `162.55.213.9` | Application server | All app containers, Nginx reverse proxy, monitoring stack |
| **Private** | `91.99.28.247` | Data server | PostgreSQL, Redis (firewall-restricted to public server only) |

## Docker Compose Deployment

### Production

```bash
# On the public server (162.55.213.9)
cd open-planning-poker-stack/open-planning-poker

# Start all application services
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Check status
docker ps
```

The production compose file (`docker-compose.prod.yml`) sets:
- `ASPNETCORE_ENVIRONMENT=Production`
- `restart: unless-stopped` on all services
- Named Docker volume for PostgreSQL data
- Environment variable placeholders for secrets

### Private Infrastructure

```bash
# On the private server (91.99.28.247)
docker network create oppnetwork

# Start PostgreSQL
docker run -d \
  --name OpenPlanningPoker.Db \
  --network oppnetwork \
  --restart unless-stopped \
  -e POSTGRES_DB=postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=<secure-password> \
  -v /opt/opp-database:/var/lib/postgresql/data \
  -p 5432:5432 \
  postgres:15.5

# Start Redis
docker run -d \
  --name OpenPlanningPoker.Cache \
  --network oppnetwork \
  --restart unless-stopped \
  -p 6379:6379 \
  redis:latest
```

### Monitoring Stack

```bash
cd backend/open-planning-poker-monitoring-services
docker compose up -d
```

| Service | Port | Access |
|---------|------|--------|
| Grafana | 3000 | `https://grafana.openplanningpoker.com` |
| Loki | 3100 | Internal (log aggregation) |
| Prometheus | 9050 | Internal (metrics) |
| OTel Collector | 4317 | Internal (gRPC telemetry) |

## CI/CD (GitHub Actions)

All deployment is automated via GitHub Actions workflows in `.github/workflows/`.

### Workflows

| Workflow | Trigger | What it deploys |
|----------|---------|-----------------|
| `deploy-opp.yaml` | Manual (`workflow_dispatch`) | Full stack: UM → GE → Gateway → API GW → UI |
| `deploy-opp-game-engine.yaml` | Manual or called by parent | Game Engine GraphQL |
| `deploy-opp-user-management.yaml` | Manual or called by parent | User Management GraphQL |
| `deploy-opp-gateway.yaml` | Manual or called by parent | Fusion Gateway |
| `deploy-opp-ui.yaml` | Manual or called by parent | Angular Web App |
| `deploy-opp-website.yaml` | Manual | Marketing website |
| `deploy-postgresql.yaml` | Manual | PostgreSQL on private server |
| `deploy-redis.yaml` | Manual | Redis on private server |
| `deploy-monitoring-services.yaml` | Manual | Monitoring stack |
| `deploy-opp-shared-nuget.yaml` | Manual | Shared NuGet package to NuGet.org |
| `release.yaml` | Push to `main` (version or changelog changed) | Tag `vX.Y.Z` + GitHub Release from `CHANGELOG.md` |
| `uptime.yaml` | Every 15 minutes | Nothing: checks the public endpoints, opens/closes an `uptime` issue |

### Releasing a version

1. In the PR, bump `"version"` in `frontend/open-planning-poker-web-app/package.json` (the app footer shows it).
2. Move the `## [Unreleased]` items to `## [X.Y.Z] — YYYY-MM-DD` in `CHANGELOG.md`.
3. After merge, `release.yaml` tags the commit and publishes the GitHub Release; `deploy-opp.yaml` deploys
   the services, the app and the website (whose "What's new" and changelog come from `CHANGELOG.md`).

### Deployment Flow

```
deploy-opp.yaml
  ├── opp-um (User Management)
  ├── opp-ge (Game Engine)
  ├── opp-graphql-gtw (Fusion Gateway)  ← depends on UM + GE
  ├── opp-ui (Web App)                  ← depends on Fusion Gateway
  └── opp-website (Website)             ← depends on Web App
```

### GitHub Variables & Secrets

The workflows use these repository variables and secrets:

| Variable/Secret | Purpose |
|----------------|---------|
| `SSH_HOST` | Public server IP (162.55.213.9) |
| `SSH_USER` | Deploy user on the public server (`deploy`) |
| `SSH_DEPLOY_KEY` | Private ed25519 key of the deploy user on the public server (secret) |
| `SSH_PRIVATE_HOST` | Private server IP |
| `SSH_PRIVATE_USER` | Deploy user on the private server (`deploy`) |
| `SSH_PRIVATE_DEPLOY_KEY` | Private ed25519 key of the deploy user on the private server (secret) |
| `OPP_API_GATEWAY` | GraphQL HTTP endpoint URL |
| `OPP_WSS_URL` | GraphQL WebSocket endpoint URL |
| `OPP_DB_HOST` | PostgreSQL host |
| `POSTGRES_USER` | PostgreSQL user |
| `POSTGRES_PASSWORD` | PostgreSQL password (secret) |
| `REDIS_HOST` | Redis connection string |
| `LOKI_URL` | Loki endpoint for Serilog |
| `OTEL_ENDPOINT` | OpenTelemetry collector endpoint |
| `OPP_DEFAULT_CORS` | CORS allowed origins JSON array |
| `OPP_GE_SUBGRAPH_URL` | Game Engine subgraph URL for fusion compose |
| `OPP_UM_SUBGRAPH_URL` | User Management subgraph URL for fusion compose |
| `OPP_GRAPHQL_GATEWAY` | Fusion Gateway URL; the web app calls it directly |

### Server access

Deploys log in over SSH with a key, never a password (OPP-28). Each server has a `deploy` user in the `docker`
group with one ed25519 key, used only by GitHub Actions. Password login is off and root can only log in with a
key (`/etc/ssh/sshd_config.d/10-opp-hardening.conf`). Both servers run `fail2ban` for SSH and
`unattended-upgrades` for security updates (OPP-29).

To set up a new server:

```bash
adduser --disabled-password --gecos "" deploy && usermod -aG docker deploy
install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
echo "<public key>" > /home/deploy/.ssh/authorized_keys
chown deploy:deploy /home/deploy/.ssh/authorized_keys && chmod 600 /home/deploy/.ssh/authorized_keys
```

Membership in the `docker` group is equivalent to root on that server, so keep the key only in the repository secrets.

## Nginx Configuration

The public server runs Nginx as a reverse proxy with SSL termination via Let's Encrypt.

### Domains

| Domain | Proxies To | WebSocket |
|--------|-----------|-----------|
| `openplanningpoker.com` | `localhost:9010` | No |
| `app.openplanningpoker.com` | `localhost:10000` | Yes (`/graphql`) |
| `gateway.openplanningpoker.com` | `localhost:10010` | Yes (`/graphql`) |
| `ge.openplanningpoker.com` | `localhost:9091` | Yes (`/graphql`) |
| `usermanagement.openplanningpoker.com` | `localhost:9090` | Yes (`/graphql`) |

### SSL Certificate Setup

```bash
sudo certbot --nginx -d openplanningpoker.com \
                        -d app.openplanningpoker.com \
                        -d gateway.openplanningpoker.com \
                        -d ge.openplanningpoker.com \
                        -d usermanagement.openplanningpoker.com
```

Reference nginx configuration files are in `nginx-config/`.

### Security Headers

All nginx configurations include:

- `Strict-Transport-Security` (HSTS) with `max-age=31536000; includeSubDomains; preload`
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `Referrer-Policy: strict-origin-when-cross-origin`
- Gzip compression for text-based responses
- HTTP/2 enabled
- Automatic HTTP → HTTPS redirection
- `www` → non-www canonical redirect

## Firewall Configuration

### Public Server (162.55.213.9)

Allow inbound: HTTP (80), HTTPS (443), SSH (22, restricted)

### Private Server (91.99.28.247)

Allow inbound: PostgreSQL (5432) and Redis (6379) **only from** `162.55.213.9`. All other inbound traffic blocked.

## Health Checks

| Service | Endpoint | Description |
|---------|----------|-------------|
| Game Engine | `/_health` | Database + Redis connectivity |
| All services | `/graphql` | GraphQL schema available |

## Rollback Procedure

```bash
# Pull a specific previous build
docker pull ghcr.io/bokunda/<service>:<build-number>

# Stop and remove current container
docker rm -f <container-name>

# Re-run with previous build
docker run -d --name <container-name> --network oppnetwork -p <port>:8080 ghcr.io/bokunda/<service>:<build-number>
```

GitHub Actions build numbers can be found in the repository's Packages section.
