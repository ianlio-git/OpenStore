using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Tenancy.Dtos;
using OpenStore.Api.Tenancy.Exceptions;
using OpenStore.Api.Tenancy.Models;

namespace OpenStore.Api.Tenancy.Validators;

public static class TenantValidator
{
    private const int MaxNameLength = 200;
    private const int MaxSlugLength = 100;

    public static string Validate(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new TenantNameValidationException("Tenant name is required.");
        }

        if (request.Name.Trim().Length > MaxNameLength)
        {
            throw new TenantNameValidationException($"Tenant name cannot exceed {MaxNameLength} characters.");
        }

        string normalizedSlug = request.Slug.Trim();

        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            throw new TenantSlugValidationException("Slug cannot be empty.");
        }

        if (normalizedSlug.Length > MaxSlugLength)
        {
            throw new TenantSlugValidationException($"Slug cannot exceed {MaxSlugLength} characters.");
        }

        if (!IsValidSlug(normalizedSlug))
        {
            throw new TenantSlugValidationException("Slug must contain only lowercase letters, digits, and hyphens.");
        }

        return normalizedSlug;
    }

    private static bool IsValidSlug(string slug) => slug.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    public static async Task ValidateSlugIsAvailableAsync(string slug, IRepository<Tenant> tenantRepository, CancellationToken cancellationToken)
    {
        bool slugExists = await tenantRepository.AnyAsync(t => t.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new DuplicateTenantSlugException();
        }
    }
}
