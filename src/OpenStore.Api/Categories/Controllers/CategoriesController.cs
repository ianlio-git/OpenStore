using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Categories.Contracts;
using OpenStore.Api.Categories.Dtos;

namespace OpenStore.Api.Categories.Controllers;

[ApiController]
[Route("api/stores/{storePublicId}/categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid storePublicId, [FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        CategoryResponse result = await _categoryService.CreateAsync(storePublicId, request, cancellationToken);

        return CreatedAtAction(nameof(Create), new { storePublicId, id = result.PublicId }, result);
    }

    [HttpGet("{publicId}")]
    public async Task<IActionResult> GetByPublicId(Guid storePublicId, Guid publicId, CancellationToken cancellationToken)
    {
        CategoryResponse result = await _categoryService.GetByPublicIdAsync(storePublicId, publicId, cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByStore(Guid storePublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CategoryResponse> result = await _categoryService.GetByStorePublicIdAsync(storePublicId, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{publicId}")]
    public async Task<IActionResult> Update(Guid storePublicId, Guid publicId, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        CategoryResponse result = await _categoryService.UpdateAsync(storePublicId, publicId, request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> Delete(Guid storePublicId, Guid publicId, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(storePublicId, publicId, cancellationToken);

        return NoContent();
    }
}
