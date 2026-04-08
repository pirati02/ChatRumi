using ChatRum.InterCommunication;
using ChatRumi.Feed.Application.Outbox;
using Xunit;

namespace ChatRumi.Feed.Application.Tests;

public class FeedOutboxMessageTests
{
    [Fact]
    public void FromEnvelope_MapsExpectedFields()
    {
        var envelope = OutboxEnvelopeFactory.Create(
            topic: "notification-triggered",
            key: Guid.NewGuid().ToString(),
            value: new { Type = "PostReaction", PostId = Guid.NewGuid() });

        var message = FeedOutboxMessage.FromEnvelope(envelope);

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
