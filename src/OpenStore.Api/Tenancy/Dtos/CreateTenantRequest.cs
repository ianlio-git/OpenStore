namespace OpenStore.Api.Tenancy.Dtos;

public sealed record CreateTenantRequest
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}
