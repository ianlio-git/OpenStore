using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;
using OpenStore.Api.Common.Errors;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Common.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    private void PrepareEntitiesToSave()
    {
        DateTimeOffset utcNow = _dateTimeProvider.UtcNow;

        foreach (EntityEntry<BaseEntity> entry in GetChangedEntries<BaseEntity>())
        {
            if (entry.State is EntityState.Added)
            {
                PrepareAddedEntity(entry.Entity, utcNow);
            }
            else
            {
                PrepareModifiedEntity(entry.Entity, utcNow);
            }
        }
    }

    private static void PrepareAddedEntity(BaseEntity entity, DateTimeOffset utcNow)
    {
        if (entity.PublicId == Guid.Empty)
        {
            entity.PublicId = Guid.NewGuid();
        }

        entity.CreatedAtUtc = utcNow;
    }

    private static void PrepareModifiedEntity(BaseEntity entity, DateTimeOffset utcNow) => entity.UpdatedAtUtc = utcNow;

    private void ValidateChangedEntities()
    {
        List<ValidationResult> results = [];

        IEnumerable<object> entitiesToValidate = GetChangedEntries<object>().Select(entry => entry.Entity);

        foreach (object entity in entitiesToValidate)
        {
            ValidationContext context = new(entity);
            Validator.TryValidateObject(entity, context, results, validateAllProperties: true);
        }

        if (results.Count > 0)
        {
            throw ModelValidationException.FromValidationResults(results);
        }
    }

    private IEnumerable<EntityEntry<T>> GetChangedEntries<T>() where T : class => ChangeTracker.Entries<T>().Where(entry => entry.State is EntityState.Added or EntityState.Modified);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOpenStoreConfigurations();
        modelBuilder.ApplySoftDeleteQueryFilters();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareEntitiesToSave();
        ValidateChangedEntities();

        int result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Cart.Models.Cart> Carts => Set<Cart.Models.Cart>();

    public DbSet<Cart.Models.CartItem> CartItems => Set<Cart.Models.CartItem>();
}
