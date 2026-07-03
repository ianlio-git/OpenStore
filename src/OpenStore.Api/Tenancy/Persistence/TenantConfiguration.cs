using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tenancy.Persistence;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> entity)
    {
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).ValueGeneratedOnAdd();
        entity.Property(t => t.PublicId).IsRequired();
        entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
        entity.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        entity.HasIndex(t => t.Slug).IsUnique();
        entity.HasIndex(t => t.PublicId).IsUnique();
    }
}
