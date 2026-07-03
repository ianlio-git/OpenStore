using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Stores.Exceptions;

public sealed class StoreNotFoundException : OpenStoreException
{
    private const string Code = "stores.store_not_found";

    public StoreNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified store was not found.")
    {
    }
}
