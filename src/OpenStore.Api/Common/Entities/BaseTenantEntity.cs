using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Entities;

public abstract class BaseTenantEntity : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }
}
