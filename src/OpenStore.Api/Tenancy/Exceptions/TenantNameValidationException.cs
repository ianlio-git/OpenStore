using Microsoft.AspNetCore.Http;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Tenancy.Exceptions;

public sealed class TenantNameValidationException : OpenStoreException
{
    private const string Code = "tenancy.invalid_tenant_name";

    public TenantNameValidationException(string detail)
        : base(Code, StatusCodes.Status400BadRequest, detail)
    {
    }
}
