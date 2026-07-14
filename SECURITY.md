# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.x (main) | ✅ Active |

## Reporting a Vulnerability

If you discover a security vulnerability in Open Planning Poker, please report it responsibly.

**Do not open a public GitHub issue.**

Instead, please email the maintainer at:

📧 **bokunda@outlook.com**

### What to include

- A clear description of the vulnerability
- Steps to reproduce
- Affected versions
- Any potential impact

### Response Timeline

- **Acknowledgment:** Within 48 hours
- **Assessment & Fix:** Within 7 days (depending on severity)
- **Disclosure:** Coordinated disclosure after a fix is released

We follow a **coordinated disclosure** policy. We will work with you to understand and address the issue, and will credit you in the release notes (unless you prefer to remain anonymous).

## Security Features

### Infrastructure

| Feature | Implementation |
|---------|---------------|
| **Rate Limiting** | 100 requests/minute per IP via ASP.NET Core fixed window limiter on the API Gateway |
| **HTTPS Enforcement** | All HTTP requests redirected to HTTPS via Nginx (301 redirect) |
| **TLS** | TLS 1.2 and 1.3 with strong cipher suites (`HIGH:!aNULL:!MD5`) |
| **HSTS** | `max-age=31536000; includeSubDomains; preload` on all domains |
| **Firewall** | Private infrastructure server (PostgreSQL, Redis) restricted to application server IP only |

### HTTP Security Headers

Applied via Nginx:

| Header | Value |
|--------|-------|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains; preload` |
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Cross-Origin-Resource-Policy` | `cross-origin` |
| `X-Permitted-Cross-Domain-Policies` | `none` |
| `Content-Security-Policy` | Restrictive CSP with allowed sources for Google Analytics, Clarity, and GraphQL endpoints |

### Application Security

| Feature | Implementation |
|---------|---------------|
| **JWT Authentication** | Bearer token-based auth for API calls |
| **CORS** | Configurable allowed origins (production domains whitelisted) |
| **Fusion Gateway** | Header propagation with secure Bearer token handling |
| **GraphQL** | HotChocolate built-in protections (query depth/complexity) |
| **Input Validation** | FluentValidation on all inputs (User Management) |
| **No Personal Data Required** | Users identified by anonymous usernames — no email, no passwords, no registration |
| **Data Retention** | Usernames auto-deleted after 24 hours |

### Network

| Feature | Implementation |
|---------|---------------|
| **Private Infrastructure** | PostgreSQL and Redis on a separate server, not exposed to the internet |
| **Internal Communication** | All inter-service communication over Docker internal network |
| **WebSocket Security** | GraphQL subscriptions with same CORS and auth policies as HTTP |

## Known Limitations

- The health check endpoint (`/_health`) is publicly accessible (planned improvement)
- `AllowQueryPlan` is enabled in the Fusion Gateway for debugging (should be disabled in strict production)
- JWT audience/issuer validation is currently disabled (planned improvement)
- No CSRF protection for GraphQL mutations (mitigated by Bearer token auth via `Authorization` header)

## Dependency Management

- All .NET dependencies managed via central package management (`Directory.Packages.props`)
- Angular dependencies use caret (`^`) ranges
- Docker images pinned to specific versions (not `latest`)

We recommend enabling **Dependabot** on the repository for automated dependency update PRs.
