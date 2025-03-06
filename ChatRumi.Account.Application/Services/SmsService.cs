using System.Net.Http.Json;
using ChatRumi.Account.Application.Options;
using Microsoft.Extensions.Options;

namespace ChatRumi.Account.Application.Services;

public interface ISmsService
{
    Task<SmsResponse> SendSmsAsync(string phoneNumber, string message);
}

public class SmsOfficeService(
    IOptions<SmsOfficeOptions> options,
    HttpClient client
) : ISmsService
{
    public async Task<SmsResponse> SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var url = $"api/v2/send/?key={options.Value.ApiKey}&destination={phoneNumber}&sender=chatrum&content={message}&urgent=true";
            var result = await client.GetAsync(url);
            return await result.Content.ReadFromJsonAsync<SmsResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending SMS: {ex.Message}");
            return null!;
        }
    }
}

public class SmsResponse
{
    public bool Success { get; set; }
    public required string Message { get; set; }
    public object? Output { get; set; }
    public required string ErrorCode { get; set; }
}