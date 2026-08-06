# SkillsetsBackend

A .NET 9 solution scaffolded with Clean Architecture: Domain, Application, Infrastructure, and API layers, separated by dependency direction (API → Application/Infrastructure → Domain, all → Shared).

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

This is a bare skeleton: the projects, references, and base infrastructure (EF Core + SQL Server, JWT bearer authentication, FluentValidation, Serilog, OpenAPI + Scalar, API versioning, global exception handling) are wired and build cleanly, but no domain entities or features have been implemented yet.

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
