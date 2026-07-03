using OpenStore.Api.Cart.Dtos;

namespace OpenStore.Api.Cart.Contracts;

public interface ICartService
{
    Task<CartResponse> CreateAsync(Guid storePublicId, CancellationToken cancellationToken = default);

    Task<CartResponse> GetByPublicIdAsync(Guid storePublicId, Guid cartPublicId, CancellationToken cancellationToken = default);

    Task<CartResponse> AddItemAsync(Guid storePublicId, Guid cartPublicId, AddCartItemRequest request, CancellationToken cancellationToken = default);

    Task<CartResponse> UpdateItemQuantityAsync(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);

    Task RemoveItemAsync(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, CancellationToken cancellationToken = default);
}
