using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ChatRum.InterCommunication;

public interface IConsumer
{
    void Consume(string topic);
}

public class KafkaConsumer(IOptions<KafkaOptions> options)
{
    private readonly ConsumerConfig _config = new()
    {
        BootstrapServers = options.Value.ConnectionString,
        GroupId = "my-consumer-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };

    public void Consume(string topic)
    {
        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        consumer.Subscribe(topic);

        Console.WriteLine($"Listening to topic: {topic}");

        CancellationTokenSource cts = new();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            while (!cts.IsCancellationRequested)
            {
                var cr = consumer.Consume(cts.Token);
                Console.WriteLine($"Received: Key = {cr.Message.Key}, Value = {cr.Message.Value}");
            }
        }
        catch (OperationCanceledException)
        {
            consumer.Close();
        }
    }
}