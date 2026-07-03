using Microsoft.AspNetCore.Mvc;
using OpenStore.Api.Cart.Contracts;
using OpenStore.Api.Cart.Dtos;

namespace OpenStore.Api.Cart.Controllers;

[ApiController]
[Route("api/stores/{storePublicId}/carts")]
public sealed class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid storePublicId, CancellationToken cancellationToken)
    {
        CartResponse result = await _cartService.CreateAsync(storePublicId, cancellationToken);

        return CreatedAtAction(nameof(GetByPublicId), new { storePublicId, cartPublicId = result.PublicId }, result);
    }

    [HttpGet("{cartPublicId}")]
    public async Task<IActionResult> GetByPublicId(Guid storePublicId, Guid cartPublicId, CancellationToken cancellationToken)
    {
        CartResponse result = await _cartService.GetByPublicIdAsync(storePublicId, cartPublicId, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{cartPublicId}/items")]
    public async Task<IActionResult> AddItem(Guid storePublicId, Guid cartPublicId, [FromBody] AddCartItemRequest request, CancellationToken cancellationToken)
    {
        CartResponse result = await _cartService.AddItemAsync(storePublicId, cartPublicId, request, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{cartPublicId}/items/{cartItemPublicId}")]
    public async Task<IActionResult> UpdateItemQuantity(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        CartResponse result = await _cartService.UpdateItemQuantityAsync(storePublicId, cartPublicId, cartItemPublicId, request, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{cartPublicId}/items/{cartItemPublicId}")]
    public async Task<IActionResult> RemoveItem(Guid storePublicId, Guid cartPublicId, Guid cartItemPublicId, CancellationToken cancellationToken)
    {
        await _cartService.RemoveItemAsync(storePublicId, cartPublicId, cartItemPublicId, cancellationToken);

        return NoContent();
    }
}
