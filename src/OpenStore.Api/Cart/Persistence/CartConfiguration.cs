using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Cart.Models;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Cart.Persistence;

internal sealed class CartConfiguration : IEntityTypeConfiguration<Models.Cart>
{
    public void Configure(EntityTypeBuilder<Models.Cart> entity)
    {
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).ValueGeneratedOnAdd();
        entity.Property(c => c.PublicId).IsRequired();
        entity.Property(c => c.TenantId).IsRequired();
        entity.Property(c => c.StoreId).IsRequired();
        entity.HasIndex(c => c.PublicId).IsUnique();
        entity.HasOne<Store>().WithMany().HasForeignKey(c => c.StoreId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasMany(c => c.Items)
            .WithOne()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
