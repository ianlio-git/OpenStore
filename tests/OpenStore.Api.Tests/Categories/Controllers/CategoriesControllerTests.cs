using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using OpenStore.Api.Categories.Contracts;
using OpenStore.Api.Categories.Controllers;
using OpenStore.Api.Categories.Dtos;

namespace OpenStore.Api.Tests.Categories.Controllers;

public sealed class CategoriesControllerTests
{
    private readonly ICategoryService _service;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _service = Substitute.For<ICategoryService>();
        _controller = new CategoriesController(_service);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();

        CreateCategoryRequest request = new()
        {
            Name = "Electronics",
            Slug = "electronics"
        };

        CategoryResponse expected = new()
        {
            PublicId = Guid.NewGuid(),
            Name = "Electronics",
            Slug = "electronics",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _service.CreateAsync(storePublicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Create(storePublicId, request, CancellationToken.None);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(CategoriesController.Create), createdResult.ActionName);

        CategoryResponse? body = Assert.IsType<CategoryResponse>(createdResult.Value);
        Assert.Equal(expected.PublicId, body.PublicId);
        Assert.Equal(expected.Name, body.Name);
        Assert.Equal(expected.Slug, body.Slug);
    }

    [Fact]
    public async Task GetByPublicId_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid publicId = Guid.NewGuid();

        CategoryResponse expected = new()
        {
            PublicId = publicId,
            Name = "Electronics",
            Slug = "electronics"
        };

        _service.GetByPublicIdAsync(storePublicId, publicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByPublicId(storePublicId, publicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        CategoryResponse? body = Assert.IsType<CategoryResponse>(okResult.Value);
        Assert.Equal(publicId, body.PublicId);
        Assert.Equal("Electronics", body.Name);
    }

    [Fact]
    public async Task GetByStore_ReturnsOkWithList()
    {
        Guid storePublicId = Guid.NewGuid();

        List<CategoryResponse> expected =
        [
            new CategoryResponse { PublicId = Guid.NewGuid(), Name = "Cat 1", Slug = "cat-1" },
            new CategoryResponse { PublicId = Guid.NewGuid(), Name = "Cat 2", Slug = "cat-2" }
        ];

        _service.GetByStorePublicIdAsync(storePublicId, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.GetByStore(storePublicId, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        IReadOnlyCollection<CategoryResponse>? body = Assert.IsAssignableFrom<IReadOnlyCollection<CategoryResponse>>(okResult.Value);
        Assert.Equal(2, body.Count);
    }

    [Fact]
    public async Task Update_ReturnsOkWithResponse()
    {
        Guid storePublicId = Guid.NewGuid();
        Guid publicId = Guid.NewGuid();

        UpdateCategoryRequest request = new()
        {
            Name = "Updated Category",
            Slug = "updated-category"
        };

        CategoryResponse expected = new()
        {
            PublicId = publicId,
            Name = "Updated Category",
            Slug = "updated-category"
        };

        _service.UpdateAsync(storePublicId, publicId, request, Arg.Any<CancellationToken>()).Returns(expected);

        IActionResult result = await _controller.Update(storePublicId, publicId, request, CancellationToken.None);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);

        CategoryResponse? body = Assert.IsType<CategoryResponse>(okResult.Value);
        Assert.Equal("Updated Category", body.Name);
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
