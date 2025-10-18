namespace ChatRumi.Account.Application.Options;

public record SmsOfficeOptions
{
    public const string Name = nameof(SmsOfficeOptions);
    public required string ApiKey { get; set; }
    public required string BaseUrl { get; set; }
}