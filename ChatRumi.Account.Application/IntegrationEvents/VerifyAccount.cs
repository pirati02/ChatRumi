using ChatRumi.Account.Application.Options;
using ChatRumi.Account.Application.Services;
using ChatRumi.Account.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ChatRumi.Account.Application.IntegrationEvents;

public class VerifyAccount
{
    public class Event
    {
        public Guid AccountId { get; init; }
        public required string Email { get; init; }
        public required string PhoneNumber { get; init; }
        public required string CountryCode { get; set; }
    }

    public class EventHandler(
        IConnectionMultiplexer connectionMultiplexer,
        ISmsService smsService,
        IOptions<RedisOptions> options
    ) : IConsumer<Event>
    {
        public async Task Consume(ConsumeContext<Event> context)
        {
            await using (connectionMultiplexer)
            {
                try
                {
                    if (await SendSmsOtpAsync(context))
                    {
                        throw new Exception("Internal exception occured, sending sms");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private async Task<bool> SendSmsOtpAsync(ConsumeContext<Event> context)
        { 
            var database = connectionMultiplexer.GetDatabase(0);
            var smsCode = new SmsCode(context.Message.PhoneNumber, OtpGenerate.New());
            if (!await database.StringSetAsync(smsCode.Key(), smsCode.Otp, TimeSpan.FromMinutes(options.Value.Expiration))) return false;
           
            var result = await smsService.SendSmsAsync($"{context.Message.CountryCode}{context.Message.PhoneNumber}", $"Your otp code is: {smsCode.Otp}");
            return result.Success && result.ErrorCode.Equals("0", StringComparison.OrdinalIgnoreCase);
        }

        // private async Task SendEmailOtpAsync(ConsumeContext<Event> context)
        // {
        //     var otp = OtpGenerate.New();
        //     var key = $"{context.Message.AccountId}-email-{context.Message.PhoneNumber}";
        //     var database = connectionMultiplexer.GetDatabase(0);
        //     if (await database.StringSetAsync(key, otp, TimeSpan.FromMinutes(otpOptions.Value.Expiration)))
        //     {
        //         var request = new SendEmailRequest
        //         {
        //             Source = "mail.chatrum.space",
        //             Destination = new Destination
        //             {
        //                 ToAddresses = new List<string> { context.Message.Email }
        //             },
        //             Message = new Message
        //             {
        //                 Subject = new Content("ChatRum Verification"),
        //                 Body = new Body
        //                 {
        //                     Html = new Content($"Your otp code is: {otp}")
        //                 }
        //             }
        //         };
        //         await simpleEmailService.SendEmailAsync(request, context.CancellationToken);
        //     }
        // }
    }
}