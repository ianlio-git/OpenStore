namespace OpenStore.Api.Stores.Dtos;

public sealed record StoreResponse
{
    public Guid PublicId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }
}
