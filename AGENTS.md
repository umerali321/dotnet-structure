# AGENTS.md

Instructions for any AI coding agent (Claude Code, Cursor, Copilot, Codex CLI, Windsurf, etc.) working in this repository. Read this file fully before making any change. Do not restructure, rename, or replace established patterns unless the user explicitly asks for it.

## What this is

`SkillsetsBackend` — a .NET 10 Clean Architecture Web API sitting in front of an existing, live SQL Server database ("SoftSkillSet") with ~145K real users. Base infrastructure (EF Core, JWT, Scalar, Serilog, CORS, versioning) plus a complete authentication + multi-company-context module and a Student Management module are built, operating against the real legacy data (see "Authentication module" and "Student Management module" below).

## Non-negotiable facts (do not "fix" these)

- **Target framework is `net10.0`**, set once in `Directory.Build.props`. Requires Visual Studio 17.14+/18.0+ with .NET 10 tooling — if VS reports NETSDK1045, that's a VS/tooling problem to fix, not a reason to downgrade the target framework. Do not change it without being asked.
- **`Microsoft.OpenApi` is pinned to an explicit version in `SkillsetsBackend.API.csproj`** (currently 2.9.0) because `Microsoft.AspNetCore.OpenApi` 10.x transitively pulls `Microsoft.OpenApi` 2.0.0, which has a known high-severity CVE (NU1903). Keep this explicit override; don't remove it even if it looks redundant.
- **API documentation is Scalar, not Swagger.** `Microsoft.AspNetCore.OpenApi` generates the spec, `Scalar.AspNetCore` serves the UI at `/scalar/v1`. Never add `Swashbuckle.AspNetCore` back.
- **`Microsoft.OpenApi` 2.x uses the flat `Microsoft.OpenApi` namespace**, not `Microsoft.OpenApi.Models`. Security scheme references use `OpenApiSecuritySchemeReference`, not the old `.Reference =` pattern. `OpenApiDocument.Security` (not `SecurityRequirements`) holds global security requirements. See the document transformer in `Program.cs` for the working pattern.
- **`AV0029`/`AV0030` are suppressed in `SkillsetsBackend.API.csproj`** — the `Asp.Versioning.Mvc.ApiExplorer` 10.x analyzer suggests `AddApiVersioning().AddOpenApi()` and `MapOpenApi().WithDocumentPerVersion()`, but neither method exists in any published Asp.Versioning package (verified by reflection). Don't "fix" the code to use them — they don't compile. Revisit only if a future Asp.Versioning release actually ships that API.
- **Naming is `SkillsetsBackend.*`** across every project, namespace, and assembly (`SkillsetsBackend.API`, `.Application`, `.Domain`, `.Infrastructure`, `.Shared`). Keep it consistent for anything new.
- **CORS is intentionally wide open** (`AllowAnyOrigin/Method/Header`, policy `AllowAll` in `Program.cs`). Don't narrow it unless asked.
- **No secrets or connection info in code, ever.** The SQL Server connection string lives only in `ConnectionStrings:DefaultConnection` in `appsettings.json` / environment-specific config / `ConnectionStrings__DefaultConnection` env var. `Infrastructure/DependencyInjection.cs` fails fast with a clear error if it's missing. Never hardcode a server, database, username, or password anywhere.
- **`FluentValidation.AspNetCore` is banned** (deprecated by its own maintainers) — validators register via `FluentValidation.DependencyInjectionExtensions` in `Application/DependencyInjection.cs`. There is no automatic validation pipeline (no MediatR, no action filter) — every command handler manually calls `IValidator<TCommand>.ValidateAsync` at the top of `Handle` and throws `Application.Common.Exceptions.ValidationException` on failure. Follow this same pattern for new commands; don't introduce MediatR to "fix" it.
- **No MediatR anywhere.** "Commands" are plain record types, "Handlers" are plain classes with a `Handle(...)` method injected directly into controllers via DI (see `Application/Auth/Commands/*` and `API/Controllers/AuthController.cs`). Don't add MediatR or `IRequest`/`IRequestHandler` — this is a deliberate, existing pattern, not an oversight.

## Authentication module

A complete JWT authentication + multi-company-context module exists at `Application/Auth/*`, `Infrastructure/Auth/*`, `Domain/Identity/*`, `API/Controllers/AuthController.cs`, authenticating against the real `SoftSkillSet` database. Read this before touching any of it.

### SuperAdmin — config-based, not in the database

Exactly one SuperAdmin exists. Its identity lives in configuration (`SuperAdmin:Id`, `SuperAdmin:Email`, `SuperAdmin:PasswordHash` — a real PBKDF2 hash via `Microsoft.AspNetCore.Identity.PasswordHasher<T>`) and is validated by `Infrastructure/Auth/SuperAdminAuthenticator.cs`, checked *before* any database lookup in `LoginCommandHandler`. Never add a SuperAdmin database row or move these credentials into `ApplicationDbContext`. Dev default: `superadmin@skillsetsbackend.local` / `SuperAdmin@123` — replace via user-secrets/env vars before any real deployment, never commit real credentials.

### Real users — existing `Users`/`UserCompanyRoles`/`Companies`/`Roles` tables

- **Legacy password verification is a direct comparison, not a hash check — this is intentional, not a bug.** `Users.PasswordHash` (despite its name) holds plaintext values 2-10 characters long (94.5% are exactly 4-digit numeric PINs) across all 145,084 users; `UserCredentials` is fully unused (0 rows) today. `Infrastructure/Auth/LegacyCredentialVerifier.cs` does a fixed-time string comparison against whichever value `IUserDirectory.FindByIdentifierAsync` returns (`UserCredentials` first if ever populated, else `Users.PasswordHash`). **Never replace this with real hash verification** — it would reject every real user. Do not change stored values either (`Do not change legacy passwords` is an explicit product requirement, not just caution).
- **Login identifier**: `Users.Email` or `Users.Username` (verified no duplicates exist in production data), matched case-sensitively via SQL. `Companies`, `Roles`, `UserCredentials` (`Domain/Identity/*`) remain **read-only entities mapping to pre-existing tables** — private setters, no public constructor. `AppUser` and `UserCompanyRole` gained a controlled write path for the Student Management module (see below) — factory methods/mutators only, still no arbitrary public setters. Don't add a write path to `Company`, `Role`, or `UserCredential` without being asked.
- **A user can hold multiple active roles at the same company** (e.g. both Student and Manager) — `UserDirectory` collapses these to one entry per company, preferring the higher-privilege role (`Manager` > `Student`), so company selection is deterministic. Don't reintroduce raw per-row results without that collapse.
- **Role normalization**: `Roles.Normalize()` collapses the DB's `Admin` and `Manager` role names to the single app role `Manager` (per product requirement — Admin/Manager are the same role). The DB `Roles` lookup table also has an `FDM` value that is currently unassigned to anyone and unhandled by any policy — leave it alone unless asked.

### Company context (multi-tenant JWT claims)

- On login, `CompanyContextResolver.Resolve()` decides the outcome from the user's active `UserCompanyRoles` (respecting `IsActive`, `Company.IsActive`, and the `StartDate`/`EndDate` validity window):
  - **Exactly one active company** → auto-selected immediately; the access token carries `company_id`/`company_name` claims (`AuthClaimTypes`) and the real `Role` claim for that company.
  - **Zero or multiple active companies** → `Role` claim is the sentinel `CompanyContextResolver.UnassignedRole` ("Unassigned"), no `company_id` claim; the login response's `companies` array lists what's available. The client must call `POST /api/v1/auth/switch-company` to obtain a company-scoped token.
- **`POST /api/v1/auth/switch-company`** (`[Authorize]`, any authenticated non-SuperAdmin user): validates membership in the requested company via `IUserDirectory.GetActiveCompanyRoleAsync`, then issues a fresh token pair. SuperAdmin calling this gets a 403 (`UnauthorizedAccessException` → 403 in the exception middleware) — SuperAdmin is not scoped to one company.
- **Refresh preserves company context**: if the token being refreshed already had a company selected, `RefreshTokenCommandHandler` re-verifies that membership is still active (if it was revoked mid-session, refresh fails with 401, forcing re-login) rather than trusting the stale claim.
- **`ICurrentCompanyContext`** (`Infrastructure/Auth/CurrentCompanyContext.cs`) exposes the active `CompanyId`/`CompanyName`/`Role`/`IsSuperAdmin` from the current request's claims — inject this in any future company-scoped handler to filter queries, rather than reading claims manually.
- **`CompanyContext` authorization policy** (`Infrastructure/Authorization/CompanyContextRequirement.cs`) requires a `company_id` claim to be present (SuperAdmin always passes). Apply `[Authorize(Policy = "CompanyContext")]` to any future company-scoped controller/endpoint — this is prepared infrastructure, not yet attached to any business endpoint since none exist yet.

### Refresh tokens — now database-backed for real

`Domain/Identity/RefreshToken.cs`, its EF configuration, and `ApplicationDbContext.RefreshTokens` are backed by a real `RefreshTokens` table (added via the `InitialCreate` migration — **additive only**, no other table was touched; see the migration file's comment). `Infrastructure/Auth/RefreshTokenRepository.cs` is the active, EF-Core-backed `IRefreshTokenRepository` implementation (Scoped, not Singleton — it depends on the scoped `ApplicationDbContext`). There is no in-memory fallback anymore.

### Endpoints, JWT, roles

- `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout` — all `[AllowAnonymous]` (refresh/logout trust the refresh token itself as the credential). `POST /api/v1/auth/switch-company` and `GET /api/v1/auth/me` — `[Authorize]` (any authenticated user; `/me` returns id/email/role/companyId/companyName from claims).
- JWT: access token 30 min, refresh token 7 days (`Jwt:AccessTokenExpiryMinutes` / `Jwt:RefreshTokenExpiryDays`). Refresh rotates the token (old one revoked, linked via `ReplacedByToken`; reuse of a revoked/expired token is rejected with 401).
- **Roles**: `Domain/Identity/Roles.cs` defines `SuperAdmin`, `Manager`, `Student` (matches the real system — Company Admin/Employee were earlier placeholders and no longer exist). Authorization policy names equal the role name (`options.AddPolicy(Roles.SuperAdmin, ...)`) — reuse that pattern, plus the separate `"CompanyContext"` policy described above.
- **`dotnet-ef` is installed as a global tool** (`dotnet tool install --global dotnet-ef`) and `Microsoft.EntityFrameworkCore.Design` is referenced by both `Infrastructure` and the `API` startup project (required for the tooling to resolve the startup project) — don't remove either.

## Student Management module

CRUD + list APIs for students, built entirely on the existing `Users`/`StudentProfiles`/`UserCompanyRoles`/`Companies`/`Roles` tables — no new tables, no redesign. A student is a `Users` row with a `StudentProfiles` row and a `Student`-role `UserCompanyRoles` membership. `API/Controllers/StudentsController.cs` (`[Authorize]` only — no policy attribute; every access decision is re-derived per-request, see below), `Application/Students/*`, `Infrastructure/Students/*`, `Domain/Identity/StudentProfile.cs`.

### Authorization is never claim-based — always re-derived from the DB

The JWT's `company_id`/`Role` claims reflect only the *currently selected* company from the last `switch-company` call. A caller acting on a **different** student may need access to a company they haven't switched into (e.g. a Manager of two companies, mid-session in company A, listing students in company B), and a caller's privilege can change between requests (revoked membership). For that reason every Students endpoint ignores the cached claims for authorization and re-queries `IUserDirectory.GetActiveCompanyRolesAsync` fresh, via the shared helpers in `Application/Students/StudentAuthorization.cs`:
- `EnsureCanViewStudentAsync` / `EnsureCanManageStudentAsync` — target student's company memberships vs caller's.
- `EnsureCanManageCompanyAsync` — used by `POST /api/students` to check the caller (if Manager) actually manages the `companyId` in the request body.
- `GetManagedCompanyIdsAsync` — returns `null` for SuperAdmin (unrestricted), or the Manager's actual managed company set, used by `ListStudentsQueryHandler` to build the `restrictToCompanyIds` filter. **Never trust a client-supplied `companyId` query param as the sole restriction** — it's validated against this set (throws `UnauthorizedAccessException` → 403 if the caller doesn't manage that company).
- **Students are single-company-scoped to themselves**: `caller.DbUserId != targetUserId` is always a 403, regardless of company. A Student calling `PUT /api/students/{id}` on their own id may only change `Email`/`Phone` — `UpdateStudentCommandHandler` diffs every other field and 403s if any of them changed (see the `restrictedFieldsChanged` check).
- This is why a fresh login for a multi-company user (`Role` claim = `CompanyContextResolver.UnassignedRole`, i.e. "Unassigned") gets a generic 403 "Only SuperAdmin and company managers can list students" from every Students list call, regardless of `companyId` filter, until the client calls `POST /api/v1/auth/switch-company` — the role check at the top of `ListStudentsQueryHandler` fails identically for any company because the caller isn't yet `SuperAdmin` or `Manager`. This is expected behavior, not a bug — confirmed by testing.

### List endpoint — server-side pagination is mandatory, this table has 145K+ rows

`GET /api/students` (`StudentQueryService.ListAsync`, `Infrastructure/Students/StudentQueryService.cs`) composes one `IQueryable` (a `from...join...select` over `StudentProfiles`/`Users`, `AsNoTracking()`) with company/search/studentType/isActive filters applied via `.Where()`, `CountAsync()` for the total, then `.Skip()/.Take()` before ever materializing rows. **Never call `.ToList()`/`.ToArray()` before filtering — this defeats server-side pagination against a 145K-row table.** Per-student company lists are batch-loaded in one extra grouped query keyed by the page's `UserId`s (`LoadCompaniesAsync`) — never loop per-row (N+1). Sorting is an inlined `switch` with explicit `.OrderBy(x => x.u.FirstName).ThenBy(x => x.u.UserId)` chains directly in `ListAsync` (stable tiebreak on `UserId`) — **do not use a generic/`dynamic`-typed sort helper**; `dynamic` breaks EF Core's expression-tree-based SQL translation and silently forces full client-side evaluation. Response shape is fixed: `{"items":[],"page":1,"pageSize":50,"totalCount":144874,"totalPages":2898}` (`Shared/Common/PaginatedList.cs`), default `pageSize=50`, server-capped `MaxPageSize=200` regardless of what the client requests (`ListStudentsQueryHandler`).

### `datetime2` vs `DateTimeOffset` — apply the converter to any new legacy date column

`Users.CreatedAt/UpdatedAt`, `Companies.CreatedAt/UpdatedAt`, `StudentProfiles.CreatedAt`, `UserCompanyRoles.CreatedAt`, `UserCredentials.PasswordChangedAt` are pre-existing SQL `datetime2` columns (no offset), but their Domain properties are C# `DateTimeOffset`. EF Core's default convention assumes `DateTimeOffset` ↔ SQL `datetimeoffset` and throws `InvalidCastException` at query time (not at startup) the first time a query actually projects one of these columns. Fix is `.HasConversion(DateTimeOffsetToDateTime2Converter.Instance)` (or `NullableDateTimeOffsetToDateTime2Converter` for nullable ones) in the entity's `IEntityTypeConfiguration`, treating the stored value as UTC — see `Infrastructure/Persistence/Conversions/DateTimeOffsetToDateTime2Converter.cs` and every `*Configuration.cs` in `Infrastructure/Persistence/Configurations/`. **Do not use `.HasColumnType("datetime2")` instead** — that throws an EF Core model-validation error at startup ("does not support mapping 'DateTimeOffset' properties to 'datetime2' columns"). `StudentProfiles.UpdatedAt` is the one exception: it's a genuinely new `datetimeoffset` column (added by `AddStudentProfileAuditColumns`, EF's own default type) and does **not** need the converter — check `INFORMATION_SCHEMA.COLUMNS` before assuming a new date column needs it either way.

### Write endpoints and audit fields

`POST/PUT/PATCH/DELETE /api/students*` go through `StudentRepository` (`Infrastructure/Students/StudentRepository.cs`). `CreateStudentAsync` is a single DB transaction: insert `Users` → `SaveChangesAsync` (to get the generated `UserId`) → insert `StudentProfiles` + `UserCompanyRoles` referencing it → `SaveChangesAsync` → commit. `CreatedBy`/`UpdatedBy` are always populated from the authenticated caller's email for new writes; legacy rows predating this module have `null` `CreatedBy`/`UpdatedBy`/`UpdatedAt` — surface as `null`/"Legacy Record" in any UI, don't backfill them. Password changes (`PATCH /api/students/{id}/password`) go through the same `ILegacyCredentialVerifier`/legacy-plaintext scheme as login (see Authentication module above) — Student self-service requires `CurrentPassword` to match, Admin/Manager resets skip that check; the password value/hash is never returned in any response.

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
  API/            Controllers/ (AuthController, StudentsController), Program.cs (composition root), Middleware/, appsettings*.json
  Application/    Common/Exceptions, Common/Interfaces, Common/CallerContext, Auth/ (Commands/{Login,Refresh,Logout,SwitchCompany}, Interfaces, DTOs, AuthClaimsFactory, CompanyContextResolver), Students/ (Commands/{CreateStudent,UpdateStudent,ChangeStudentPassword,DeactivateStudent}, Queries/{ListStudents,GetStudentById,GetStudentCompanies,GetStudentRoles}, DTOs, Interfaces, StudentAuthorization), DependencyInjection.cs — use-case layer, no framework deps
  Domain/         Common/ (BaseEntity, IAggregateRoot), Identity/ (Roles, RefreshToken, AppUser, Company, Role, UserCompanyRole, UserCredential, StudentProfile) — zero dependencies on other layers
  Infrastructure/ Persistence/ (ApplicationDbContext, Configurations/, Conversions/, Migrations/), Options/ (JwtSettings, SuperAdminSettings), Auth/ (PasswordHasher, SuperAdminAuthenticator, TokenService, LegacyCredentialVerifier, UserDirectory, RefreshTokenRepository, CurrentCompanyContext), Students/ (StudentQueryService, StudentRepository), Authorization/ (CompanyContextRequirement), DependencyInjection.cs
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

# Login as the built-in SuperAdmin (dev default credentials, see "Authentication module" above)
curl -X POST http://localhost:5175/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"superadmin@skillsetsbackend.local","password":"SuperAdmin@123"}'
```

## Database — this is a live production database with real users

`ConnectionStrings:DefaultConnection` points at the real `SoftSkillSet` SQL Server database (~145K users, 618 companies) via `dotnet user-secrets` locally — never put real credentials in `appsettings.json`. Before running any EF Core migration command against it:

1. `dotnet ef migrations script` first and read the SQL — never run `database update` blind.
2. Every existing table (`Users`, `Companies`, `Roles`, `UserCompanyRoles`, `UserCredentials`, and the 12 others listed in the DB schema) must appear **only** as `IEntityTypeConfiguration` mapping to it, never as a `CreateTable` in a migration. If `dotnet ef migrations add` generates `CreateTable` for any of them (it will, the first time a fresh migration history sees them), manually strip that call from the migration's `Up()`/`Down()` before applying — keep only genuinely new tables/columns (see `InitialCreate`'s and `AddStudentProfileAuditColumns`'s comments for precedent: the latter is a hand-edited `AddColumn`-only migration adding `StudentProfiles.UpdatedAt/CreatedBy/UpdatedBy`).
3. `Company`, `Role`, `UserCredential` remain read-only by design (private setters, no public constructor) — do not add a write path to them without being explicitly asked. `AppUser` and `UserCompanyRole` have a narrow, explicit write path (factory methods + named mutators — see Student Management module above) added specifically for Student Management; don't widen it with generic public setters, and never write code that updates `Users.PasswordHash` or any other legacy column.
4. Testing writes against this live DB: use obviously-fake identifiers (e.g. an `@example-test.invalid` email domain, `ZZZTEST`-prefixed names) so test rows are unambiguous, and hard-delete them from `UserCompanyRoles` → `StudentProfiles` → `Users` (in that FK order) via `sqlcmd` immediately after testing — don't leave synthetic rows in the real data. Never modify a real user's data as a side effect of testing (e.g. testing "allowed self-update fields" on a real account) without reverting it in the same session.
