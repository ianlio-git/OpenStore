using NSubstitute;
using OpenStore.Api.Cart.Contracts;
using OpenStore.Api.Cart.Dtos;
using OpenStore.Api.Cart.Exceptions;
using OpenStore.Api.Cart.Services;
using CartEntity = OpenStore.Api.Cart.Models.Cart;
using CartItemEntity = OpenStore.Api.Cart.Models.CartItem;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Products.Exceptions;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;

namespace OpenStore.Api.Tests.Cart.Services;

public sealed class CartServiceTests
{
    private readonly IRepository<CartEntity> _cartRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<CartItemEntity> _cartItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CartService _service;

    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    public CartServiceTests()
    {
        _cartRepository = Substitute.For<IRepository<CartEntity>>();
        _storeRepository = Substitute.For<IRepository<Store>>();
        _productRepository = Substitute.For<IRepository<Product>>();
        _cartItemRepository = Substitute.For<IRepository<CartItemEntity>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(FixedUtcNow);

        _service = new CartService(_cartRepository, _storeRepository, _productRepository, _cartItemRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_ValidStore_CreatesCart()
    {
        Guid storePublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        CartEntity? capturedCart = null;

        _cartRepository.When(x => x.Add(Arg.Any<CartEntity>()))
            .Do(callInfo => capturedCart = callInfo.Arg<CartEntity>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedCart is not null && capturedCart.Id == 0)
                {
                    capturedCart.Id = 100;
                    capturedCart.PublicId = Guid.NewGuid();
                    capturedCart.CreatedAtUtc = FixedUtcNow;
                }
            });

        CartResponse result = await _service.CreateAsync(storePublicId, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal(storePublicId, result.StorePublicId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);

        _cartRepository.Received(1).Add(Arg.Is<CartEntity>(c =>
            c.TenantId == tenantId &&
            c.StoreId == storeId));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.CreateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));

        _cartRepository.DidNotReceive().Add(Arg.Any<CartEntity>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByPublicIdAsync_CartFound_ReturnsCart()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity>());

        CartResponse result = await _service.GetByPublicIdAsync(storePublicId, cartPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(cartPublicId, result.PublicId);
        Assert.Equal(storePublicId, result.StorePublicId);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task GetByPublicIdAsync_CartNotFound_ThrowsCartNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        long storeId = 42;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>());

        await Assert.ThrowsAsync<CartNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByPublicIdAsync_WrongStore_ThrowsCartNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        long storeId = 42;
        long otherStoreId = 99;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = 100, PublicId = cartPublicId, StoreId = otherStoreId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        await Assert.ThrowsAsync<CartNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, cartPublicId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AddItemAsync_ValidRequest_AddsItem()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        Guid newItemPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;
        long productId = 200;

        AddCartItemRequest request = new()
        {
            ProductPublicId = productPublicId,
            Quantity = 2
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>
            {
                new() { Id = productId, PublicId = productPublicId, StoreId = storeId, TenantId = tenantId, Name = "Laptop", Slug = "laptop", Price = 999.99m, IsActive = true }
            });

        _cartItemRepository.When(x => x.Add(Arg.Any<CartItemEntity>()))
            .Do(callInfo =>
            {
                CartItemEntity item = callInfo.Arg<CartItemEntity>();
                item.Id = 300;
                item.PublicId = newItemPublicId;
            });

        CartItemEntity newItemResponse = new()
        {
            Id = 300,
            PublicId = newItemPublicId,
            CartId = cartId,
            ProductId = productId,
            ProductPublicId = productPublicId,
            ProductName = "Laptop",
            ProductSlug = "laptop",
            UnitPrice = 999.99m,
            Quantity = 2,
            IsActive = true
        };

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(
                new List<CartItemEntity>(),
                new List<CartItemEntity> { newItemResponse });

        CartResponse result = await _service.AddItemAsync(storePublicId, cartPublicId, request, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal("Laptop", result.Items[0].ProductName);
        Assert.Equal(productPublicId, result.Items[0].ProductPublicId);
        Assert.Equal(999.99m, result.Items[0].UnitPrice);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(1999.98m, result.Items[0].Subtotal);
        Assert.Equal(1999.98m, result.Total);

        _cartItemRepository.Received(1).Add(Arg.Is<CartItemEntity>(i =>
            i.CartId == cartId &&
            i.ProductId == productId &&
            i.Quantity == 2 &&
            i.UnitPrice == 999.99m));
    }

    [Fact]
    public async Task AddItemAsync_SameProduct_IncreasesQuantity()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        Guid itemPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;
        long productId = 200;
        long itemId = 300;

        AddCartItemRequest request = new()
        {
            ProductPublicId = productPublicId,
            Quantity = 3
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>
            {
                new() { Id = productId, PublicId = productPublicId, StoreId = storeId, TenantId = tenantId, Name = "Laptop", Slug = "laptop", Price = 999.99m, IsActive = true }
            });

        CartItemEntity existingItem = new()
        {
            Id = itemId,
            PublicId = itemPublicId,
            CartId = cartId,
            ProductId = productId,
            ProductPublicId = productPublicId,
            ProductName = "Laptop",
            ProductSlug = "laptop",
            UnitPrice = 999.99m,
            Quantity = 2,
            IsActive = true
        };

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity> { existingItem });

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                existingItem.Quantity = 5;
            });

        CartResponse result = await _service.AddItemAsync(storePublicId, cartPublicId, request, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);
        Assert.Equal(4999.95m, result.Total);

        _cartItemRepository.Received(1).Update(Arg.Is<CartItemEntity>(i => i.Quantity == 5));
        _cartItemRepository.DidNotReceive().Add(Arg.Any<CartItemEntity>());
    }

    [Fact]
    public async Task AddItemAsync_ProductFromAnotherStore_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        long storeId = 42;
        long otherStoreId = 99;
        long tenantId = 10;
        long cartId = 100;

        AddCartItemRequest request = new()
        {
            ProductPublicId = productPublicId,
            Quantity = 1
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>
            {
                new() { Id = 200, PublicId = productPublicId, StoreId = otherStoreId, Name = "Other Product", Slug = "other", Price = 10m, IsActive = true }
            });

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.AddItemAsync(storePublicId, cartPublicId, request, TestContext.Current.CancellationToken));

        _cartItemRepository.DidNotReceive().Add(Arg.Any<CartItemEntity>());
        _cartItemRepository.DidNotReceive().Update(Arg.Any<CartItemEntity>());
    }

    [Fact]
    public async Task AddItemAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;

        AddCartItemRequest request = new()
        {
            ProductPublicId = Guid.NewGuid(),
            Quantity = 1
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.AddItemAsync(storePublicId, cartPublicId, request, TestContext.Current.CancellationToken));

        _cartItemRepository.DidNotReceive().Add(Arg.Any<CartItemEntity>());
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ValidRequest_UpdatesQuantity()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid itemPublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;
        long itemId = 300;

        UpdateCartItemRequest request = new()
        {
            Quantity = 5
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        CartItemEntity item = new()
        {
            Id = itemId,
            PublicId = itemPublicId,
            CartId = cartId,
            ProductId = 200,
            ProductPublicId = productPublicId,
            ProductName = "Laptop",
            ProductSlug = "laptop",
            UnitPrice = 999.99m,
            Quantity = 2,
            IsActive = true
        };

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity> { item });

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                item.Quantity = 5;
            });

        CartResponse result = await _service.UpdateItemQuantityAsync(storePublicId, cartPublicId, itemPublicId, request, TestContext.Current.CancellationToken);

        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);
        Assert.Equal(4999.95m, result.Total);

        _cartItemRepository.Received(1).Update(Arg.Is<CartItemEntity>(i => i.Quantity == 5));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ItemNotFound_ThrowsCartItemNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;

        UpdateCartItemRequest request = new()
        {
            Quantity = 3
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity>());

        await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
            _service.UpdateItemQuantityAsync(storePublicId, cartPublicId, Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _cartItemRepository.DidNotReceive().Update(Arg.Any<CartItemEntity>());
    }

    [Fact]
    public async Task RemoveItemAsync_ValidRequest_SoftDeletesItem()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid itemPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;
        long itemId = 300;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        CartItemEntity item = new()
        {
            Id = itemId,
            PublicId = itemPublicId,
            CartId = cartId,
            ProductId = 200,
            ProductPublicId = Guid.NewGuid(),
            ProductName = "Laptop",
            ProductSlug = "laptop",
            UnitPrice = 999.99m,
            Quantity = 2,
            IsActive = true
        };

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity> { item });

        await _service.RemoveItemAsync(storePublicId, cartPublicId, itemPublicId, TestContext.Current.CancellationToken);

        _cartItemRepository.Received(1).Update(Arg.Is<CartItemEntity>(i =>
            i.IsActive == false &&
            i.DeletedAtUtc == FixedUtcNow));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItemAsync_ItemNotFound_ThrowsCartItemNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartItemEntity>());

        await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
            _service.RemoveItemAsync(storePublicId, cartPublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));

        _cartItemRepository.DidNotReceive().Update(Arg.Any<CartItemEntity>());
    }

    [Fact]
    public async Task CartTotal_CalculatedCorrectly()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid product1PublicId = Guid.NewGuid();
        Guid product2PublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        long cartId = 100;
        long product1Id = 200;
        long product2Id = 201;

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _cartRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CartEntity>
            {
                new() { Id = cartId, PublicId = cartPublicId, TenantId = tenantId, StoreId = storeId, CreatedAtUtc = FixedUtcNow, Items = [] }
            });

        List<CartItemEntity> items =
        [
            new()
            {
                Id = 300,
                PublicId = Guid.NewGuid(),
                CartId = cartId,
                ProductId = product1Id,
                ProductPublicId = product1PublicId,
                ProductName = "Laptop",
                ProductSlug = "laptop",
                UnitPrice = 1000m,
                Quantity = 2,
                IsActive = true
            },
            new()
            {
                Id = 301,
                PublicId = Guid.NewGuid(),
                CartId = cartId,
                ProductId = product2Id,
                ProductPublicId = product2PublicId,
                ProductName = "Mouse",
                ProductSlug = "mouse",
                UnitPrice = 50m,
                Quantity = 3,
                IsActive = true
            }
        ];

        _cartItemRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CartItemEntity, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(items);

        CartResponse result = await _service.GetByPublicIdAsync(storePublicId, cartPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2000m, result.Items[0].Subtotal);
        Assert.Equal(150m, result.Items[1].Subtotal);
        Assert.Equal(2150m, result.Total);
    }
}
