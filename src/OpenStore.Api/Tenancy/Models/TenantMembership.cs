using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Tenancy.Models;

public sealed class TenantMembership : BaseTenantEntity
{
    public Guid UserId { get; internal set; }

    public string Role { get; internal set; } = string.Empty;
}
