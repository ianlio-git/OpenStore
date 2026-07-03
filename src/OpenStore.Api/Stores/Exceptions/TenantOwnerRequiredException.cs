using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Stores.Exceptions;

public sealed class TenantOwnerRequiredException : OpenStoreException
{
    private const string Code = "stores.tenant_owner_required";

    public TenantOwnerRequiredException() : base(Code, StatusCodes.Status403Forbidden, "Only a tenant owner can create stores in this tenant.")
    {
    }
}
