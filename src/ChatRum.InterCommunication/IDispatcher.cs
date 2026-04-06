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

    public Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(value) };

        try
        {
            _producer.Produce(topic, message, deliveryReport =>
            {
                if (deliveryReport.Error.IsError)
                {
                    logger.LogWarning(
                        "Kafka failed to deliver message to topic {Topic}. Error: {Error}",
                        topic,
                        deliveryReport.Error.Reason);
                    return;
                }

                logger.LogInformation(
                    "Kafka delivered message to {TopicPartitionOffset}",
                    deliveryReport.TopicPartitionOffset);
            });
        }
        catch (ProduceException<string, string> ex)
        {
            logger.LogWarning(
                ex,
                "Kafka produce enqueue failed for topic {Topic}",
                topic);
        }

        return Task.CompletedTask;
    }
}
