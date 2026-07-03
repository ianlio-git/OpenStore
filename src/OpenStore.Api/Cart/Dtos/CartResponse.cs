namespace OpenStore.Api.Cart.Dtos;

public sealed record CartResponse
{
    public Guid PublicId { get; init; }

    public Guid StorePublicId { get; init; }

    public List<CartItemResponse> Items { get; init; } = [];

    public decimal Total { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? UpdatedAtUtc { get; init; }
}
