using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Cart.Exceptions;

public sealed class CartItemNotFoundException : OpenStoreException
{
    private const string Code = "cart.cart_item_not_found";

    public CartItemNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified cart item was not found.")
    {
    }
}
