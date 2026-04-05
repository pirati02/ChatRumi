using ChatRum.InterCommunication;

namespace ChatRumi.IntegrationTesting;

public sealed class NoOpDispatcher : IDispatcher
{
    public Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
