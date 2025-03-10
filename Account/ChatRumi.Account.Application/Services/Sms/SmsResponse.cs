namespace ChatRumi.Account.Application.Services.Sms;

public class SmsResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public object? Output { get; set; }
    public required string ErrorCode { get; set; }
}