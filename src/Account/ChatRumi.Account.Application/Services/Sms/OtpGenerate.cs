namespace ChatRumi.Account.Application.Services.Sms;

public static class OtpGenerate
{
    public static string New()
    {
        return Random.Shared.Next(1000, 9999).ToString();
    }
}