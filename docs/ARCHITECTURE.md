# Architecture

OpenStore starts as a simple ASP.NET Core API.

The goal is to make the first features easy to understand before adding architectural layers.

---

## 1. Initial Style

```text
One solution
One API project
One test project
One shared Common folder
Feature folders inside the API
Generic Repository + Unit of Work
EntityServiceBase for concrete services
One AppDbContext
Postman for functional API testing
```

Do not start with microservices.
Do not start with separate Domain/Application/Infrastructure projects.
Do not start with one `DbContext` per module.

---

## 2. Request Flow

For `CreateTenant`, the flow is:

```text
Postman or client
  -> Tenancy/Controllers/TenantsController
  -> CreateTenantRequest
  -> Tenancy/Contracts/ITenantService
  -> Tenancy/Services/TenantService
  -> Tenant / TenantMembership
  -> IRepository<Tenant> and IRepository<TenantMembership>
  -> UnitOfWork
  -> AppDbContext
  -> Database
  -> CreateTenantResponse
```

Meaning:

- Controller handles HTTP only.
- Request is what enters the API.
- Feature service contains the use-case logic.
- Models represent business data.
- Repository performs repeated CRUD.
- Unit of Work saves changes once.
- AppDbContext is EF Core's database session.
- Response is what leaves the API.

---

## 3. Common

Use `Common` only for code that is truly reusable.

```text
Common/
  Contracts/
  Entities/
  Persistence/
  Services/
  Time/
  Errors/
```

### `Common/Contracts`

Interfaces only.

Examples:

- `IRepository`
- `IUnitOfWork`
- `ITenantEntity`
- `ICurrentUserContext`
- `IDateTimeProvider`

### `Common/Entities`

Base entity class only.

Examples:

- `BaseEntity`

### `Common/Persistence`

Reusable EF Core implementation.

Examples:

- `AppDbContext`
- `Repository`
- `UnitOfWork`

### `Common/Services`

Reusable generic abstract base services for concrete feature services.

Examples:

- `EntityServiceBase`

### `Common/Time`

Clock implementation.

### `Common/Errors`

Global API error handling.

---

## 4. Persistence Pattern

Use one generic repository interface:

```csharp
public interface IRepository<TEntity>
    where TEntity : class
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    Task DeleteAsync(long id, CancellationToken cancellationToken);
}
```

Use one generic implementation:

```csharp
public sealed class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly AppDbContext dbContext;
    private readonly DbSet<TEntity> set;

    public Repository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
        set = dbContext.Set<TEntity>();
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<TEntity> entities = await set.ToListAsync(cancellationToken);

        return entities;
    }

    public async Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        TEntity? entity = await set.FindAsync([id], cancellationToken);

        return entity;
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        bool exists = await set.AnyAsync(predicate, cancellationToken);

        return exists;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await set.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        set.Update(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        TEntity? entity = await GetByIdAsync(id, cancellationToken);

        if (entity is not null)
        {
            set.Remove(entity);
        }
    }
}
```

Use Unit of Work to save:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

Repositories must not call `SaveChangesAsync` inside `Add`, `Update`, or `Delete`.


## 4.1 Entity identity pattern

OpenStore uses separate internal and public identities for persisted entities whenever possible.

```text
Id        long/bigint identity, internal primary key
PublicId  Guid/uuid unique, public opaque identifier
```

Use `Id` for primary keys, foreign keys, joins, internal repository lookups, and relationship indexes.

Use `PublicId` for API responses, route identifiers, Postman examples, and integration messages crossing service boundaries.

Do not expose internal numeric IDs in public API contracts by default. Do not use `Guid Id` as the primary key unless an ADR or bounded-context note explains why.

Example table shape:

```sql
"Id" bigint GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
"PublicId" uuid NOT NULL,
CONSTRAINT "AK_<Table>_PublicId" UNIQUE ("PublicId")
```

Tenant-owned tables reference the internal tenant key:

```sql
"TenantId" bigint NOT NULL REFERENCES "Tenants" ("Id")
```

A request may carry a public identifier or slug, but must not trust an internal `TenantId` from the body.

---

## 5. Entity Inheritance

OpenStore uses a single base entity class and one entity contract interface:

```text
Common/Entities/BaseEntity.cs
Common/Contracts/ITenantEntity.cs
```

`BaseEntity` owns every common field:

- `long Id` — internal database primary key
- `Guid PublicId` — public opaque identifier
- `DateTimeOffset CreatedAtUtc` — creation timestamp
- `DateTimeOffset? UpdatedAtUtc` — last update timestamp
- `bool IsActive` — soft-delete flag
- `DateTimeOffset? DeletedAtUtc` — soft-delete timestamp
- `MarkAsDeleted(DateTimeOffset)` — soft-delete method
- `Restore()` — undo soft-delete

`ITenantEntity` is a marker contract:

```csharp
public interface ITenantEntity
{
    long TenantId { get; }
}
```

Inheritance rules:

| Entity | Inherits | Implements | Has TenantId? |
|--------|----------|------------|---------------|
| `Tenant` | `BaseEntity` | — | No |
| `TenantMembership` | `BaseEntity` | `ITenantEntity` | Yes |
| `Store` | `BaseEntity` | `ITenantEntity` | Yes |
| `Category` | `BaseEntity` | `ITenantEntity` | Yes |
| `Product` | `BaseEntity` | `ITenantEntity` | Yes |

- `Tenant` is the root — it has no `TenantId`.
- All tenant-scoped models implement `ITenantEntity`.
- There is no `BaseTenantEntity` or `ISoftDeleteEntity`.
- Soft-delete behavior lives directly in `BaseEntity`.
- Query filters (`HasQueryFilter(e => e.IsActive)`) are applied per entity type in `AppDbContext.OnModelCreating`.

---

## 6. EntityServiceBase Pattern

Use `EntityServiceBase<TEntity>` as an abstract base class for feature-specific services that need common persistence helpers.

It is not a generic CRUD service — concrete services own all business operations and mapping.

```csharp
public abstract class EntityServiceBase<TEntity> where TEntity : BaseEntity
{
    protected IRepository<TEntity> Repository { get; }

    protected IUnitOfWork UnitOfWork { get; }

    protected IDateTimeProvider DateTimeProvider { get; }

    protected EntityServiceBase(
        IRepository<TEntity> repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
        DateTimeProvider = dateTimeProvider;
    }

    protected async Task<TEntity> GetByPublicIdOrThrowAsync(
        Guid publicId,
        Func<Exception> notFoundFactory,
        CancellationToken cancellationToken);

    protected async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    protected async Task<IReadOnlyCollection<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    protected void Add(TEntity entity);
    protected void Update(TEntity entity);
    protected void Remove(TEntity entity);
    protected async Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

Feature services inherit and add their own business rules:

```csharp
public sealed class StoreService : EntityServiceBase<Store>, IStoreService
{
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly ICurrentUserContext _currentUser;

    public async Task<StoreResponse> UpdateAsync(
        Guid publicId,
        UpdateStoreRequest request,
        CancellationToken cancellationToken)
    {
        Store store = await GetByPublicIdOrThrowAsync(
            publicId,
            () => new StoreNotFoundException(),
            cancellationToken);

        // business rules...
        Update(store);
        await SaveChangesAsync(cancellationToken);
    }
}
```

Register only concrete services in `Program.cs`:

```csharp
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStoreService, StoreService>();
```

There is no generic `IEntityService<T>` or `EntityService<T>` — that concept has been intentionally removed. `Repository<T>` remains the generic data access abstraction. `EntityServiceBase<TEntity>` is a base class, not a replaceable service.

---

## 7. Generic CRUD Service Contracts

Common CRUD signatures live in `Common/Contracts` as generic interfaces.
Feature interfaces inherit them and declare only feature-specific methods.

### `ICrudService<TResponse, TCreateRequest, TUpdateRequest>`

For root entities that are not children of another feature entity (e.g., Store):

```csharp
public interface ICrudService<TResponse, TCreateRequest, TUpdateRequest>
{
    Task<TResponse> CreateAsync(TCreateRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);
    Task<TResponse> UpdateAsync(Guid publicId, TUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid publicId, CancellationToken cancellationToken = default);
}
```

### `IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>`

For entities scoped under a parent entity (e.g., Category under Store, Product under Store):

```csharp
public interface IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>
{
    Task<TResponse> CreateAsync(Guid parentPublicId, TCreateRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> GetByPublicIdAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default);
    Task<TResponse> UpdateAsync(Guid parentPublicId, Guid publicId, TUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid parentPublicId, Guid publicId, CancellationToken cancellationToken = default);
}
```

### Inheritance pattern

Feature interfaces inherit the generic contract and add only their own methods:

```csharp
public interface IStoreService : ICrudService<StoreResponse, CreateStoreRequest, UpdateStoreRequest>
{
    Task<IReadOnlyCollection<StoreResponse>> GetByTenantPublicIdAsync(Guid tenantPublicId, CancellationToken cancellationToken = default);
}

public interface ICategoryService : IChildCrudService<CategoryResponse, CreateCategoryRequest, UpdateCategoryRequest>
{
    Task<IReadOnlyCollection<CategoryResponse>> GetByStorePublicIdAsync(Guid storePublicId, CancellationToken cancellationToken = default);
}
```

### Rules

- `CreateAsync` normally returns the same response DTO used by `GetByPublicIdAsync` and `UpdateAsync`.
- A create-specific response DTO is allowed only when it contains genuinely different data (e.g., `CreateTenantResponse` because there is no general `TenantResponse`).
- Concrete services implement the feature interface and its inherited methods directly.
- `ITenantService` is not forced into a generic contract because it only supports `CreateAsync` today.

---

## 8. Why AppDbContext Exists

Dependency injection can resolve:

```text
IRepository<Tenant> -> Repository<Tenant>
```

But `Repository<Tenant>` still needs EF Core.

`AppDbContext` is the EF Core object that knows:

- Which database connection to use.
- Which models exist.
- Which objects are being tracked.
- How to save changes.
- How entity metadata is configured.

So the initial rule is:

```text
One AppDbContext for the API.
Generic Repository for CRUD.
UnitOfWork for SaveChanges.
No module-specific DbContext at the beginning.
```

`AppDbContext.OnModelCreating` stays small:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyOpenStoreConfigurations();
    modelBuilder.ApplySoftDeleteQueryFilters();
}
```

Entity-specific mapping lives in feature-level `IEntityTypeConfiguration<T>` classes.
Soft-delete query filters are applied centrally through a reflection-based extension, not repeated per entity.

---

## 9. Database Mapping

Do not put Fluent API mappings inside `AppDbContext.OnModelCreating`.
Create one `IEntityTypeConfiguration<T>` class per entity inside the owning feature folder's `Persistence/` subfolder.

Feature folders with entity models include:

```text
Tenancy/
  Persistence/
    TenantConfiguration.cs
    TenantMembershipConfiguration.cs
Stores/
  Persistence/
    StoreConfiguration.cs
Categories/
  Persistence/
    CategoryConfiguration.cs
Products/
  Persistence/
    ProductConfiguration.cs
```

These are applied explicitly through a shared extension:

```csharp
// Common/Persistence/ModelBuilderConfigurationExtensions.cs
internal static class ModelBuilderConfigurationExtensions
{
    public static void ApplyOpenStoreConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMembershipConfiguration());
        modelBuilder.ApplyConfiguration(new StoreConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
    }
}
```

This is explicit, easy to navigate, and avoids `ApplyConfigurationsFromAssembly` magic.

Example configuration:

```csharp
internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> entity)
    {
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).ValueGeneratedOnAdd();
        entity.Property(s => s.PublicId).IsRequired();
        entity.Property(s => s.TenantId).IsRequired();
        entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
        entity.Property(s => s.Slug).HasMaxLength(100).IsRequired();
        entity.HasIndex(s => s.PublicId).IsUnique();
        entity.HasIndex(s => new { s.TenantId, s.Slug }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
```

Do not put `HasQueryFilter(x => x.IsActive)` inside entity configurations.
Soft-delete query filters are applied centrally — see the next section.

---

## 10. Soft Delete Query Filters

Because `BaseEntity` owns `IsActive`, query filters are applied centrally rather than repeated per entity.

```csharp
// Common/Persistence/SoftDeleteModelBuilderExtensions.cs
internal static class SoftDeleteModelBuilderExtensions
{
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
            MemberExpression property = Expression.Property(parameter, nameof(BaseEntity.IsActive));
            LambdaExpression filter = Expression.Lambda(property, parameter);

            entityType.SetQueryFilter(filter);
        }
    }
}
```

This uses expression trees against entity metadata. Every entity that inherits `BaseEntity` automatically gets `entity => entity.IsActive` as a query filter.

Rules:

- Do not repeat `HasQueryFilter(x => x.IsActive)` inside individual entity configurations.
- Do not create `ISoftDeleteEntity` or a secondary base class.
- If an entity should not be filtered, add an explicit opt-out mechanism later — the MVP has no such case.

---

## 11. Tenancy

Start with one readable feature folder:

```text
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
  Dtos/
    CreateTenantRequest.cs
    CreateTenantResponse.cs
```

Rules:

- `Controllers` contains HTTP only.
- `Services` contains business logic.
- `Contracts` contains Tenancy-specific interfaces such as `ITenantService`.
- `Models` contains business/database models and value objects.
- `Dtos` contains API input/output contracts.

Controllers should depend on `ITenantService`, not directly on `TenantService`.

Do not create `TenantRepository` at the beginning.

For slug uniqueness, use the repository directly:

```csharp
bool exists = await _tenantRepository.AnyAsync(
    tenant => tenant.Slug == slug,
    cancellationToken);
```

Create `TenantRepository` later only if the generic repository becomes awkward or hides important behavior.

---

## 12. When To Add More Structure

Add structure only when the code asks for it:

- Many EF mappings inside `ModelBuilderConfigurationExtensions`: consider `ApplyConfigurationsFromAssembly`.
- Specific repeated queries: create a specific repository.
- Too many files inside a Tenancy subfolder: split that subfolder by sub-feature.
- Too many API features: consider separate module projects through an ADR.

Do not add layers before they reduce real pain.
