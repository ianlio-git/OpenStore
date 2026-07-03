using Microsoft.EntityFrameworkCore;
using OpenStore.Api.Cart.Persistence;
using OpenStore.Api.Categories.Persistence;
using OpenStore.Api.Products.Persistence;
using OpenStore.Api.Stores.Persistence;
using OpenStore.Api.Tenancy.Persistence;

namespace OpenStore.Api.Common.Persistence;

internal static class ModelBuilderConfigurationExtensions
{
    public static void ApplyOpenStoreConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMembershipConfiguration());
        modelBuilder.ApplyConfiguration(new StoreConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new CartConfiguration());
        modelBuilder.ApplyConfiguration(new CartItemConfiguration());
    }
}
