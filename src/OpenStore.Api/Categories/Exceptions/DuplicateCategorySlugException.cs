using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Categories.Exceptions;

public sealed class DuplicateCategorySlugException : OpenStoreException
{
    private const string Code = "categories.duplicate_category_slug";

    public DuplicateCategorySlugException() : base(Code, StatusCodes.Status409Conflict, "A category with this slug already exists in the store.")
    {
    }
}
