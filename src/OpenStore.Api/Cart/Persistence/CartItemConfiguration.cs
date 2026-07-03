using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Cart.Models;
using OpenStore.Api.Products.Models;

namespace OpenStore.Api.Cart.Persistence;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> entity)
    {
        entity.HasKey(i => i.Id);
        entity.Property(i => i.Id).ValueGeneratedOnAdd();
        entity.Property(i => i.PublicId).IsRequired();
        entity.Property(i => i.CartId).IsRequired();
        entity.Property(i => i.ProductId).IsRequired();
        entity.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        entity.Property(i => i.ProductSlug).HasMaxLength(100).IsRequired();
        entity.Property(i => i.ProductPublicId).IsRequired();
        entity.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        entity.Property(i => i.Quantity).IsRequired();
        entity.HasIndex(i => i.PublicId).IsUnique();
        entity.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
