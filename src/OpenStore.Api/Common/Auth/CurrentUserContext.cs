using System.Security.Claims;
using OpenStore.Api.Common.Contracts;
using OpenStore.Api.Common.Errors;

namespace OpenStore.Api.Common.Auth;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetRequiredUserId()
    {
        if (!IsAuthenticated)
        {
            throw new OpenStoreException("auth.unauthenticated", StatusCodes.Status401Unauthorized, "Authentication is required.");
        }

        ClaimsPrincipal user = _httpContextAccessor.HttpContext!.User;

        string? sub = user.FindFirst("sub")?.Value;

        if (sub is null || !Guid.TryParse(sub, out Guid userId))
        {
            throw new OpenStoreException("auth.unauthenticated", StatusCodes.Status401Unauthorized, "Authentication is required.");
        }

        return userId;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
