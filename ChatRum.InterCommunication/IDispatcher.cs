using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication;

public interface IDispatcher
{
    Task ProduceAsync(string topic, string key, string value);
}

public class KafkaProducer : IDispatcher
{
    private readonly IProducer<string, string> _producer;

    public KafkaProducer(IOptions<KafkaOptions> options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.ConnectionString
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync(string topic, string key, string value)
    {
        var message = new Message<string, string> { Key = key, Value = value };

        var deliveryResult = await _producer.ProduceAsync(topic, message);

        Console.WriteLine($"Delivered '{deliveryResult.Value}' to '{deliveryResult.TopicPartitionOffset}'");
    }
}