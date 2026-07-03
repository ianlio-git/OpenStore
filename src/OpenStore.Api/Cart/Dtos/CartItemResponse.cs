namespace OpenStore.Api.Cart.Dtos;

public sealed record CartItemResponse
{
    public Guid PublicId { get; init; }

    public Guid ProductPublicId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductSlug { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal Subtotal { get; init; }
}
