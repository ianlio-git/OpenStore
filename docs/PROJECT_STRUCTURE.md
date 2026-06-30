# Project Structure

This document explains the initial OpenStore structure.

OpenStore starts simple: one solution, one API project, one test project, one shared `Common` area, and feature folders.

Do not split code into Domain/Application/Infrastructure projects yet.
Do not create one `DbContext`, repository, or configuration class per module/entity until the code is large enough to justify it.

---

## 1. Initial Structure

Create source code only when the first feature starts.

```text
OpenStore/
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

        Services/
          EntityService.cs

        Time/
          SystemDateTimeProvider.cs

        Errors/
          GlobalExceptionHandler.cs

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
          TenantSlug.cs
        Validators/
          TenantValidator.cs
        Dtos/
          CreateTenantRequest.cs
          CreateTenantResponse.cs

  tests/
    OpenStore.Api.Tests/
      OpenStore.Api.Tests.csproj
      Tenancy/
        Controllers/
          TenantsControllerTests.cs
        Services/
          TenantServiceTests.cs
        Models/
          TenantSlugTests.cs
        Validators/
          TenantValidatorTests.cs

  postman/
    OpenStore.postman_collection.json
    environments/
      local.postman_environment.json

  docs/
  .ai/
  README.md
  AGENTS.md
  OPENCODE.md
  ANTIGRAVITY.md
  Directory.Build.props
  .editorconfig
```

---

## 2. Folder Meanings

### `Common/Contracts`

Interfaces only.

Examples:

- `IRepository.cs`
- `IEntityService.cs`
- `IUnitOfWork.cs`
- `ITenantEntity.cs`
- `ICurrentUserContext.cs`
- `IDateTimeProvider.cs`

If a file starts with `I`, it usually belongs here.

### `Common/Entities`

Base entity classes only.

Examples:

- `BaseEntity.cs`
- `BaseTenantEntity.cs`

Do not put interfaces here.

### `Common/Persistence`

Reusable EF Core persistence implementation.

Examples:

- `AppDbContext.cs`
- `Repository.cs`
- `UnitOfWork.cs`

Start with one `AppDbContext`.
Do not create `TenancyDbContext`, `CatalogDbContext`, or one context per module yet.

### `Common/Services`

Reusable generic services.

Examples:

- `EntityService.cs`

Use `EntityService<TEntity>` for simple CRUD behavior.
Use a feature-specific service, such as `TenantService`, when business rules are more than basic CRUD.

### `Common/Time`

Date/time implementations.

### `Common/Errors`

Global exception handling and API error mapping.

### `Tenancy`

Everything specific to the Tenancy feature area.

For the first slice, use small responsibility folders:

- `Controllers/`: HTTP only.
- `Services/`: business logic.
- `Contracts/`: Tenancy-specific interfaces.
- `Models/`: business/database models and value objects.
- `Validators/`: Tenancy-specific validation helpers.
- `Dtos/`: API input/output contracts.

This keeps Tenancy readable without creating separate projects or architecture layers.

---

## 3. Persistence Rules

Use dependency injection with generic persistence:

```text
IRepository<Tenant> -> Repository<Tenant>
IRepository<Product> -> Repository<Product>
IEntityService<Tenant> -> EntityService<Tenant>
IEntityService<Product> -> EntityService<Product>
IUnitOfWork -> UnitOfWork
AppDbContext -> EF Core database session
```

The repository does repeated CRUD work.
The entity service provides reusable CRUD operations above the repository.
The Unit of Work saves changes.
The DbContext is still required because EF Core needs one object that knows the database connection and tracked entities.

Do not create this at the beginning:

```text
Tenancy/Data/TenancyDbContext.cs
Tenancy/Data/TenantRepository.cs
Tenancy/Data/TenantConfiguration.cs
Tenancy/Data/TenantMembershipConfiguration.cs
```

Instead, start with:

```text
Common/Persistence/AppDbContext.cs
Common/Persistence/Repository.cs
Common/Persistence/UnitOfWork.cs
Common/Services/EntityService.cs
```

Put simple EF mappings directly inside `AppDbContext.OnModelCreating`.

Extract configuration classes only when `OnModelCreating` becomes too large.
Create a specific repository only when the generic repository cannot express a required query clearly.

---

## 4. CreateTenant Shape

The first feature should create this shape:

```text
src/
  OpenStore.Api/
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
      Services/
        EntityService.cs
      Time/
        SystemDateTimeProvider.cs
      Errors/
        GlobalExceptionHandler.cs

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
        TenantSlug.cs
      Validators/
        TenantValidator.cs
      Dtos/
        CreateTenantRequest.cs
        CreateTenantResponse.cs
```

No `TenancyDbContext`.
No `TenantRepository`.
No `TenantConfiguration`.
No separate Domain/Application/Infrastructure projects.

---

## 5. When To Split Later

Split only when there is real pressure:

- `AppDbContext.OnModelCreating` is too large: extract `TenantConfiguration`.
- Generic repository cannot express a query cleanly: create `TenantRepository`.
- `Tenancy/Models` has too many models: split by sub-feature.
- `Tenancy/Services` has too many services: split by sub-feature.
- The API project becomes too large: consider module projects through an ADR.

Do not split just because a pattern exists.

---

## 6. Test Structure

Tests mirror the production folder structure.

The only differences are:

- Tests start under `tests/OpenStore.Api.Tests`.
- Test namespaces start with `OpenStore.Api.Tests`.
- Test files end with `Tests.cs`.
- Test classes end with `Tests`.

Example:

```text
src/OpenStore.Api/Tenancy/Services/TenantService.cs
tests/OpenStore.Api.Tests/Tenancy/Services/TenantServiceTests.cs
```

```text
src/OpenStore.Api/Tenancy/Controllers/TenantsController.cs
tests/OpenStore.Api.Tests/Tenancy/Controllers/TenantsControllerTests.cs
```

```text
src/OpenStore.Api/Tenancy/Models/TenantSlug.cs
tests/OpenStore.Api.Tests/Tenancy/Models/TenantSlugTests.cs
```

```text
src/OpenStore.Api/Tenancy/Validators/TenantValidator.cs
tests/OpenStore.Api.Tests/Tenancy/Validators/TenantValidatorTests.cs
```

Namespaces follow the same logical path:

```csharp
namespace OpenStore.Api.Tenancy.Services;
```

```csharp
namespace OpenStore.Api.Tests.Tenancy.Services;
```

Do not create separate `UnitTests`, `IntegrationTests`, `Api`, or `Persistence` folders at the beginning. Use one `OpenStore.Api.Tests` project and mirror the production folder being tested.

---

## 7. Prompt To Start

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
- Tenancy/Models for Tenant, TenantMembership, and TenantSlug.
- Tenancy/Validators for TenantValidator.
- Tenancy/Dtos for CreateTenantRequest and CreateTenantResponse.
- Tests mirror the source structure under tests/OpenStore.Api.Tests.
- Test files must end with Tests.cs.

Do not create Domain/Application/Infrastructure projects.
Do not create TenancyDbContext.
Do not create TenantRepository unless the generic repository cannot solve the query.
Do not create TenantConfiguration unless AppDbContext mapping becomes too large.
Use IEntityService<TEntity> and EntityService<TEntity> for reusable CRUD.
Use TenantService for CreateTenant because it has business rules beyond CRUD.
Show the planned file structure before creating files.
```
