using ChatRum.InterCommunication;
using Nest;

namespace ChatRumi.Feed.Application.Outbox;

public sealed class FeedOutboxWriter(IElasticClient client) : IOutboxWriter
{
    public async Task EnqueueAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default)
    {
        var message = FeedOutboxMessage.FromEnvelope(OutboxEnvelopeFactory.Create(topic, key, value));
        var response = await client.IndexAsync(
            message,
            i => i.Index(PostIndexes.Outbox).Id(message.Id).Refresh(Elasticsearch.Net.Refresh.WaitFor),
            cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException(
                $"Feed outbox enqueue failed for topic {topic}: {response.OriginalException?.Message}");
        }
    }
}
