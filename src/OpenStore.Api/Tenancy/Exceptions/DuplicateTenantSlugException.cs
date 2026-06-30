using Microsoft.AspNetCore.Http;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Tenancy.Exceptions;

public sealed class DuplicateTenantSlugException : OpenStoreException
{
    private const string Code = "tenancy.duplicate_tenant_slug";

    public DuplicateTenantSlugException() : base(Code, StatusCodes.Status409Conflict, "A tenant with this slug already exists.")
    {
    }
}
