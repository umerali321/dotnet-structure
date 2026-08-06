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

Base infrastructure (EF Core + SQL Server, JWT bearer authentication, FluentValidation, Serilog, OpenAPI + Scalar, API versioning, global exception handling) and a complete SuperAdmin authentication module are wired and build cleanly. No other domain entities/features exist yet, and Company Admin / Employee accounts are intentionally not implemented.

## Authentication

There is exactly one built-in SuperAdmin account, defined in configuration (`SuperAdmin` section in `appsettings.json`) rather than the database — it always exists even before any database is connected. Dev default: `superadmin@skillsetsbackend.local` / `SuperAdmin@123` (change this — via user secrets or environment variables, never commit real credentials — before deploying anywhere real).

| Endpoint | Auth | Description |
|---|---|---|
| `POST /api/v1/auth/login` | Anonymous | Validates SuperAdmin credentials, returns an access token (30 min) + refresh token (7 days) |
| `POST /api/v1/auth/refresh` | Anonymous | Exchanges a valid refresh token for a new access + refresh token pair (rotates and revokes the old one) |
| `POST /api/v1/auth/logout` | Anonymous | Revokes a refresh token |
| `GET /api/v1/auth/me` | Bearer (SuperAdmin) | Returns the authenticated identity's claims — proves token validation works |

Refresh tokens are stored in memory for now (lost on restart, not shared across instances) via `InMemoryRefreshTokenRepository` — a deliberate placeholder. The entity, EF Core configuration, and `DbSet` are already in place and migration-ready; swapping in a real database only requires writing an EF-Core-backed `IRefreshTokenRepository` and changing one DI registration line (see `AGENTS.md` for details) — no other auth code changes.

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
