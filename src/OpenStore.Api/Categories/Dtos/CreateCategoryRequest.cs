using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Categories.Dtos;

public sealed record CreateCategoryRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Slug]
    public string Slug { get; init; } = string.Empty;
}
