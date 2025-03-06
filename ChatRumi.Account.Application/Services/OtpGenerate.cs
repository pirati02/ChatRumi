namespace ChatRumi.Account.Application.Services;

public class OtpGenerate
{
    public static string New()
    {
        return Random.Shared.Next(1000, 9999).ToString();
    }
}