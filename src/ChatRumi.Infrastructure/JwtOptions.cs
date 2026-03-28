namespace ChatRumi.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "";

    public string Audience { get; set; } = "";

    /// <summary>Symmetric key for HS256; use at least 32 characters.</summary>
    public string SigningKey { get; set; } = "";

    public int AccessTokenExpirationMinutes { get; set; } = 60;
}
