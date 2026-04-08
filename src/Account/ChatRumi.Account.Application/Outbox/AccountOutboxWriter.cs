using ChatRum.InterCommunication;
using ChatRumi.Account.Application.Documents;
using Marten;

namespace ChatRumi.Account.Application.Outbox;

public sealed class AccountOutboxWriter(IDocumentSession session) : IOutboxWriter
{
    public Task EnqueueAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var envelope = OutboxEnvelopeFactory.Create(topic, key, value);
        session.Store(AccountOutboxMessage.FromEnvelope(envelope));
        return Task.CompletedTask;
    }
}
