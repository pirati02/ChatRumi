using ChatRum.InterCommunication;
using ChatRumi.Account.Application.Documents;
using Xunit;

namespace ChatRumi.Account.Application.Tests;

public class AccountOutboxMessageTests
{
    [Fact]
    public void FromEnvelope_MapsExpectedFields()
    {
        var envelope = OutboxEnvelopeFactory.Create(
            topic: "account-updated",
            key: Guid.NewGuid().ToString(),
            value: new { Id = Guid.NewGuid(), Name = "Alice" });

        var message = AccountOutboxMessage.FromEnvelope(envelope);

        Assert.Equal(envelope.MessageId, message.Id);
        Assert.Equal(envelope.Topic, message.Topic);
        Assert.Equal(envelope.Key, message.Key);
        Assert.Equal(envelope.EventType, message.EventType);
        Assert.Equal(envelope.Payload, message.Payload);
        Assert.Equal(envelope.OccurredOnUtc, message.OccurredOnUtc);
        Assert.Equal(0, message.AttemptCount);
        Assert.Null(message.ProcessedOnUtc);
    }
}
