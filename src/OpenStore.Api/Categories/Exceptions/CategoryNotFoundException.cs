using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Categories.Exceptions;

public sealed class CategoryNotFoundException : OpenStoreException
{
    private const string Code = "categories.category_not_found";

    public CategoryNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified category was not found.")
    {
    }
}
