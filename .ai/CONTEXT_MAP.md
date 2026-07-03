# Context Map

This file is the fast navigation map for OpenStore. Use it as a repo GPS before loading large context.

It is not the source of truth. When exact behavior matters, read the referenced docs and code.

---

## 1. What OpenStore Is

OpenStore is a mobile-first, multi-tenant commerce API.

MVP direction:

1. Register user.
2. Create tenant.
3. Create store.
4. Create products/categories.
5. Publish catalog.
6. Build cart.
7. Checkout through WhatsApp.

Current shape:

- ASP.NET Core API only. No frontend yet.
- One solution: `OpenStore.sln`.
- One API project: `src/OpenStore.Api`.
- One test project: `tests/OpenStore.Api.Tests`.
- Postman is the functional API testing surface.
- PostgreSQL is the runtime database.
- Development schema is created by `EnsureCreatedAsync()` for now.

---

## 2. Current Structure

```text
src/OpenStore.Api/
  Program.cs

  Common/
    Auth/
    Contracts/
    Entities/
    Errors/
    Persistence/
    Services/
    Time/
    Validation/

  Tenancy/
    Controllers/
    Contracts/
    Dtos/
    Exceptions/
    Models/
    Services/

  Categories/
    Controllers/
    Contracts/
    Dtos/
    Exceptions/
    Models/
    Services/

  Products/
    Controllers/
    Contracts/
    Dtos/
    Exceptions/
    Models/
    Services/

  Stores/
    Controllers/
    Contracts/
    Dtos/
    Exceptions/
    Models/
    Services/

tests/OpenStore.Api.Tests/
  Common/
  Tenancy/
  Categories/
  Products/
  Stores/

postman/
  OpenStore.postman_collection.json
  environments/local.postman_environment.json
```

Feature folders own their HTTP contracts, service contract, service implementation, models, DTOs, and business exceptions.

---

## 3. Feature Folder Pattern

Use `Stores` as the current reference implementation.

```text
Stores/
  Controllers/
    StoresController.cs       # HTTP only
  Contracts/
    IStoreService.cs          # Controller-facing service contract
  Dtos/
    CreateStoreRequest.cs     # API input
    UpdateStoreRequest.cs     # API input
    StoreResponse.cs          # Read/update output
  Exceptions/
    DuplicateStoreSlugException.cs
    StoreNotFoundException.cs
    TenantNotFoundException.cs
    TenantOwnerRequiredException.cs
  Models/
    Store.cs                  # EF entity / business model
  Services/
    StoreService.cs           # Business rules and use-case flow
```

Tests mirror source folders:

```text
tests/OpenStore.Api.Tests/Stores/
  Controllers/StoresControllerTests.cs
  Services/StoreServiceTests.cs
```

When adding a feature, copy this shape first. Do not invent new folders unless there is a clear reason.

---

## 4. Read These First

For most feature work, read in this order:

1. `.ai/CONTEXT_MAP.md`
2. `docs/ARCHITECTURE.md`
3. `docs/PROJECT_STRUCTURE.md`
4. `.ai/skills/create-feature-slice/SKILL.md`
5. Reference feature source, usually `src/OpenStore.Api/Stores/`
6. Reference feature tests, usually `tests/OpenStore.Api.Tests/Stores/`
7. `postman/OpenStore.postman_collection.json` when endpoints change

For validation/error changes, read:

- `src/OpenStore.Api/Common/Validation/`
- `src/OpenStore.Api/Common/Errors/`
- `src/OpenStore.Api/Program.cs`
- `src/OpenStore.Api/Common/Persistence/AppDbContext.cs`

For persistence changes, read:

- `src/OpenStore.Api/Common/Persistence/AppDbContext.cs`
- `src/OpenStore.Api/Common/Persistence/Repository.cs`
- `src/OpenStore.Api/Common/Persistence/UnitOfWork.cs`
- `src/OpenStore.Api/Common/Persistence/ModelBuilderConfigurationExtensions.cs`
- `src/OpenStore.Api/Common/Persistence/SoftDeleteModelBuilderExtensions.cs`
- affected model files and their feature's `Persistence/` configuration class

---

## 5. Architecture Rules

Keep the architecture small and explicit:

- Controllers handle HTTP only.
- Controllers depend on feature contracts (`IStoreService`, `ITenantService`).
- Services own business rules, authorization checks, mapping, and use-case flow.
- Repositories do generic data access only.
- `UnitOfWork` saves changes.
- `AppDbContext` owns EF mappings, entity metadata, and entity validation before save.
- `EntityServiceBase<TEntity>` is only a base class for feature services with persistence helpers.
- There is no generic `IEntityService<TEntity>` or concrete `EntityService<TEntity>`.
- Common CRUD signatures live in generic contracts: `ICrudService<TResponse, TCreateRequest, TUpdateRequest>` for root entities, `IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>` for child entities.
- Feature interfaces inherit those generic contracts and declare only feature-specific methods.
- `CreateAsync` normally returns the same response DTO used by `Get/Update`. Create-specific DTOs are allowed only when they carry genuinely different data.
- Do not expose EF entities from APIs.
- Public API uses `PublicId`, slugs, or public identifiers. Do not expose internal numeric `Id`.
- Persisted entities use `long Id` as internal PK and `Guid PublicId` as public identifier.
- Tenant/store IDs from request bodies are not trusted.
- Field validation is attribute-based: `[Required]`, `[StringLength]`, `[Slug]`, `[RequiredGuid]`.
- API validation converts ModelState into `ModelValidationException` in `Program.cs`.
- Entity validation runs in `AppDbContext` before save.
- Business errors use typed exceptions that extend `OpenStoreException`.
- Validation errors use `ModelValidationException` with field-level `errors`.
- Soft delete is built into `BaseEntity`: `IsActive`, `DeletedAtUtc`, `MarkAsDeleted(utcNow)`, `Restore()`.
- Soft-delete query filters are applied centrally via `ApplySoftDeleteQueryFilters()`, not per-entity `HasQueryFilter`.
- Entity Fluent API mappings live in feature-level `IEntityTypeConfiguration<T>` classes inside each feature's `Persistence/` folder.
- `AppDbContext.OnModelCreating` applies configurations via `modelBuilder.ApplyOpenStoreConfigurations()`.
- New entities must create an `IEntityTypeConfiguration<T>` inside their feature folder and register it in `ModelBuilderConfigurationExtensions`.

---

## 6. Do Not Do This

Do not add these without explicit approval:

- New API projects.
- Domain/Application/Infrastructure split.
- Microservices.
- `Modules/` folder.
- One DbContext per feature.
- One repository per entity when `IRepository<TEntity>` is enough.
- EF configuration files before `AppDbContext.OnModelCreating` becomes hard to read.
- Generic CRUD/ABM framework with request/response generics.
- Recreating `IEntityService<TEntity>` or `EntityService<TEntity>`.
- Recreating `BaseTenantEntity` or `ISoftDeleteEntity`.
- Adding create-specific response DTOs when the normal response DTO carries the same data.
- Recreating `TenantValidator`, `StoreValidator`, `SlugValidator`, or `ModelValidator`.
- Field-specific validation exceptions like `StoreNameValidationException`.
- `WebApplicationFactory`, SQLite, Docker, or Testcontainers unless explicitly requested.
- Public setters or public helpers only for tests.
- `InternalsVisibleTo` for testing internal implementation details.
- Repeating `HasQueryFilter(x => x.IsActive)` per entity configuration.
- Adding entity mappings directly inside `AppDbContext.OnModelCreating`.

---

## 7. How To Add A New Feature

Use `Stores` as the working template.

1. Pick the owning feature folder, for example `Products/` or `Categories/`.
2. Create the standard folders:

```text
<Feature>/
  Controllers/
  Contracts/
  Dtos/
  Exceptions/
  Models/
  Services/
```

3. Add a feature service contract, for example `IProductService`.
4. Add a feature service, for example `ProductService`.
5. Inherit `EntityServiceBase<TEntity>` only if persistence helpers are useful.
6. Keep business rules explicit in the feature service.
7. Use DTO attributes for common field validation.
8. Create an `IEntityTypeConfiguration<T>` class inside the feature's `Persistence/` folder and register it in `ModelBuilderConfigurationExtensions.ApplyOpenStoreConfigurations()`.
9. Register the concrete feature service in `Program.cs`.
10. Add tests under the mirrored test folder.
11. Add or update Postman requests.
12. Update docs/context map only if structure, commands, or public behavior changed.

For feature implementation details, follow `.ai/skills/create-feature-slice/SKILL.md`.

---

## 8. Categories and Products Reference

Categories and Products follow the same pattern as Stores.

| Aspect | Categories | Products |
|--------|-----------|----------|
| Route prefix | `api/stores/{storePublicId}/categories` | `api/stores/{storePublicId}/products` |
| Service base | `EntityServiceBase<Category>` | `EntityServiceBase<Product>` |
| Tenant-owned via | `Store.TenantId` | `Store.TenantId` |
| Soft delete | Yes (`BaseEntity`) | Yes (`BaseEntity`) |
| Slug scope | Unique per store (StoreId+Slug) | Unique per store (StoreId+Slug) |
| Category assignment | N/A | Optional `CategoryId` (nullable FK) |
| Cross-store access? | Throws not-found | Throws not-found |

Reference source files:

- `src/OpenStore.Api/Categories/`
- `src/OpenStore.Api/Products/`
- `tests/OpenStore.Api.Tests/Categories/`
- `tests/OpenStore.Api.Tests/Products/`

## 9. Stores Reference Checklist

Before adding another tenant/store-owned feature, inspect:

- `src/OpenStore.Api/Stores/Controllers/StoresController.cs`
- `src/OpenStore.Api/Stores/Contracts/IStoreService.cs`
- `src/OpenStore.Api/Stores/Services/StoreService.cs`
- `src/OpenStore.Api/Stores/Models/Store.cs`
- `src/OpenStore.Api/Stores/Dtos/`
- `src/OpenStore.Api/Stores/Exceptions/`
- `tests/OpenStore.Api.Tests/Stores/Controllers/StoresControllerTests.cs`
- `tests/OpenStore.Api.Tests/Stores/Services/StoreServiceTests.cs`

Pay attention to:

- Tenant lookup by `Tenant.PublicId`.
- Authorization through `ICurrentUserContext` and tenant membership.
- Slug uniqueness inside tenant scope.
- Public IDs in API responses and routes.
- Soft delete behavior.
- Unit tests with mocks/fakes, not integration infrastructure.

---

## 10. Validation Commands

Run from repo root:

```powershell
# Build
dotnet build OpenStore.sln

# Tests
dotnet test OpenStore.sln

# Run API locally
dotnet run --project src/OpenStore.Api

# Generate local dev JWT
scripts/create-dev-token.ps1
```

Postman local base URL:

```text
https://localhost:5001
```

If schema changed while using `EnsureCreatedAsync()`, recreate the local development database before manual Postman testing.

---

## 11. Keep This Map Fresh

Update this file only when one of these changes:

- Repo structure.
- Standard commands.
- Feature folder pattern.
- Architecture rules.
- Validation/error strategy.
- Testing strategy.
- Current reference feature.

Keep it short, practical, and actionable.
