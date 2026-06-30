using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Common.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeProvider dateTimeProvider) : base(options)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Slug).HasMaxLength(100).IsRequired();
            entity.HasIndex(t => t.Slug).IsUnique();
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(tm => tm.Id);
            entity.Property(tm => tm.Role).HasMaxLength(50).IsRequired();
            entity.HasIndex(tm => new { tm.TenantId, tm.UserId }).IsUnique();
        });
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset utcNow = _dateTimeProvider.UtcNow;

        IEnumerable<EntityEntry<BaseEntity>> entries = ChangeTracker.Entries<BaseEntity>().Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (EntityEntry<BaseEntity> entry in entries)
        {
            BaseEntity baseEntity = entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    if (baseEntity.Id == Guid.Empty)
                    {
                        baseEntity.Id = Guid.NewGuid();
                    }

                    baseEntity.CreatedAtUtc = utcNow;
                    break;

                case EntityState.Modified:
                    baseEntity.UpdatedAtUtc = utcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
