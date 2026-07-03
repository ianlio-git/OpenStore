using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Products.Exceptions;

public sealed class ProductNotFoundException : OpenStoreException
{
    private const string Code = "products.product_not_found";

    public ProductNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified product was not found.")
    {
    }
}
