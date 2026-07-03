using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Products.Models;

[Table("products")]
public sealed class Product : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }

    [Required]
    public long StoreId { get; internal set; }

    public long? CategoryId { get; internal set; }

    [Required]
    [StringLength(200)]
    public string Name { get; internal set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; internal set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; internal set; }

    public decimal Price { get; internal set; }
}
