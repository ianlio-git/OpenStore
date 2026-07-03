using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Tenancy.Models;

[Table("tenant_memberships")]
public sealed class TenantMembership : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }

    [Required]
    public Guid UserId { get; internal set; }

    [Required]
    [StringLength(50)]
    public string Role { get; internal set; } = string.Empty;
}
