using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OpenStore.Api.Products.Contracts;
using OpenStore.Api.Products.Controllers;
using OpenStore.Api.Products.Dtos;

namespace OpenStore.Api.Tests.Products.Controllers;

public sealed class ProductsControllerTests
{
    private readonly IProductService _service;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _service = Substitute.For<IProductService>();
        _controller = new ProductsController(_service);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();

        CreateProductRequest request = new()
        {
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        ProductResponse expected = new()
        {
            PublicId = Guid.NewGuid(),
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.CreateAsync(storePublicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Create(storePublicId, request, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(ProductsController.Create), createdResult.ActionName);

        ProductResponse? body = Assert.IsType<ProductResponse>(createdResult.Value);
        Assert.Equal(expected.PublicId, body.PublicId);
        Assert.Equal(expected.Name, body.Name);
        Assert.Equal(expected.Slug, body.Slug);
        Assert.Equal(expected.Price, body.Price);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid publicId = Guid.NewGuid();

        ProductResponse expected = new()
        {
            PublicId = publicId,
            Name = "Laptop",
            Slug = "laptop",
            Price = 999.99m
        };

        _service.GetByPublicIdAsync(storePublicId, publicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByPublicId(storePublicId, publicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        ProductResponse? body = Assert.IsType<ProductResponse>(okResult.Value);
        Assert.Equal(publicId, body.PublicId);
        Assert.Equal("Laptop", body.Name);
    }

    [Fact]
    public async Task GetByStore_ReturnsOkWithList()
    {
        Guid storePublicId = Guid.NewGuid();

        List<ProductResponse> expected =
        [
            new ProductResponse { PublicId = Guid.NewGuid(), Name = "Product 1", Slug = "product-1", Price = 10m },
            new ProductResponse { PublicId = Guid.NewGuid(), Name = "Product 2", Slug = "product-2", Price = 20m }
        ];

        _service.GetByStorePublicIdAsync(storePublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByStore(storePublicId, null, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        IReadOnlyCollection<ProductResponse>? body = Assert.IsAssignableFrom<IReadOnlyCollection<ProductResponse>>(okResult.Value);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task GetByStore_WithCategoryFilter_ReturnsFilteredList()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid categoryPublicId = Guid.NewGuid();

        List<ProductResponse> expected =
        [
            new ProductResponse { PublicId = Guid.NewGuid(), Name = "Filtered Product", Slug = "filtered-product", Price = 10m }
        ];

        _service.GetByCategoryPublicIdAsync(storePublicId, categoryPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByStore(storePublicId, categoryPublicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        IReadOnlyCollection<ProductResponse>? body = Assert.IsAssignableFrom<IReadOnlyCollection<ProductResponse>>(okResult.Value);
        ProductResponse single = Assert.Single(body);
        Assert.Equal("Filtered Product", single.Name);
    }

    [Fact]
    public async Task Update_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid publicId = Guid.NewGuid();

        UpdateProductRequest request = new()
        {
            Name = "Updated Product",
            Slug = "updated-product",
            Price = 29.99m
        };

        ProductResponse expected = new()
        {
            PublicId = publicId,
            Name = "Updated Product",
            Slug = "updated-product",
            Price = 29.99m
        };

        _service.UpdateAsync(storePublicId, publicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Update(storePublicId, publicId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        ProductResponse? body = Assert.IsType<ProductResponse>(okResult.Value);
        Assert.Equal("Updated Product", body.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid publicId = Guid.NewGuid();

        IActionResult result = await _controller.Delete(storePublicId, publicId, CancellationToken.None);

        NoContentResult noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        await _service.Received(1).DeleteAsync(storePublicId, publicId, Arg.Any<CancellationToken>());
    }
}
