using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Stores.Contracts;
using OpenStore.Api.Stores.Dtos;

namespace OpenStore.Api.Stores.Controllers;

[ApiController]
[Route("api/stores")]
[Authorize]
public sealed class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request, CancellationToken cancellationToken)
    {
        StoreResponse result = await _storeService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Create), new { id = result.PublicId }, result);
    }

    [HttpGet("{publicId}")]
    public async Task<IActionResult> GetByPublicId(Guid publicId, CancellationToken cancellationToken)
    {
        StoreResponse result = await _storeService.GetByPublicIdAsync(publicId, cancellationToken);

        return Ok(result);
    }

    [HttpGet("tenants/{tenantPublicId}/stores")]
    public async Task<IActionResult> GetByTenantPublicId(Guid tenantPublicId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<StoreResponse> result = await _storeService.GetByTenantPublicIdAsync(tenantPublicId, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{publicId}")]
    public async Task<IActionResult> Update(Guid publicId, [FromBody] UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        StoreResponse result = await _storeService.UpdateAsync(publicId, request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{publicId}")]
    public async Task<IActionResult> Delete(Guid publicId, CancellationToken cancellationToken)
    {
        await _storeService.DeleteAsync(publicId, cancellationToken);

        return NoContent();
    }
}
