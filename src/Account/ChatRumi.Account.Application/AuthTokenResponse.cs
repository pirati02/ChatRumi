namespace ChatRumi.Account.Application;

public sealed record AuthTokenResponse(
    string access_token,
    string token_type,
    int expires_in,
    string refresh_token,
    int refresh_expires_in);
