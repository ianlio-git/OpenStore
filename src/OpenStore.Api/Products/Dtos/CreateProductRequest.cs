using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Products.Dtos;

public sealed record CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Slug]
    public string Slug { get; init; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; init; }

    public decimal Price { get; init; }

    public Guid? CategoryPublicId { get; init; }
}
