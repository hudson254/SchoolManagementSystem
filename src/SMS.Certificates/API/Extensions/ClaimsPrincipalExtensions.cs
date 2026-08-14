using System.Security.Claims;

namespace SMS.Certificates.API.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract user information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user ID from the claims principal.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || string.IsNullOrEmpty(claim.Value))
            return null;

        return Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the user role from the claims principal.
    /// </summary>
    public static string? GetUserRole(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.Role);
        return claim?.Value;
    }
}
