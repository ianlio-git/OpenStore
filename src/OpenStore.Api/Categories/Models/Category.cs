using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Categories.Models;

[Table("categories")]
public sealed class Category : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }

    [Required]
    public long StoreId { get; internal set; }

    [Required]
    [StringLength(200)]
    public string Name { get; internal set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; internal set; } = string.Empty;
}
