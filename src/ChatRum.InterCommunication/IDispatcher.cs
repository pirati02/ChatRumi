using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication;

public interface IDispatcher
{
    Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default);
}

public class KafkaProducer(
    IOptions<KafkaOptions> options,
    ILogger<KafkaProducer> logger
) : IDispatcher
{
    private readonly IProducer<string, string> _producer = new ProducerBuilder<string, string>(BuildProducerConfig(options.Value))
        .Build();

    private static ProducerConfig BuildProducerConfig(KafkaOptions o)
    {
        if (!Enum.TryParse<CompressionType>(o.CompressionType, ignoreCase: true, out var compression))
        {
            compression = CompressionType.Lz4;
        }

        return new ProducerConfig
        {
            BootstrapServers = o.ConnectionString,
            CompressionType = compression,
            LingerMs = o.LingerMs,
            BatchSize = o.BatchSizeBytes,
            Acks = o.ProducerAcks
        };
    }

    public async Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(value) };

        var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Kafka delivered message to {TopicPartitionOffset}",
            deliveryResult.TopicPartitionOffset);
    }
}
