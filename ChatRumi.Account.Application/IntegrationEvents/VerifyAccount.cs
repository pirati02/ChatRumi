using MassTransit;

namespace ChatRumi.Account.Application.IntegrationEvents;

public class VerifyAccount
{
    public class Event
    {
        public Guid AccountId { get; init; }
        public required string Email { get; init; }
        public required string PhoneNumber { get; init; }
        public DateTime Timestamp { get; init; }
    }

    public class EventHandler(
        
    ) : IConsumer<Event>
    {
        public Task Consume(ConsumeContext<Event> context)
        {
            //Bla bla verify account email sent
            //Bla bla verify account OTP sent
            return Task.CompletedTask;
        }
    }
}