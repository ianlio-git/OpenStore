using System.ComponentModel.DataAnnotations;

namespace OpenStore.Api.Cart.Dtos;

public sealed record UpdateCartItemRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
