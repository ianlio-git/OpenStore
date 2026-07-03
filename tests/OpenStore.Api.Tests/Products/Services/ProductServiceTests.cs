using NSubstitute;
using OpenStore.Api.Categories.Exceptions;
using OpenStore.Api.Categories.Models;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Products.Contracts;
using OpenStore.Api.Products.Dtos;
using OpenStore.Api.Products.Exceptions;
using OpenStore.Api.Products.Models;
using OpenStore.Api.Products.Services;
using OpenStore.Api.Stores.Exceptions;
using OpenStore.Api.Stores.Models;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tests.Products.Services;

public sealed class ProductServiceTests
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Store> _storeRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<TenantMembership> _membershipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IProductService _service;

    private static readonly DateTimeOffset FixedUtcNow = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

    public ProductServiceTests()
    {
        _productRepository = Substitute.For<IRepository<Product>>();
        _storeRepository = Substitute.For<IRepository<Store>>();
        _categoryRepository = Substitute.For<IRepository<Category>>();
        _membershipRepository = Substitute.For<IRepository<TenantMembership>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _currentUser = Substitute.For<ICurrentUserContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.GetRequiredUserId().Returns(Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(FixedUtcNow);

        _service = new ProductService(_productRepository, _storeRepository, _categoryRepository, _membershipRepository, _unitOfWork, _currentUser, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        Guid storePublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        Guid userId = Guid.NewGuid();

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Description = "A powerful laptop",
            Price = 999.99m
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        Product? capturedProduct = null;

        _productRepository.When(x => x.Add(Arg.Any<Product>()))
            .Do(callInfo => capturedProduct = callInfo.Arg<Product>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedProduct is not null && capturedProduct.Id == 0)
                {
                    capturedProduct.Id = 100;
                    capturedProduct.PublicId = Guid.NewGuid();
                }
            });

        ProductResponse result = await _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("laptop", result.Slug);
        Assert.Equal("A powerful laptop", result.Description);
        Assert.Equal(999.99m, result.Price);
        Assert.Null(result.CategoryPublicId);

        _productRepository.Received(1).Add(Arg.Is<Product>(p =>
            p.StoreId == storeId &&
            p.TenantId == tenantId &&
            p.Name == "Laptop" &&
            p.Slug == "laptop" &&
            p.Description == "A powerful laptop" &&
            p.Price == 999.99m &&
            p.CategoryId == null));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithCategory_CreatesProductWithCategory()
    {
        Guid storePublicId = Guid.NewGuid();
        long storeId = 42;
        long tenantId = 10;
        Guid categoryPublicId = Guid.NewGuid();
        long categoryId = 5;

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m,
            CategoryPublicId = categoryPublicId
        };

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = storeId, PublicId = storePublicId, TenantId = tenantId, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>
            {
                new() { Id = categoryId, PublicId = categoryPublicId, StoreId = storeId, TenantId = tenantId, Name = "Electronics", Slug = "electronics", IsActive = true }
            });

        Product? capturedProduct = null;

        _productRepository.When(x => x.Add(Arg.Any<Product>()))
            .Do(callInfo => capturedProduct = callInfo.Arg<Product>());

        _unitOfWork.When(x => x.SaveChangesAsync(Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                if (capturedProduct is not null && capturedProduct.Id == 0)
                {
                    capturedProduct.Id = 100;
                    capturedProduct.PublicId = Guid.NewGuid();
                }
            });

        ProductResponse result = await _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Null(result.CategoryPublicId);

        _productRepository.Received(1).Add(Arg.Is<Product>(p =>
            p.CategoryId == categoryId));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StoreNotFound_ThrowsStoreNotFoundException()
    {
        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>());

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        await Assert.ThrowsAsync<StoreNotFoundException>(() =>
            _service.CreateAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DuplicateSlug_ThrowsDuplicateProductSlugException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "duplicate-slug",
            Price = 999.99m
        };

        await Assert.ThrowsAsync<DuplicateProductSlugException>(() =>
            _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CategoryNotFound_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m,
            CategoryPublicId = Guid.NewGuid()
        };

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.CreateAsync(storePublicId, request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Add(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByPublicIdAsync_ProductFound_ReturnsResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Laptop",
            Slug = "laptop",
            Description = "A laptop",
            Price = 999.99m,
            CreatedAtUtc = FixedUtcNow
        };

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        ProductResponse result = await _service.GetByPublicIdAsync(storePublicId, productPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(productPublicId, result.PublicId);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("laptop", result.Slug);
        Assert.Equal("A laptop", result.Description);
        Assert.Equal(999.99m, result.Price);
    }

    [Fact]
    public async Task GetByPublicIdAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByPublicIdAsync_ProductFromDifferentStore_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 99,
            TenantId = 10,
            Name = "Laptop",
            Slug = "laptop"
        };

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.GetByPublicIdAsync(storePublicId, productPublicId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByStorePublicIdAsync_ReturnsActiveProducts()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        List<Product> products =
        [
            new() { Id = 1, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, Name = "Product 1", Slug = "product-1", Price = 10m, IsActive = true },
            new() { Id = 2, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, Name = "Product 2", Slug = "product-2", Price = 20m, IsActive = true }
        ];

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(products);

        IReadOnlyCollection<ProductResponse> result = await _service.GetByStorePublicIdAsync(storePublicId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByCategoryPublicIdAsync_ReturnsProductsInCategory()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>
            {
                new() { Id = 5, PublicId = categoryPublicId, StoreId = 42, TenantId = 10, Name = "Electronics", Slug = "electronics", IsActive = true }
            });

        List<Product> products =
        [
            new() { Id = 1, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, CategoryId = 5, Name = "Product 1", Slug = "product-1", Price = 10m, IsActive = true },
            new() { Id = 2, PublicId = Guid.NewGuid(), StoreId = 42, TenantId = 10, CategoryId = 5, Name = "Product 2", Slug = "product-2", Price = 20m, IsActive = true }
        ];

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(products);

        IReadOnlyCollection<ProductResponse> result = await _service.GetByCategoryPublicIdAsync(storePublicId, categoryPublicId, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByCategoryPublicIdAsync_CategoryNotFound_ThrowsCategoryNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _categoryRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Category, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Category>());

        await Assert.ThrowsAsync<CategoryNotFoundException>(() =>
            _service.GetByCategoryPublicIdAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesProduct()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug",
            Description = "Old description",
            Price = 10m,
            CreatedAtUtc = FixedUtcNow
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UpdateProductRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug",
            Description = "New description",
            Price = 20m
        };

        ProductResponse result = await _service.UpdateAsync(storePublicId, productPublicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("new-slug", result.Slug);

        _productRepository.Received(1).Update(Arg.Is<Product>(p =>
            p.Name == "New Name" &&
            p.Slug == "new-slug" &&
            p.Description == "New description" &&
            p.Price == 20m));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        UpdateProductRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug",
            Price = 20m
        };

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.UpdateAsync(storePublicId, Guid.NewGuid(), request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Update(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug",
            Price = 10m
        };

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        UpdateProductRequest request = new()
        {
            Name = "New Name",
            Slug = "new-slug",
            Price = 20m
        };

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.UpdateAsync(storePublicId, productPublicId, request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Update(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DuplicateSlug_ThrowsDuplicateProductSlugException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "old-slug",
            Price = 10m
        };

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _productRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateProductRequest request = new()
        {
            Name = "New Name",
            Slug = "duplicate-slug",
            Price = 20m
        };

        await Assert.ThrowsAsync<DuplicateProductSlugException>(() =>
            _service.UpdateAsync(storePublicId, productPublicId, request, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Update(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_SameSlug_DoesNotCheckDuplicate()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Old Name",
            Slug = "same-slug",
            Description = "Old description",
            Price = 10m,
            CreatedAtUtc = FixedUtcNow
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        UpdateProductRequest request = new()
        {
            Name = "New Name",
            Slug = "same-slug",
            Price = 20m
        };

        ProductResponse result = await _service.UpdateAsync(storePublicId, productPublicId, request, TestContext.Current.CancellationToken);

        Assert.Equal("New Name", result.Name);
        Assert.Equal("same-slug", result.Slug);

        _productRepository.Received(1).Update(Arg.Any<Product>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesProduct()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        _currentUser.GetRequiredUserId().Returns(userId);

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await _service.DeleteAsync(storePublicId, productPublicId, TestContext.Current.CancellationToken);

        _productRepository.Received(1).Update(Arg.Is<Product>(p =>
            p.IsActive == false &&
            p.DeletedAtUtc == FixedUtcNow));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ProductNotFound_ThrowsProductNotFoundException()
    {
        Guid storePublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product>());

        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.DeleteAsync(storePublicId, Guid.NewGuid(), TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Update(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsTenantOwnerRequiredException()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        _storeRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Store, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Store>
            {
                new() { Id = 42, PublicId = storePublicId, TenantId = 10, Name = "My Store", Slug = "my-store", IsActive = true }
            });

        Product product = new()
        {
            Id = 1,
            PublicId = productPublicId,
            StoreId = 42,
            TenantId = 10,
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        _productRepository.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Product, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Product> { product });

        _membershipRepository.AnyAsync(Arg.Any<System.Linq.Expressions.Expression<Func<TenantMembership, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<TenantOwnerRequiredException>(() =>
            _service.DeleteAsync(storePublicId, productPublicId, TestContext.Current.CancellationToken));

        _productRepository.DidNotReceive().Update(Arg.Any<Product>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
