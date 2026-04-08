using ChatRum.InterCommunication;

namespace ChatRumi.Account.Application.Documents;

public sealed class AccountOutboxMessage
{
    public Guid Id { get; init; }
    public required string Topic { get; init; }
    public required string Key { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
    public DateTimeOffset CreatedOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }

    public static AccountOutboxMessage FromEnvelope(OutboxEnvelope envelope)
    {
        return new AccountOutboxMessage
        {
            Id = envelope.MessageId,
            Topic = envelope.Topic,
            Key = envelope.Key,
            EventType = envelope.EventType,
            Payload = envelope.Payload,
            OccurredOnUtc = envelope.OccurredOnUtc
        };
    }
}
