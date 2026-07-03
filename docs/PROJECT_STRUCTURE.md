# Project Structure

This document explains the initial OpenStore structure.

OpenStore starts simple: one solution, one API project, one test project, one shared `Common` area, and feature folders.

Do not split code into Domain/Application/Infrastructure projects yet.
Do not create one `DbContext` or repository per module/entity until the generic structure becomes insufficient. EF entity mappings live in feature-level `Persistence/` configuration classes.

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
          IUnitOfWork.cs
          ITenantEntity.cs
          ICurrentUserContext.cs
          IDateTimeProvider.cs
          ICrudService.cs
          IChildCrudService.cs

        Entities/
          BaseEntity.cs

        Persistence/
          AppDbContext.cs
          Repository.cs
          UnitOfWork.cs
          ModelBuilderConfigurationExtensions.cs
          SoftDeleteModelBuilderExtensions.cs

        Services/
          EntityServiceBase.cs

        Time/
          SystemDateTimeProvider.cs

        Validation/
          SlugAttribute.cs
          RequiredGuidAttribute.cs

        Errors/
          GlobalExceptionHandler.cs
          OpenStoreException.cs
          ModelValidationException.cs
          ProblemDetailsBuilder.cs

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
        Exceptions/
          DuplicateTenantSlugException.cs
        Persistence/
          TenantConfiguration.cs
          TenantMembershipConfiguration.cs
        Dtos/
          CreateTenantRequest.cs
          CreateTenantResponse.cs

      Categories/
        Controllers/
          CategoriesController.cs
        Services/
          CategoryService.cs
        Contracts/
          ICategoryService.cs
        Models/
          Category.cs
        Dtos/
          CreateCategoryRequest.cs
          UpdateCategoryRequest.cs
          CategoryResponse.cs
        Exceptions/
          CategoryNotFoundException.cs
          DuplicateCategorySlugException.cs
        Persistence/
          CategoryConfiguration.cs

      Products/
        Controllers/
          ProductsController.cs
        Services/
          ProductService.cs
        Contracts/
          IProductService.cs
        Models/
          Product.cs
        Dtos/
          CreateProductRequest.cs
          UpdateProductRequest.cs
          ProductResponse.cs
        Exceptions/
          ProductNotFoundException.cs
          DuplicateProductSlugException.cs
        Persistence/
          ProductConfiguration.cs

      Stores/
        Controllers/
          StoresController.cs
        Services/
          StoreService.cs
        Contracts/
          IStoreService.cs
        Models/
          Store.cs
        Dtos/
          CreateStoreRequest.cs
          StoreResponse.cs
          UpdateStoreRequest.cs
        Exceptions/
          DuplicateStoreSlugException.cs
          StoreNotFoundException.cs
          TenantNotFoundException.cs
          TenantOwnerRequiredException.cs
        Persistence/
          StoreConfiguration.cs

      Cart/
        Controllers/
          CartsController.cs
        Services/
          CartService.cs
        Contracts/
          ICartService.cs
        Models/
          Cart.cs
          CartItem.cs
        Dtos/
          AddCartItemRequest.cs
          UpdateCartItemRequest.cs
          CartItemResponse.cs
          CartResponse.cs
        Exceptions/
          CartNotFoundException.cs
          CartItemNotFoundException.cs
          ProductNotFoundException.cs
          StoreNotFoundException.cs
        Persistence/
          CartConfiguration.cs
          CartItemConfiguration.cs

  tests/
    OpenStore.Api.Tests/
      OpenStore.Api.Tests.csproj
      Common/
        Validation/
          SlugAttributeTests.cs
          RequiredGuidAttributeTests.cs
        Services/
          EntityServiceBaseTests.cs
      Tenancy/
        Controllers/
          TenantsControllerTests.cs
        Services/
          TenantServiceTests.cs
      Categories/
        Controllers/
          CategoriesControllerTests.cs
        Services/
          CategoryServiceTests.cs
      Products/
        Controllers/
          ProductsControllerTests.cs
        Services/
          ProductServiceTests.cs
      Stores/
        Controllers/
          StoresControllerTests.cs
        Services/
          StoreServiceTests.cs
      Cart/
        Controllers/
          CartsControllerTests.cs
        Services/
          CartServiceTests.cs

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
- `IUnitOfWork.cs`
- `ITenantEntity.cs`
- `ICurrentUserContext.cs`
- `IDateTimeProvider.cs`
- `ICrudService.cs`
- `IChildCrudService.cs`

If a file starts with `I`, it usually belongs here.

### `Common/Entities`

Base entity class only.

Examples:

- `BaseEntity.cs`

Do not put other interface types here.

### `Common/Persistence`

Reusable EF Core persistence implementation.

Examples:

- `AppDbContext.cs`
- `Repository.cs`
- `UnitOfWork.cs`
- `ModelBuilderConfigurationExtensions.cs`
- `SoftDeleteModelBuilderExtensions.cs`

`AppDbContext.OnModelCreating` must stay small - entity configuration classes live in each feature folder's `Persistence/` subfolder.

### `Common/Services`

Reusable generic services.

Examples:

- `EntityServiceBase.cs`

Use `EntityServiceBase<TEntity>` as an abstract base class for feature-specific services (e.g., `StoreService`) that need persistence helpers (`GetByPublicIdOrThrowAsync`, `ExistsAsync`, `FindAsync`, `Add`, `Update`, `Remove`, `SaveChangesAsync`).
Concrete services own business operations and mapping.
There is no generic `IEntityService` - `Repository<T>` is the generic data access abstraction.

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
- `Dtos/`: API input/output contracts with attribute-based validation rules.
- `Persistence/`: EF Core `IEntityTypeConfiguration<T>` classes for entity models.

This keeps Tenancy readable without creating separate projects or architecture layers.

---

## 3. Persistence Rules

Use dependency injection with generic persistence:

```text
IRepository<Tenant> -> Repository<Tenant>
IRepository<Product> -> Repository<Product>
IUnitOfWork -> UnitOfWork
AppDbContext -> EF Core database session
```

The repository does repeated CRUD work.
The Unit of Work saves changes.
The DbContext is still required because EF Core needs one object that knows the database connection and tracked entities.

Do not create this at the beginning:

```text
Tenancy/Data/TenancyDbContext.cs
Tenancy/Data/TenantRepository.cs
```

Instead, start with:

```text
Common/Persistence/AppDbContext.cs
Common/Persistence/Repository.cs
Common/Persistence/UnitOfWork.cs
Common/Services/EntityServiceBase.cs
```

Put entity-specific EF mappings inside each feature's `Persistence/` folder in dedicated `IEntityTypeConfiguration<T>` classes.
Soft-delete query filters are applied centrally — do not repeat `HasQueryFilter(x => x.IsActive)` per entity.
`AppDbContext.OnModelCreating` applies both through explicit extension methods:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyOpenStoreConfigurations();
    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

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
        IUnitOfWork.cs
        ITenantEntity.cs
        ICurrentUserContext.cs
        IDateTimeProvider.cs
      Entities/
        BaseEntity.cs
     Persistence/
        AppDbContext.cs
        Repository.cs
        UnitOfWork.cs
        ModelBuilderConfigurationExtensions.cs
        SoftDeleteModelBuilderExtensions.cs
      Services/
        EntityServiceBase.cs
      Time/
        SystemDateTimeProvider.cs
      Errors/
        GlobalExceptionHandler.cs
        ModelValidationException.cs
        ProblemDetailsBuilder.cs
      Validation/
        RequiredGuidAttribute.cs
        SlugAttribute.cs

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
      Persistence/
        TenantConfiguration.cs
        TenantMembershipConfiguration.cs
      Dtos/
        CreateTenantRequest.cs
        CreateTenantResponse.cs
```

No `TenancyDbContext`.
No `TenantRepository`.
No separate Domain/Application/Infrastructure projects.
`TenantConfiguration` and `TenantMembershipConfiguration` live in `Tenancy/Persistence/` because they belong to the feature.

---

## 5. When To Split Later

Split only when there is real pressure:

- `ModelBuilderConfigurationExtensions.ApplyOpenStoreConfigurations` is too long: consider `ApplyConfigurationsFromAssembly`.
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
src/OpenStore.Api/Common/Errors/ModelValidationException.cs
tests/OpenStore.Api.Tests/Common/Errors/ModelValidationExceptionTests.cs
```

```text
src/OpenStore.Api/Common/Errors/ProblemDetailsBuilder.cs
tests/OpenStore.Api.Tests/Common/Errors/ProblemDetailsBuilderTests.cs
```

```text
src/OpenStore.Api/Common/Validation/SlugAttribute.cs
tests/OpenStore.Api.Tests/Common/Validation/SlugAttributeTests.cs
```

```text
src/OpenStore.Api/Common/Validation/RequiredGuidAttribute.cs
tests/OpenStore.Api.Tests/Common/Validation/RequiredGuidAttributeTests.cs
```

```text
src/OpenStore.Api/Common/Services/EntityServiceBase.cs
tests/OpenStore.Api.Tests/Common/Services/EntityServiceBaseTests.cs
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
- Common/Persistence for AppDbContext, Repository, UnitOfWork, and extension methods.
- Common/Services for EntityServiceBase.
- Tenancy/Controllers for TenantsController.
- Tenancy/Services for TenantService.
- Tenancy/Contracts for ITenantService.
- Tenancy/Models for Tenant and TenantMembership.
- Tenancy/Persistence for EF Core entity configurations (TenantConfiguration, TenantMembershipConfiguration).
- Common/Validation for reusable validation attributes such as SlugAttribute and RequiredGuidAttribute.
- Tenancy/Dtos for CreateTenantRequest and CreateTenantResponse.
- Tests mirror the source structure under tests/OpenStore.Api.Tests.
- Test files must end with Tests.cs.

Do not create Domain/Application/Infrastructure projects.
Do not create TenancyDbContext.
Do not create TenantRepository unless the generic repository cannot solve the query.
Entity configurations (IEntityTypeConfiguration<T>) live in each feature's Persistence/ folder.
There is no generic IEntityService — use Repository<T> directly for data access and EntityServiceBase<TEntity> as a base class for feature services that need persistence helpers.
Use TenantService for CreateTenant because it has business rules beyond CRUD.
Show the planned file structure before creating files.
```

---

## Entity identity convention

Every new persisted model should follow the shared identity convention unless there is an explicit documented exception:

```text
Id        long internal database primary key
PublicId  Guid public opaque identifier when exposed externally
```

Foreign keys use internal numeric IDs. Public API contracts use `PublicId`, slugs, or other public business identifiers. Database scripts, migrations, and EF mappings must preserve this split.