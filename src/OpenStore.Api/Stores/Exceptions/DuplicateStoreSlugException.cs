using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Stores.Exceptions;

public sealed class DuplicateStoreSlugException : OpenStoreException
{
    private const string Code = "stores.duplicate_store_slug";

    public DuplicateStoreSlugException() : base(Code, StatusCodes.Status409Conflict, "A store with this slug already exists in the tenant.")
    {
    }
}
