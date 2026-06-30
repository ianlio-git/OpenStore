using OpenStore.Api.Common.Entities;

namespace OpenStore.Api.Tenancy.Models;

public sealed class Tenant : BaseEntity
{
    public string Name { get; internal set; } = string.Empty;

    public string Slug { get; internal set; } = string.Empty;
}
