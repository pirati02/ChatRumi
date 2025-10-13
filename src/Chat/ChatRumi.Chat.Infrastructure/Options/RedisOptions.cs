namespace ChatRumi.Chat.Infrastructure.Options;

public class RedisOptions
{
    public const string Name = nameof(RedisOptions);
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string User { get; set; }
    public required string Password { get; set; }
    public long Expiration { get; set; }
}