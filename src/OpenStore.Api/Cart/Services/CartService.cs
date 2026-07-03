using OpenStore.Api.Cart.Contracts;
using OpenStore.Api.Cart.Dtos;
using OpenStore.Api.Cart.Exceptions;
using OpenStore.Api.Cart.Models;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Services;
using OpenStore.Api.Products.Exceptions;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;

namespace OpenStore.Api.Cart.Services;

public sealed class CartService : EntityServiceBase<Models.Cart>, ICartService
{
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<CartItem> _cartItemRepository;

    public CartService(
        IRepository<Models.Cart> cartRepository,
        IRepository<Store> storeRepository,
        IRepository<Product> productRepository,
        IRepository<CartItem> cartItemRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
        : base(cartRepository, unitOfWork, dateTimeProvider)
    {
        _storeRepository = storeRepository;
        _productRepository = productRepository;
        _cartItemRepository = cartItemRepository;
    }

    private static CartItemResponse MapItemToResponse(CartItem item)
    {
        CartItemResponse result = new()
        {
            PublicId = item.PublicId,
            ProductPublicId = item.ProductPublicId,
            ProductName = item.ProductName,
            ProductSlug = item.ProductSlug,
            UnitPrice = item.UnitPrice,
            Quantity = item.Quantity,
            Subtotal = item.Subtotal
        };

        return result;
    }

    private static CartResponse MapToResponse(Models.Cart cart, Guid storePublicId)
    {
        List<CartItemResponse> items = [.. cart.Items.Select(MapItemToResponse)];

        decimal total = items.Sum(i => i.Subtotal);

        CartResponse result = new()
        {
            PublicId = cart.PublicId,
            StorePublicId = storePublicId,
            Items = items,
            Total = total,
            CreatedAtUtc = cart.CreatedAtUtc,
            UpdatedAtUtc = cart.UpdatedAtUtc
        };

        return result;
    }

    private async Task<Store> GetStoreOrThrowAsync(Guid storePublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Store> matches = await _storeRepository.FindAsync(s => s.PublicId == storePublicId && s.IsActive, cancellationToken);

        Store result = matches.FirstOrDefault() ?? throw new StoreNotFoundException();

        return result;
    }

    private async Task<Product> GetProductOrThrowAsync(Guid productPublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Product> matches = await _productRepository.FindAsync(p => p.PublicId == productPublicId && p.IsActive, cancellationToken);

        Product result = matches.FirstOrDefault() ?? throw new ProductNotFoundException();

        return result;
    }

    public async Task<CartResponse> CreateAsync(Guid storePublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        Models.Cart cart = new()
        {
            TenantId = store.TenantId,
            StoreId = store.Id,
            Items = []
        };

        Add(cart);

        await SaveChangesAsync(cancellationToken);

        CartResponse result = MapToResponse(cart, storePublicId);

        return result;
    }

    public async Task<CartResponse> GetByPublicIdAsync(Guid storePublicId, Guid cartPublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        Models.Cart cart = await GetByPublicIdOrThrowAsync(cartPublicId, () => new CartNotFoundException(), cancellationToken);

        if (cart.StoreId != store.Id)
        {
            throw new CartNotFoundException();
        }

        IReadOnlyCollection<CartItem> items = await _cartItemRepository.FindAsync(i => i.CartId == cart.Id && i.IsActive, cancellationToken);

        cart.Items = [.. items];

        CartResponse result = MapToResponse(cart, storePublicId);

        return result;
    }

    public async Task<CartResponse> AddItemAsync(Guid storePublicId, Guid cartPublicId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        Models.Cart cart = await GetByPublicIdOrThrowAsync(cartPublicId, () => new CartNotFoundException(), cancellationToken);

        if (cart.StoreId != store.Id)
        {
            throw new CartNotFoundException();
        }

        Product product = await GetProductOrThrowAsync(request.ProductPublicId, cancellationToken);

        if (product.StoreId != store.Id)
        {
            throw new ProductNotFoundException();
        }

        IReadOnlyCollection<CartItem> existingItems = await _cartItemRepository.FindAsync(i => i.CartId == cart.Id && i.ProductId == product.Id && i.IsActive, cancellationToken);

        CartItem? existingItem = existingItems.FirstOrDefault();

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;

            _cartItemRepository.Update(existingItem);
        }
        else
        {
            CartItem newItem = new()
            {
                CartId = cart.Id,
                ProductId = product.Id,
                ProductPublicId = product.PublicId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                UnitPrice = product.Price,
                Quantity = request.Quantity
            };

            _cartItemRepository.Add(newItem);
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<CartItem> allItems = await _cartItemRepository.FindAsync(i => i.CartId == cart.Id && i.IsActive, cancellationToken);

        cart.Items = [.. allItems];

        CartResponse result = MapToResponse(cart, storePublicId);

        return result;
    }

    public async Task<CartResponse> UpdateItemQuantityAsync(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        Models.Cart cart = await GetByPublicIdOrThrowAsync(cartPublicId, () => new CartNotFoundException(), cancellationToken);

        if (cart.StoreId != store.Id)
        {
            throw new CartNotFoundException();
        }

        IReadOnlyCollection<CartItem> itemMatches = await _cartItemRepository.FindAsync(i => i.PublicId == cartItemPublicId && i.CartId == cart.Id && i.IsActive, cancellationToken);

        CartItem item = itemMatches.FirstOrDefault() ?? throw new CartItemNotFoundException();

        item.Quantity = request.Quantity;

        _cartItemRepository.Update(item);

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<CartItem> allItems = await _cartItemRepository.FindAsync(i => i.CartId == cart.Id && i.IsActive, cancellationToken);

        cart.Items = [.. allItems];

        CartResponse result = MapToResponse(cart, storePublicId);

        return result;
    }

    public async Task RemoveItemAsync(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, CancellationToken cancellationToken = default)
    {
        Store store = await GetStoreOrThrowAsync(storePublicId, cancellationToken);

        Models.Cart cart = await GetByPublicIdOrThrowAsync(cartPublicId, () => new CartNotFoundException(), cancellationToken);

        if (cart.StoreId != store.Id)
        {
            throw new CartNotFoundException();
        }

        IReadOnlyCollection<CartItem> itemMatches = await _cartItemRepository.FindAsync(i => i.PublicId == cartItemPublicId && i.CartId == cart.Id && i.IsActive, cancellationToken);

        CartItem item = itemMatches.FirstOrDefault() ?? throw new CartItemNotFoundException();

        item.MarkAsDeleted(DateTimeProvider.UtcNow);

        _cartItemRepository.Update(item);

        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}
