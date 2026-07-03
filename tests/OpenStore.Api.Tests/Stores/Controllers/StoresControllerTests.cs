using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OpenStore.Api.Stores.Contracts;
using OpenStore.Api.Stores.Controllers;
using OpenStore.Api.Stores.Dtos;

namespace OpenStore.Api.Tests.Stores.Controllers;

public sealed class StoresControllerTests
{
    private readonly IStoreService _service;
    private readonly StoresController _controller;

    public StoresControllerTests()
    {
        _service = Substitute.For<IStoreService>();
        _controller = new StoresController(_service);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithResponse()
    {
        CreateStoreRequest request = new()
        {
            TenantPublicId = Guid.NewGuid(),
            Name = "Test Store",
            Slug = "test-store"
        };

        StoreResponse expected = new()
        {
            PublicId = Guid.NewGuid(),
            Name = "Test Store",
            Slug = "test-store",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Create(request, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(StoresController.Create), createdResult.ActionName);

        StoreResponse? body = Assert.IsType<StoreResponse>(createdResult.Value);
        Assert.Equal(expected.PublicId, body.PublicId);
        Assert.Equal(expected.Name, body.Name);
        Assert.Equal(expected.Slug, body.Slug);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsOkWithResponse()
    {
        Guid publicId = Guid.NewGuid();

        StoreResponse expected = new()
        {
            PublicId = publicId,
            Name = "Test Store",
            Slug = "test-store"
        };

        _service.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByPublicId(publicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        StoreResponse? body = Assert.IsType<StoreResponse>(okResult.Value);
        Assert.Equal(publicId, body.PublicId);
        Assert.Equal("Test Store", body.Name);
    }

    [Fact]
    public async Task GetByTenantPublicId_ReturnsOkWithList()
    {
        Guid tenantPublicId = Guid.NewGuid();

        List<StoreResponse> expected =
        [
            new StoreResponse { PublicId = Guid.NewGuid(), Name = "Store 1", Slug = "store-1" },
            new StoreResponse { PublicId = Guid.NewGuid(), Name = "Store 2", Slug = "store-2" }
        ];

        _service.GetByTenantPublicIdAsync(tenantPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByTenantPublicId(tenantPublicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        IReadOnlyCollection<StoreResponse>? body = Assert.IsAssignableFrom<IReadOnlyCollection<StoreResponse>>(okResult.Value);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task Update_ReturnsOkWithResponse()
    {
        Guid publicId = Guid.NewGuid();

        UpdateStoreRequest request = new()
        {
            Name = "Updated Store",
            Slug = "updated-store"
        };

        StoreResponse expected = new()
        {
            PublicId = publicId,
            Name = "Updated Store",
            Slug = "updated-store"
        };

        _service.UpdateAsync(publicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Update(publicId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        StoreResponse? body = Assert.IsType<StoreResponse>(okResult.Value);
        Assert.Equal("Updated Store", body.Name);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        Guid publicId = Guid.NewGuid();

        IActionResult result = await _controller.Delete(publicId, CancellationToken.None);

        NoContentResult noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        await _service.Received(1).DeleteAsync(publicId, Arg.Any<CancellationToken>());
    }
}
