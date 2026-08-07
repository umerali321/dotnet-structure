# SkillsetsBackend

A .NET 10 solution scaffolded with Clean Architecture: Domain, Application, Infrastructure, and API layers, separated by dependency direction (API → Application/Infrastructure → Domain, all → Shared).

## Solution structure

```
SkillsetsBackend.slnx
src/
  API/             ASP.NET Core Web API — controllers, middleware, Scalar API docs, versioning, composition root (Program.cs)
  Application/     Use cases — commands, queries, DTOs, validators, application interfaces (no framework dependencies)
  Domain/          Entities, value objects, enums, domain interfaces and events (no dependencies on other layers)
  Infrastructure/  EF Core DbContext, JWT auth wiring, external service implementations
  Shared/          Cross-cutting kernel types (Result, PaginatedList) with no dependencies on any other layer
tests/
  UnitTests/         xUnit — targets Domain and Application
  IntegrationTests/  xUnit — targets API and Infrastructure
docs/              Architecture decision records and supplementary documentation
```

## Status

Base infrastructure (EF Core + SQL Server, JWT bearer authentication, FluentValidation, Serilog, OpenAPI + Scalar, API versioning, global exception handling) and a complete authentication + multi-company-context module are wired, tested against the real `SoftSkillSet` database, and build cleanly. No other domain entities/features exist yet.

## Authentication

Two kinds of identity:

- **SuperAdmin** — exactly one, defined in configuration (`SuperAdmin` section in `appsettings.json` / user secrets), never stored in the database. Dev default: `superadmin@skillsetsbackend.local` / `SuperAdmin@123` (replace via user secrets/environment variables before deploying anywhere real — never commit real credentials).
- **Real users** — authenticated against the existing `Users` table. Legacy passwords in this database are short plaintext values (not cryptographic hashes), so verification is a direct comparison — this matches the real data format and is documented in `AGENTS.md`.

| Endpoint | Auth | Description |
|---|---|---|
| `POST /api/v1/auth/login` | Anonymous | Validates credentials (SuperAdmin or a real user), returns an access token (30 min) + refresh token (7 days), plus the user's role and available companies |
| `POST /api/v1/auth/switch-company` | Bearer | Switches the active company for the session (validates membership first), returns a new token pair scoped to that company |
| `POST /api/v1/auth/refresh` | Anonymous | Exchanges a valid refresh token for a new access + refresh token pair, preserving company context |
| `POST /api/v1/auth/logout` | Anonymous | Revokes a refresh token |
| `GET /api/v1/auth/me` | Bearer | Returns the authenticated identity's claims (id, email, role, current company) |

**Multi-company users**: on login, if a user has exactly one active company (via `UserCompanyRoles`), it's auto-selected. With zero or multiple companies, the token has no company selected (role `"Unassigned"`) and the login response lists the available companies — the client calls `/switch-company` to pick one.

Refresh tokens are now stored in the real database (`RefreshTokens` table, added via an additive-only migration — no existing table was touched).

## Getting started

```bash
dotnet restore
dotnet build
dotnet test
```

Configure `ConnectionStrings:DefaultConnection` and the `Jwt` section in `src/API/appsettings.json` (or user secrets) before running:

```bash
dotnet run --project src/API
```

Scalar API reference UI is available at `/scalar` when running in the `Development` environment. A liveness endpoint is exposed at `/health`.

## Adding a feature

1. Add entities/value objects to `Domain`.
2. Add commands/queries, DTOs, and FluentValidation validators to `Application`.
3. Add EF Core entity configurations and any external integrations to `Infrastructure`.
4. Add controllers to `API`.
5. Add an initial EF Core migration:
   ```bash
   dotnet ef migrations add InitialCreate --project src/Infrastructure --startup-project src/API
   ```
6. Write unit tests (`tests/UnitTests`) and integration tests (`tests/IntegrationTests`).
