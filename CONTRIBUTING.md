# Contributing to Open Planning Poker

Thank you for your interest in contributing! 🎉

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you agree to uphold this code.

## How to Contribute

### Reporting Bugs

1. Check [existing issues](https://github.com/bokunda/open-planning-poker/issues) to avoid duplicates.
2. Open a new issue using the **Bug Report** template.
3. Include:
   - Clear description
   - Steps to reproduce
   - Expected vs actual behavior
   - Browser/OS version
   - Screenshots if applicable

### Suggesting Features

1. Open a new issue with the **Feature Request** label.
2. Describe the feature and why it's valuable.
3. Discuss with maintainers before implementing.

### Pull Request Workflow

1. **Fork** the repository.
2. **Create a branch** from `main`:
   ```bash
   git checkout -b feature/my-feature
   ```
3. **Make your changes** following the coding standards below.
4. **Write or update tests** for your changes.
5. **Run the test suite** to ensure nothing is broken.
6. **Commit** using [Conventional Commits](https://www.conventionalcommits.org/):
   ```bash
   git commit -m "feat: add hidden votes feature"
   git commit -m "fix: correct CORS middleware ordering"
   git commit -m "docs: add deployment guide"
   git commit -m "chore: update dependencies"
   ```
7. **Push** and open a **Pull Request** against `main`.

### Branch Naming

| Prefix | Purpose |
|--------|---------|
| `feature/` | New features |
| `fix/` | Bug fixes |
| `docs/` | Documentation changes |
| `chore/` | Maintenance, dependencies |
| `refactor/` | Code restructuring |

## Development Environment

See [docs/setup-and-development.md](docs/setup-and-development.md) for full setup instructions.

### Quick Start

```bash
git clone https://github.com/bokunda/open-planning-poker.git
cd open-planning-poker
docker compose up -d opp-db OpenPlanningPoker.Cache

# Start backend services (each in a separate terminal)
cd backend/open-planning-poker-api-gateway && dotnet run
cd backend/open-planning-poker-game-engine/src/OpenPlanningPoker.GameEngine.GraphQL && dotnet run
cd backend/open-planning-poker-user-management/src/OpenPlanningPoker.UserManagement.GraphQL && dotnet run
cd backend/open-planning-poker-graphql-gateway && dotnet run

# Start frontend
cd frontend/open-planning-poker-web-app && npm install && npm start
```

## Coding Standards

### .NET / C&#35;

- Follow [Clean Architecture](docs/architecture.md) layering: Domain → Application → Infrastructure → Presentation
- **Domain layer** must have zero external dependencies
- Use **MediatR** for commands and queries
- Use **FluentValidation** for input validation
- Use **AutoMapper** for object mapping
- Use **primary constructors** (C# 12+ feature)
- Follow standard C# naming conventions (PascalCase for public, camelCase for private)

### Angular / TypeScript

- Use **standalone components** where possible
- Use **SCSS** for styling with Angular Material
- Type-safe GraphQL via `graphql-codegen` generated types
- Lazy-load routes for better performance
- Use `inject()` function for dependency injection (modern pattern)

### GraphQL

- HotChocolate schema-first via code
- Fusion subgraph patterns for schema stitching
- Always regenerate fusion compose after schema changes (see `generate-schema.ps1`)

### Tests

Test projects are organized by layer:

- `*.Domain.UnitTests` — Pure domain logic
- `*.GraphQL.Tests` — API integration (Testcontainers for PostgreSQL)
- `*.Architecture.Tests` — NetArchTest rules

## Pull Request Checklist

Before submitting, ensure:

- [ ] Code follows project conventions
- [ ] Tests pass (`dotnet test` and `npm test`)
- [ ] GraphQL schema changes are reflected in gateway fusion compose
- [ ] Types are regenerated (`npm run generate-types` for frontend)
- [ ] No hardcoded secrets or IP addresses
- [ ] Documentation updated if needed
- [ ] PR description clearly describes the change

## Getting Help

- **Questions?** Open a [GitHub Discussion](https://github.com/bokunda/open-planning-poker/discussions)
- **Bugs?** Open an [Issue](https://github.com/bokunda/open-planning-poker/issues)
- **Security?** See [SECURITY.md](SECURITY.md)

## License

By contributing, you agree that your contributions will be licensed under the [GNU General Public License v3.0](LICENSE).
