using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Tenancy.Models;

[Table("tenants")]
public sealed class Tenant : BaseEntity
{
    [Required]
    [StringLength(200)]
    public string Name { get; internal set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; internal set; } = string.Empty;
}
