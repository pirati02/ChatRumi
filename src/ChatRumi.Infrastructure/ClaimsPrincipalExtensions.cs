using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatRumi.Infrastructure;

/// <summary>
/// Resolves the account id from JWT <c>sub</c> (issuer should set <c>sub</c> to the account id).
/// Supports both raw <c>sub</c> and the inbound-mapped <see cref="ClaimTypes.NameIdentifier"/> claim.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static bool TryGetAccountId(this ClaimsPrincipal? user, out Guid accountId)
    {
        accountId = Guid.Empty;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return sub is not null && Guid.TryParse(sub, out accountId);
    }
}
