namespace OpenStore.Api.Tenancy.Dtos;

public sealed record CreateTenantResponse
{
    public Guid PublicId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }
}
