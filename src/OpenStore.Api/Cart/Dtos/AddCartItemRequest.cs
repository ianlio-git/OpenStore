using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Cart.Dtos;

public sealed record AddCartItemRequest
{
    [RequiredGuid]
    public Guid ProductPublicId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
