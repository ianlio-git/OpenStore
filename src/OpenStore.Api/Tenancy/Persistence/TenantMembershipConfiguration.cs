using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tenancy.Persistence;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> entity)
    {
        entity.HasKey(tm => tm.Id);
        entity.Property(tm => tm.Id).ValueGeneratedOnAdd();
        entity.Property(tm => tm.PublicId).IsRequired();
        entity.Property(tm => tm.Role).HasMaxLength(50).IsRequired();
        entity.HasIndex(tm => new { tm.TenantId, tm.UserId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(tm => tm.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
