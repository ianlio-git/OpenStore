# Approved Code Examples

## 1. Create service with one return

```csharp
public sealed class CreateProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryReader _categoryReader;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateProductService(
        IProductRepository productRepository,
        ICategoryReader categoryReader,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _productRepository = productRepository;
        _categoryReader = categoryReader;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    private async Task ValidateCategoryAsync(
        Guid storeId,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId is not null)
        {
            bool categoryExists = await _categoryReader.ExistsAsync(
                _tenantContext.TenantId,
                storeId,
                categoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new CategoryNotFoundException(categoryId.Value);
            }
        }
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        ProductResponse result;

        ProductValidationHelper.Validate(request);

        await ValidateCategoryAsync(
            request.StoreId,
            request.CategoryId,
            cancellationToken);

        Product product = Product.Create(
            Guid.NewGuid(),
            _tenantContext.TenantId,
            request.StoreId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.CurrencyCode,
            _dateTimeProvider.UtcNow);

        await _productRepository.AddAsync(product, cancellationToken);

        result = ProductMapper.ToResponse(product);

        return result;
    }
}
```

---

## 2. Update service

```csharp
public async Task<ProductResponse> UpdateAsync(
    Guid productId,
    UpdateProductRequest request,
    CancellationToken cancellationToken)
{
    ProductResponse result;

    Product product = await _productRepository.GetRequiredAsync(
        _tenantContext.TenantId,
        productId,
        cancellationToken);

    ProductValidationHelper.Validate(request);

    product.Update(
        request.Name,
        request.Description,
        request.Price,
        request.CurrencyCode,
        _dateTimeProvider.UtcNow);

    await _productRepository.SaveChangesAsync(cancellationToken);

    result = ProductMapper.ToResponse(product);

    return result;
}
```

---

## 3. Test class

```csharp
public sealed class CreateProductServiceTests
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryReader _categoryReader;
    private readonly ITenantContext _tenantContext;
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private readonly CreateProductService _service;

    public CreateProductServiceTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _categoryReader = Substitute.For<ICategoryReader>();
        _tenantContext = Substitute.For<ITenantContext>();
        _dateTimeProvider = new FakeDateTimeProvider(
            new DateTimeOffset(2026, 6, 29, 12, 0, 0, TimeSpan.Zero));

        _tenantContext.TenantId.Returns(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        _service = new CreateProductService(
            _productRepository,
            _categoryReader,
            _tenantContext,
            _dateTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        // Arrange
        Guid storeId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

        Guid categoryId = Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

        CreateProductRequest request = new(
            storeId,
            categoryId,
            "Black shirt",
            "Cotton shirt",
            25000m,
            "ARS");

        _categoryReader.ExistsAsync(
                _tenantContext.TenantId,
                storeId,
                categoryId,
                Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        ProductResponse result = await _service.CreateAsync(
            request,
            CancellationToken.None);

        // Assert
        Assert.Equal("Black shirt", result.Name);
        Assert.Equal(25000m, result.Price);
        Assert.Equal("ARS", result.CurrencyCode);

        await _productRepository.Received(1).AddAsync(
            Arg.Is<Product>(product =>
                product.TenantId == _tenantContext.TenantId &&
                product.StoreId == storeId &&
                product.CreatedAtUtc == _dateTimeProvider.UtcNow),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_CategoryBelongsToAnotherTenant_ThrowsCategoryNotFoundException()
    {
        // Arrange
        CreateProductRequest request = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Black shirt",
            null,
            25000m,
            "ARS");

        _categoryReader.ExistsAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Task Act() => _service.CreateAsync(
            request,
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<CategoryNotFoundException>(Act);
    }
}
```

---

## 4. Typed exception

```csharp
public sealed class ProductNotFoundException : NotFoundException
{
    private const string Code = "catalog.product_not_found";

    public ProductNotFoundException(Guid productId)
        : base(Code, $"The product '{productId}' was not found.")
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
```

---

## 5. Date-time provider

```csharp
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
```

```csharp
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

---

## 6. Validation helper

```csharp
public static class ProductValidationHelper
{
    private const int MaximumNameLength = 200;

    private static void ValidateCurrencyCode(string currencyCode)
    {
        if (currencyCode.Length != 3)
        {
            throw new InvalidCurrencyCodeException(currencyCode);
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ProductNameRequiredException();
        }

        if (name.Length > MaximumNameLength)
        {
            throw new ProductNameTooLongException(MaximumNameLength);
        }
    }

    private static void ValidatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new InvalidProductPriceException(price);
        }
    }

    public static void Validate(CreateProductRequest request)
    {
        ValidateName(request.Name);
        ValidatePrice(request.Price);
        ValidateCurrencyCode(request.CurrencyCode);
    }
}
```
