namespace ChatRumi.Account.Application.Services.Sms;

public sealed record SmsResponse
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public object? Output { get; init; }
    public required int ErrorCode { get; init; }
}