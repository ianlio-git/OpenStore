using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Products.Persistence;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> entity)
    {
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).ValueGeneratedOnAdd();
        entity.Property(p => p.PublicId).IsRequired();
        entity.Property(p => p.TenantId).IsRequired();
        entity.Property(p => p.StoreId).IsRequired();
        entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
        entity.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        entity.Property(p => p.Description).HasMaxLength(2000);
        entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
        entity.HasIndex(p => p.PublicId).IsUnique();
        entity.HasIndex(p => new { p.StoreId, p.Slug }).IsUnique();
        entity.HasOne<Store>().WithMany().HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}
