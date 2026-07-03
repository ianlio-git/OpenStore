using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Products.Exceptions;

public sealed class DuplicateProductSlugException : OpenStoreException
{
    private const string Code = "products.duplicate_product_slug";

    public DuplicateProductSlugException() : base(Code, StatusCodes.Status409Conflict, "A product with this slug already exists in the store.")
    {
    }
}
