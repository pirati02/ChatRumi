namespace ChatRumi.Account.Application.Services;

public interface IJwtAccessTokenIssuer
{
    string CreateAccessToken(Guid accountId, string email, string userName, out int expiresInSeconds);
}
