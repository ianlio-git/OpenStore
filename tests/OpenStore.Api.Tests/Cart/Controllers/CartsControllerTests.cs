using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OpenStore.Api.Cart.Contracts;
using OpenStore.Api.Cart.Controllers;
using OpenStore.Api.Cart.Dtos;

namespace OpenStore.Api.Tests.Cart.Controllers;

public sealed class CartsControllerTests
{
    private readonly ICartService _service;
    private readonly CartsController _controller;

    public CartsControllerTests()
    {
        _service = Substitute.For<ICartService>();
        _controller = new CartsController(_service);
    }

    [Fact]
    public async Task Create_ReturnsCreatedWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();

        CartResponse expected = new()
        {
            PublicId = Guid.NewGuid(),
            StorePublicId = storePublicId,
            Items = [],
            Total = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.CreateAsync(storePublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Create(storePublicId, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(CartsController.GetByPublicId), createdResult.ActionName);
        Assert.Equal(storePublicId, createdResult.RouteValues!["storePublicId"]);
        Assert.Equal(expected.PublicId, createdResult.RouteValues!["cartPublicId"]);

        CartResponse? body = Assert.IsType<CartResponse>(createdResult.Value);
        Assert.Equal(expected.PublicId, body.PublicId);
        Assert.Equal(storePublicId, body.StorePublicId);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();

        CartResponse expected = new()
        {
            PublicId = cartPublicId,
            StorePublicId = storePublicId,
            Items = [],
            Total = 0,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.GetByPublicIdAsync(storePublicId, cartPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByPublicId(storePublicId, cartPublicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        CartResponse? body = Assert.IsType<CartResponse>(okResult.Value);
        Assert.Equal(cartPublicId, body.PublicId);
        Assert.Equal(storePublicId, body.StorePublicId);
    }

    [Fact]
    public async Task AddItem_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid productPublicId = Guid.NewGuid();

        AddCartItemRequest request = new()
        {
            ProductPublicId = productPublicId,
            Quantity = 2
        };

        CartResponse expected = new()
        {
            PublicId = cartPublicId,
            StorePublicId = storePublicId,
            Items =
            [
                new CartItemResponse
                {
                    PublicId = Guid.NewGuid(),
                    ProductPublicId = productPublicId,
                    ProductName = "Laptop",
                    ProductSlug = "laptop",
                    UnitPrice = 999.99m,
                    Quantity = 2,
                    Subtotal = 1999.98m
                }
            ],
            Total = 1999.98m,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.AddItemAsync(storePublicId, cartPublicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.AddItem(storePublicId, cartPublicId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        CartResponse? body = Assert.IsType<CartResponse>(okResult.Value);
        Assert.Single(body.Items);
        Assert.Equal(1999.98m, body.Total);
    }

    [Fact]
    public async Task UpdateItemQuantity_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid cartItemPublicId = Guid.NewGuid();

        UpdateCartItemRequest request = new()
        {
            Quantity = 5
        };

        CartResponse expected = new()
        {
            PublicId = cartPublicId,
            StorePublicId = storePublicId,
            Items =
            [
                new CartItemResponse
                {
                    PublicId = cartItemPublicId,
                    ProductPublicId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    ProductSlug = "laptop",
                    UnitPrice = 999.99m,
                    Quantity = 5,
                    Subtotal = 4999.95m
                }
            ],
            Total = 4999.95m,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.UpdateItemQuantityAsync(storePublicId, cartPublicId, cartItemPublicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.UpdateItemQuantity(storePublicId, cartPublicId, cartItemPublicId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        CartResponse? body = Assert.IsType<CartResponse>(okResult.Value);
        Assert.Equal(5, body.Items[0].Quantity);
    }

    [Fact]
    public async Task RemoveItem_ReturnsNoContent()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid cartPublicId = Guid.NewGuid();
        Guid cartItemPublicId = Guid.NewGuid();

        IActionResult result = await _controller.RemoveItem(storePublicId, cartPublicId, cartItemPublicId, CancellationToken.None);

        NoContentResult noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        await _service.Received(1).RemoveItemAsync(storePublicId, cartPublicId, cartItemPublicId, Arg.Any<CancellationToken>());
    }
}
