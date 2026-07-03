using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Categories.Persistence;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).ValueGeneratedOnAdd();
        entity.Property(c => c.PublicId).IsRequired();
        entity.Property(c => c.TenantId).IsRequired();
        entity.Property(c => c.StoreId).IsRequired();
        entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
        entity.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        entity.HasIndex(c => c.PublicId).IsUnique();
        entity.HasIndex(c => new { c.StoreId, c.Slug }).IsUnique();
        entity.HasOne<Store>().WithMany().HasForeignKey(c => c.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
