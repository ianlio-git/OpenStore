using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Stores.Exceptions;

public sealed class TenantNotFoundException : OpenStoreException
{
    private const string Code = "stores.tenant_not_found";

    public TenantNotFoundException() : base(Code, StatusCodes.Status404NotFound, "The specified tenant was not found.")
    {
    }
}
