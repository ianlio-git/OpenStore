using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Stores.Models;

[Table("stores")]
public sealed class Store : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }

    [Required]
    [StringLength(200)]
    public string Name { get; internal set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; internal set; } = string.Empty;
}
