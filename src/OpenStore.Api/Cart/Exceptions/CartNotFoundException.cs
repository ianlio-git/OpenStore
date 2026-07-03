using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Cart.Exceptions;

public sealed class CartNotFoundException : OpenStoreException
{
    private const string Code = "cart.cart_not_found";

    public CartNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified cart was not found.")
    {
    }
}
