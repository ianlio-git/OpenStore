using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Cart.Models;

[Table("cart_items")]
public sealed class CartItem : BaseEntity
{
    [Required]
    public long CartId { get; internal set; }

    [Required]
    public long ProductId { get; internal set; }

    [Required]
    public string ProductName { get; internal set; } = string.Empty;

    [Required]
    public string ProductSlug { get; internal set; } = string.Empty;

    [Required]
    public Guid ProductPublicId { get; internal set; }

    [Required]
    public decimal UnitPrice { get; internal set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; internal set; }

    public decimal Subtotal => UnitPrice * Quantity;
}
