# Context Map

This file is the compact working index for OpenStore.

Use it to reduce repeated context loading. It is a map, not the source of truth. When a task needs exact behavior, read the referenced files and code.

---

## 1. Project Snapshot

OpenStore is a mobile-first, multi-tenant commerce platform.

Initial MVP:

1. Register user.
2. Create tenant.
3. Create store.
4. Create product.
5. Publish store and product.
6. Read public catalog.
7. Build cart.
8. Validate cart on backend.
9. Generate WhatsApp cart URL.
10. Prove tenant isolation.

Current state:

- CreateTenant feature implemented.
- Dev JWT tokens generated via `scripts/create-dev-token.ps1` (HMACSHA256, no token endpoint).
- Database schema created automatically via `EnsureCreatedAsync()` in Development mode.
- Postman `baseUrl`: `https://localhost:5001`.
- One API project `src/OpenStore.Api`, one test project `tests/OpenStore.Api.Tests`.
- Common infrastructure: `IRepository<T>`, `Repository<T>`, `IUnitOfWork`, `UnitOfWork`, `IEntityService<T>`, `EntityService<T>`, `AppDbContext`.
- Authentication: JWT Bearer with `ICurrentUserContext` abstraction (scoped, `IsAuthenticated` guard). Local dev tokens come from `scripts/create-dev-token.ps1`; production token issuing is deferred - see `AGENTS.md` section 2.1.
- Entity properties use `internal set` for encapsulation.
- Exception model: single `OpenStoreException` class with `StatusCode` and `ErrorCode`.
- All automated tests are unit tests using mocks/fakes. Integration tests with `WebApplicationFactory` are deferred.
- Do not use `InternalsVisibleTo` or widen visibility for tests unless a concrete design need is explicitly approved.

---

## 2. Read Order

For most implementation tasks:

1. `.ai/CONTEXT_MAP.md`
2. `README.md`
3. `AGENTS.md`
4. `docs/PROJECT_STRUCTURE.md`
5. `docs/ARCHITECTURE.md`
6. Applicable `.ai/skills/**/SKILL.md`
7. Target source and test files.

Do not load every document when the task is narrow and the relevant route is clear.

---

## 3. Key Documents

| File | Use for |
|---|---|
| `README.md` | Product vision, MVP, domain concepts, definition of done. |
| `AGENTS.md` | Mandatory AI rules and task workflow. |
| `docs/PROJECT_STRUCTURE.md` | Initial source layout and where files belong. |
| `docs/ARCHITECTURE.md` | Simple API architecture, request flow, repository, entity service, Unit of Work, AppDbContext. |
| `docs/TECHNOLOGY_STACK.md` | Approved technologies and forbidden defaults. |
| `docs/TESTING_STRATEGY.md` | Unit, integration, tenant isolation, adapter, and workflow tests. |
| `docs/POSTMAN_FUNCTIONAL_TESTING.md` | Postman collection rules while there is no frontend. |
| `docs/GIT_WORKFLOW.md` | Feature branch, small commit, and PR readiness rules. |
| `.ai/README.md` | How to invoke and combine AI skills. |
| `.ai/agent-startup/` | Short startup routines for Codex, OpenCode, Antigravity CLI, and generic agents. |

---

## 4. Planned Source Layout

Create this only when the first feature is implemented:

```text
OpenStore.sln
Directory.Build.props
Directory.Packages.props

src/
  OpenStore.Api/
    OpenStore.Api.csproj
    Program.cs

    Common/
      Contracts/
        IRepository.cs
        IEntityService.cs
        IUnitOfWork.cs
        ITenantEntity.cs
        ICurrentUserContext.cs
        IDateTimeProvider.cs
      Entities/
        BaseEntity.cs
        BaseTenantEntity.cs
      Persistence/
        AppDbContext.cs
        Repository.cs
        UnitOfWork.cs
      Auth/
        CurrentUserContext.cs
        JwtSettings.cs
      Services/
        EntityService.cs
      Time/
        SystemDateTimeProvider.cs
      Errors/
        GlobalExceptionHandler.cs
        OpenStoreException.cs

    Tenancy/
      Controllers/
        TenantsController.cs
      Services/
        TenantService.cs
      Contracts/
        ITenantService.cs
      Models/
        Tenant.cs
        TenantMembership.cs
      Validators/
        TenantValidator.cs
      Dtos/
        CreateTenantRequest.cs
        CreateTenantResponse.cs
      Exceptions/
        DuplicateTenantSlugException.cs
        TenantNameValidationException.cs
        TenantSlugValidationException.cs

tests/
  OpenStore.Api.Tests/
    OpenStore.Api.Tests.csproj
    Tenancy/
      Controllers/
        TenantsControllerTests.cs
      Services/
        TenantServiceTests.cs
      Validators/
        TenantValidatorTests.cs

postman/
  OpenStore.postman_collection.json
  environments/
    local.postman_environment.json
```

Do not create:

- `OpenStore.Common.csproj`
- `Modules/`
- Domain/Application/Infrastructure projects
- `TenancyDbContext`
- `TenantRepository`
- `TenantConfiguration`
- `TenantMembershipConfiguration`
- frontend
- Aspire
- ServiceDefaults
- gateway
- messaging

---

## 5. Architecture Guardrails

- Start with the smallest complete vertical slice.
- Keep a single root solution named `OpenStore.sln`.
- Keep one root `Directory.Build.props`.
- Keep one root `Directory.Packages.props` for central NuGet versions.
- Keep one API project named `src/OpenStore.Api/OpenStore.Api.csproj`.
- Keep one test project named `tests/OpenStore.Api.Tests/OpenStore.Api.Tests.csproj`.
- Tests mirror the production folder structure under `tests/OpenStore.Api.Tests`.
- Test files and classes end with `Tests`.
- Put reusable code under `src/OpenStore.Api/Common/`.
- Put Tenancy code under `src/OpenStore.Api/Tenancy/` using `Controllers`, `Services`, `Contracts`, `Models`, and `Dtos`.
- Use `IRepository<TEntity>`, `Repository<TEntity>`, `IEntityService<TEntity>`, `EntityService<TEntity>`, `IUnitOfWork`, `UnitOfWork`, and one `AppDbContext`.
- Register open generic repositories through dependency injection.
- Register open generic entity services through dependency injection.
- Do not create a repository per entity unless the generic repository cannot express the query clearly.
- Do not replace feature-specific services with `EntityService<TEntity>` when business rules exist.
- Do not make implementation members public for tests.
- Keep implementation helpers internal; test them through the public contract of the service that uses them.
- Unit tests should use interfaces, mocks, or fakes and verify behavior through public contracts.
- Interface members must not include redundant public modifiers.
- Entity properties use `internal set` (narrowest that works with single-project architecture: EF Core, service layer, and AppDbContext are all in the same assembly).
- `InternalsVisibleTo` is only for `WebApplicationFactory<Program>` - do not add it for testing internal types.
- Use `ICurrentUserContext` (registered as scoped) to resolve the authenticated user; check `IsAuthenticated` before reading claims in endpoints.
- Authentication uses JWT Bearer with `UseAuthentication` and `UseAuthorization` middleware in the correct order.
- Exceptions: single concrete `OpenStoreException` with `StatusCode`. No intermediate abstract layers. Handler reads `StatusCode` to map to Problem Details.
- Controllers extend `ControllerBase` with `[ApiController]` and `[Route]` attributes. Register with `AddControllers()`/`MapControllers()`.
- `Program.cs` uses Npgsql exclusively.
- Put simple EF mappings inside `AppDbContext.OnModelCreating`.
- Do not create EF configuration classes until `OnModelCreating` becomes hard to read.
- Do not trust `TenantId` or `StoreId` from request bodies.
- Add negative cross-tenant tests for tenant-owned behavior.
- Add or update Postman requests for changed HTTP endpoints.
- Use one feature branch per feature and small coherent commits when committing is requested.

---

## 6. First Recommended Task

Recommended prompt:

```text
Follow .ai/agent-startup/opencode.md.
Use create-feature-slice to implement CreateTenant.

Use the simple initial structure:
- One solution: OpenStore.sln.
- One Directory.Build.props at the root.
- One Directory.Packages.props at the root for central NuGet versions.
- One API project: src/OpenStore.Api.
- One test project: tests/OpenStore.Api.Tests.
- Common/Contracts for interfaces.
- Common/Entities for base entity classes.
- Common/Persistence for AppDbContext, Repository, and UnitOfWork.
- Common/Services for EntityService.
- Tenancy/Controllers for TenantsController.
- Tenancy/Services for TenantService.
- Tenancy/Contracts for ITenantService.
- Tenancy/Models for Tenant and TenantMembership.
- Tenancy/Validators for TenantValidator.
- Tenancy/Dtos for CreateTenantRequest and CreateTenantResponse.
- Tests mirror the source structure under tests/OpenStore.Api.Tests.
- Test files must end with Tests.cs.

Use IRepository<TEntity>, Repository<TEntity>, IEntityService<TEntity>, EntityService<TEntity>, IUnitOfWork, UnitOfWork, and AppDbContext.
Do not create TenancyDbContext.
Do not create TenantRepository unless the generic repository cannot solve the query.
Do not create TenantConfiguration or TenantMembershipConfiguration unless AppDbContext mapping becomes too large.
Do not create Domain/Application/Infrastructure projects.
Do not remove TenantService from CreateTenant; use it to coordinate business rules and UnitOfWork.
Controllers should depend on ITenantService.
Show the planned file structure before creating files.
```

---

## 7. Standard Commands

```powershell
# Build
dotnet build OpenStore.sln

# Run all tests
dotnet test tests/OpenStore.Api.Tests/

# Run API locally
dotnet run --project src/OpenStore.Api

# Generate a dev JWT
scripts/create-dev-token.ps1

# Generate a dev JWT with a specific user ID
scripts/create-dev-token.ps1 -UserId "00000000-0000-0000-0000-000000000001"

# Add NuGet package
dotnet add src/OpenStore.Api/OpenStore.Api.csproj package <PackageName>
```

---

## 8. Maintenance Rules

Update this file when:

- A new important folder or entry point is created.
- A command becomes the standard way to build, test, run, migrate, or deploy.
- A major architecture, infrastructure, testing, or skill decision changes.
- A folder is moved or renamed.

Keep updates concise. Prefer links and summaries over copied content.
