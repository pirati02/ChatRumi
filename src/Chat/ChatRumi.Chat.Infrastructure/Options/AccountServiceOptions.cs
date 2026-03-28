namespace ChatRumi.Chat.Infrastructure.Options;

public sealed class AccountServiceOptions
{
    public const string SectionName = "AccountService";

    /// <summary>Base URL of the Account API (e.g. http://accountservice:8080). Aspire injects ConnectionStrings:accountservice when referenced.</summary>
    public string BaseUrl { get; set; } = "";
}
