namespace ChatRumi.Account.Application.Options;

public class SmsOfficeOptions
{
    public const string Name = nameof(SmsOfficeOptions);
    public required string ApiKey { get; set; }
    public required string BaseUrl { get; set; }
}