using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using E_commerce_Application.Entities;

namespace E_commerce_Application.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var identifier = principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (identifier is null || !Guid.TryParse(identifier, out var userId))
        {
            throw new UnauthorizedAccessException("User identifier is not available.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(UserRole.Admin.ToString());
    }
}


