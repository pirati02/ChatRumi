using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication;

public interface IDispatcher
{
    Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default);
}

public class KafkaProducer : IDispatcher
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.ConnectionString,
            
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<TEvent>(string topic, string key, TEvent value, CancellationToken cancellationToken = default)
    {
        var message = new Message<string, string> { Key = key, Value = JsonSerializer.Serialize(value) };

        var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken: cancellationToken);

        Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
    }
}