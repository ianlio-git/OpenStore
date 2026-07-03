using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Cart.Models;

[Table("carts")]
public sealed class Cart : BaseEntity, ITenantEntity
{
    [Required]
    public long TenantId { get; internal set; }

    [Required]
    public long StoreId { get; internal set; }

    public List<CartItem> Items { get; internal set; } = [];
}
