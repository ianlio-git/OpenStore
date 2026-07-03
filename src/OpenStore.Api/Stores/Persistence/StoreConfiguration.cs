using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Stores.Persistence;

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
