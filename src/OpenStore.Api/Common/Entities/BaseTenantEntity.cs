using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Entities;

public abstract class BaseTenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; internal set; }
}
