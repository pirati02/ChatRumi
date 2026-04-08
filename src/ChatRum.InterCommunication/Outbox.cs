using System.Text.Json;

namespace ChatRum.InterCommunication;

public sealed record OutboxEnvelope(
    Guid MessageId,
    string Topic,
    string Key,
    string EventType,
    string Payload,
    DateTimeOffset OccurredOnUtc
);

public sealed record OutboxRelayOptions
{
    public const string SectionName = "Outbox";

    public int BatchSize { get; init; } = 50;
    public int PollIntervalMs { get; init; } = 1000;
    public int MaxAttempts { get; init; } = 10;
    public int InitialRetryDelayMs { get; init; } = 500;
    public int MaxRetryDelayMs { get; init; } = 30000;
}

public interface IOutboxWriter
{
    Task EnqueueAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default);
}

public static class OutboxEnvelopeFactory
{
    public static OutboxEnvelope Create<TEvent>(string topic, string key, TEvent value)
    {
        return new OutboxEnvelope(
            MessageId: Guid.NewGuid(),
            Topic: topic,
            Key: key,
            EventType: typeof(TEvent).FullName ?? typeof(TEvent).Name,
            Payload: JsonSerializer.Serialize(value),
            OccurredOnUtc: DateTimeOffset.UtcNow
        );
    }
}
