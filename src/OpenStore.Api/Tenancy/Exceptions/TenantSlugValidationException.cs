using Microsoft.AspNetCore.Http;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Tenancy.Exceptions;

public sealed class TenantSlugValidationException : OpenStoreException
{
    private const string Code = "tenancy.invalid_tenant_slug";

    public TenantSlugValidationException(string detail) : base(Code, StatusCodes.Status400BadRequest, detail)
    {
    }
}
