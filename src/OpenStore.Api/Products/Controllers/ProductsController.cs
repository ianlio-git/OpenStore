using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Products.Contracts;
using OpenStore.Api.Products.Dtos;

namespace OpenStore.Api.Products.Controllers;

[ApiController]
[Route("api/stores/{storePublicId}/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid storePublicId, [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        ProductResponse result = await _productService.CreateAsync(storePublicId, request, cancellationToken);

        return CreatedAtAction(nameof(Create), new { storePublicId, id = result.PublicId }, result);
    }

    [HttpGet("{publicId}")]
    public async Task<IActionResult> GetByPublicId(Guid storePublicId, Guid publicId, CancellationToken cancellationToken)
    {
        ProductResponse result = await _productService.GetByPublicIdAsync(storePublicId, publicId, cancellationToken);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByStore(Guid storePublicId, [FromQuery] Guid? categoryPublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProductResponse> result;

        if (categoryPublicId.HasValue)
        {
            result = await _productService.GetByCategoryPublicIdAsync(storePublicId, categoryPublicId.Value, cancellationToken);
        }
        else
        {
            result = await _productService.GetByStorePublicIdAsync(storePublicId, cancellationToken);
        }

        return Ok(result);
    }

    [HttpPut("{publicId}")]
    public async Task<IActionResult> Update(Guid storePublicId, Guid publicId, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        ProductResponse result = await _productService.UpdateAsync(storePublicId, publicId, request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> Delete(Guid storePublicId, Guid publicId, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(storePublicId, publicId, cancellationToken);

        return NoContent();
    }
}
