using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatRumi.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatRumi.Account.Application.Services;

public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options) : IJwtAccessTokenIssuer
{
    public string CreateAccessToken(Guid accountId, string email, string userName, out int expiresInSeconds)
    {
        var jwt = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        expiresInSeconds = jwt.AccessTokenExpirationMinutes * 60;
        var expires = DateTime.UtcNow.AddMinutes(jwt.AccessTokenExpirationMinutes);
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
            expires: expires,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
