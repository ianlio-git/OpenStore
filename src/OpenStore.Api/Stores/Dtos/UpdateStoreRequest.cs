using System.ComponentModel.DataAnnotations;
using OpenStore.Api.Common.Validation;

namespace OpenStore.Api.Stores.Dtos;

public sealed record UpdateStoreRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Slug]
    public string Slug { get; init; } = string.Empty;
}
