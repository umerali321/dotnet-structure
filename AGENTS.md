# AGENTS.md

Instructions for any AI coding agent (Claude Code, Cursor, Copilot, Codex CLI, Windsurf, etc.) working in this repository. Read this file fully before making any change. Do not restructure, rename, or replace established patterns unless the user explicitly asks for it.

## What this is

`SkillsetsBackend` — a .NET 10 Clean Architecture Web API skeleton. As of now it is still a **bare skeleton**: infrastructure and wiring are in place, no domain entities/features exist yet.

## Non-negotiable facts (do not "fix" these)

- **Target framework is `net10.0`**, set once in `Directory.Build.props`. Requires Visual Studio 17.14+/18.0+ with .NET 10 tooling — if VS reports NETSDK1045, that's a VS/tooling problem to fix, not a reason to downgrade the target framework. Do not change it without being asked.
- **`Microsoft.OpenApi` is pinned to an explicit version in `SkillsetsBackend.API.csproj`** (currently 2.9.0) because `Microsoft.AspNetCore.OpenApi` 10.x transitively pulls `Microsoft.OpenApi` 2.0.0, which has a known high-severity CVE (NU1903). Keep this explicit override; don't remove it even if it looks redundant.
- **API documentation is Scalar, not Swagger.** `Microsoft.AspNetCore.OpenApi` generates the spec, `Scalar.AspNetCore` serves the UI at `/scalar/v1`. Never add `Swashbuckle.AspNetCore` back.
- **`Microsoft.OpenApi` 2.x uses the flat `Microsoft.OpenApi` namespace**, not `Microsoft.OpenApi.Models`. Security scheme references use `OpenApiSecuritySchemeReference`, not the old `.Reference =` pattern. `OpenApiDocument.Security` (not `SecurityRequirements`) holds global security requirements. See the document transformer in `Program.cs` for the working pattern.
- **`AV0029`/`AV0030` are suppressed in `SkillsetsBackend.API.csproj`** — the `Asp.Versioning.Mvc.ApiExplorer` 10.x analyzer suggests `AddApiVersioning().AddOpenApi()` and `MapOpenApi().WithDocumentPerVersion()`, but neither method exists in any published Asp.Versioning package (verified by reflection). Don't "fix" the code to use them — they don't compile. Revisit only if a future Asp.Versioning release actually ships that API.
- **Naming is `SkillsetsBackend.*`** across every project, namespace, and assembly (`SkillsetsBackend.API`, `.Application`, `.Domain`, `.Infrastructure`, `.Shared`). Keep it consistent for anything new.
- **CORS is intentionally wide open** (`AllowAnyOrigin/Method/Header`, policy `AllowAll` in `Program.cs`). Don't narrow it unless asked.
- **No secrets or connection info in code, ever.** The SQL Server connection string lives only in `ConnectionStrings:DefaultConnection` in `appsettings.json` / environment-specific config / `ConnectionStrings__DefaultConnection` env var. `Infrastructure/DependencyInjection.cs` fails fast with a clear error if it's missing. Never hardcode a server, database, username, or password anywhere.
- **`FluentValidation.AspNetCore` is banned** (deprecated by its own maintainers) — validators register via `FluentValidation.DependencyInjectionExtensions` in `Application/DependencyInjection.cs`.

## Layer dependency rule (enforced by project references — do not violate)

```
Domain          -> Shared
Application     -> Domain, Shared
Infrastructure  -> Application, Domain, Shared
API             -> Application, Infrastructure, Domain, Shared
```

Domain never references anything above it. Application never references Infrastructure or API. If a change seems to need a reference that violates this direction, the design is wrong — stop and reconsider, don't force the reference.

## Solution layout

```
SkillsetsBackend.slnx
Directory.Build.props        common TargetFramework/Nullable/ImplicitUsings for every project
src/
  API/            Controllers, Program.cs (composition root), Middleware/, appsettings*.json
  Application/    Common/Exceptions, Common/Interfaces, DependencyInjection.cs — use-case layer, no framework deps
  Domain/         Common/ (BaseEntity, IAggregateRoot) — zero dependencies on other layers
  Infrastructure/ Persistence/ApplicationDbContext.cs, Options/JwtSettings.cs, DependencyInjection.cs
  Shared/         Common/Result.cs, Common/PaginatedList.cs — cross-cutting kernel, zero dependencies
tests/
  UnitTests/         targets Domain + Application
  IntegrationTests/  targets API + Infrastructure
```

Each layer with wiring exposes one `AddXxx(IServiceCollection ...)` extension in its own `DependencyInjection.cs`, called from `Program.cs`. Follow that pattern for new wiring rather than adding services directly in `Program.cs`.

## Before making a change

1. Find an existing file that does something similar and match its pattern (exception handling, DI registration, folder placement).
2. Confirm which layer the change belongs to using the dependency rule above.
3. Run `dotnet build` (must be 0 warnings, 0 errors) before considering a change done.
4. Don't add packages that duplicate something already wired (e.g. another logging, DI, or API-docs library).
5. Don't touch `Directory.Build.props`, package versions, or the rename/Scalar/CORS decisions above without the user asking.

## Commands

```bash
dotnet build
dotnet test
dotnet run --project src/API      # Scalar UI at /scalar/v1 in Development
```
