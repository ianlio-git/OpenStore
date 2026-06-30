using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Tenancy.Contracts;
using OpenStore.Api.Tenancy.Dtos;

namespace OpenStore.Api.Tenancy.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        CreateTenantResponse result = await _tenantService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Create), new { id = result.PublicId }, result);
    }
}
