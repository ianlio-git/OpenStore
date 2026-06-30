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
Generic Entity Service for simple CRUD
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
  -> IEntityService<Tenant> and IEntityService<TenantMembership>
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
- Entity Service provides reusable CRUD behavior above repositories.
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
- `IEntityService`
- `IUnitOfWork`
- `ITenantEntity`
- `ICurrentUserContext`
- `IDateTimeProvider`

### `Common/Entities`

Base entity classes only.

Examples:

- `BaseEntity`
- `BaseTenantEntity`

### `Common/Persistence`

Reusable EF Core implementation.

Examples:

- `AppDbContext`
- `Repository`
- `UnitOfWork`

### `Common/Services`

Reusable generic services.

Examples:

- `EntityService`

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

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
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

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
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

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
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

---

## 5. Entity Service Pattern

Use `IEntityService<TEntity>` as the generic service layer for simple CRUD.

```csharp
public interface IEntityService<TEntity>
    where TEntity : BaseEntity
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken);

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

Default implementation:

```csharp
public sealed class EntityService<TEntity> : IEntityService<TEntity>
    where TEntity : BaseEntity
{
    private readonly IRepository<TEntity> repository;

    public EntityService(IRepository<TEntity> repository)
    {
        this.repository = repository;
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TEntity> entities = await repository.GetAllAsync(cancellationToken);

        return entities;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        TEntity? entity = await repository.GetByIdAsync(id, cancellationToken);

        return entity;
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken)
    {
        bool exists = await repository.AnyAsync(predicate, cancellationToken);

        return exists;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        repository.Update(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(id, cancellationToken);
    }
}
```

Register open generics in `Program.cs`:

```csharp
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IEntityService<>), typeof(EntityService<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

`EntityService<TEntity>` must not call `SaveChangesAsync`. Use `IUnitOfWork` in the controller or feature service so one use case can save all changes together.

Use `EntityService<TEntity>` directly from controllers only for simple CRUD with no business rules.

Use a feature-specific service when the operation has business rules.

Example:

```csharp
public sealed class TenantService
{
    private readonly IEntityService<Tenant> tenantService;
    private readonly IEntityService<TenantMembership> membershipService;
    private readonly IUnitOfWork unitOfWork;

    public TenantService(
        IEntityService<Tenant> tenantService,
        IEntityService<TenantMembership> membershipService,
        IUnitOfWork unitOfWork)
    {
        this.tenantService = tenantService;
        this.membershipService = membershipService;
        this.unitOfWork = unitOfWork;
    }
}
```

---

## 6. Why AppDbContext Exists

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
- How simple mappings are configured.

So the initial rule is:

```text
One AppDbContext for the API.
Generic Repository for CRUD.
UnitOfWork for SaveChanges.
No module-specific DbContext at the beginning.
```

---

## 7. Database Mapping

Start with simple mappings inside `AppDbContext.OnModelCreating`.

Example:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Tenant>(entity =>
    {
        entity.ToTable("tenants");
        entity.HasKey(tenant => tenant.Id);
        entity.Property(tenant => tenant.Name).IsRequired();
        entity.HasIndex(tenant => tenant.Slug).IsUnique();
    });

    modelBuilder.Entity<TenantMembership>(entity =>
    {
        entity.ToTable("tenant_memberships");
        entity.HasKey(membership => membership.Id);
    });
}
```

Do not create separate `TenantConfiguration` or `TenantMembershipConfiguration` files until `OnModelCreating` becomes hard to read.

---

## 8. Tenancy

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
    TenantSlug.cs
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

For slug uniqueness, use:

```csharp
bool exists = await tenantEntityService.AnyAsync(
    tenant => tenant.Slug == slug,
    cancellationToken);
```

Create `TenantRepository` later only if the generic repository becomes awkward or hides important behavior.

---

## 9. When To Add More Structure

Add structure only when the code asks for it:

- Many EF mappings: extract configuration classes.
- Specific repeated queries: create a specific repository.
- Too many files inside a Tenancy subfolder: split that subfolder by sub-feature.
- Too many API features: consider separate module projects through an ADR.

Do not add layers before they reduce real pain.
