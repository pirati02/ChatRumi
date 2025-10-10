namespace ChatRumi.Account.Domain.ValueObjects;

public struct SmsCode(string PhoneNumber, string Code)
{
    public string Key()
    {
        return $"{PhoneNumber}-smscode";
    }

    public string Otp { get; set; } = Code;
}