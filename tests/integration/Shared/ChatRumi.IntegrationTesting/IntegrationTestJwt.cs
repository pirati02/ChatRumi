using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatRumi.Infrastructure;
using Microsoft.IdentityModel.Tokens;

namespace ChatRumi.IntegrationTesting;

public static class IntegrationTestJwt
{
    public static string CreateAccessToken(JwtOptions jwt, Guid accountId, string email = "test@example.com", string userName = "testuser")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, accountId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.UniqueName, userName)
        };
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
