using ChatRum.InterCommunication;
using ChatRumi.Friendship.Application;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ChatRumi.Friendship.Api;

public class AccountCreatedConsumer(
    ILogger<AccountCreatedConsumer> logger,
    IOptions<KafkaOptions> options,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var peerConnectionManager = scope.ServiceProvider.GetRequiredService<IPeerConnectionManager>();
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = options.Value.ConnectionString,
            GroupId = "webapi-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("account-created");

        logger.LogInformation("Kafka Consumer started...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);

                var peerId = cr.Message.Value;
                // peerConnectionManager.CreatePeerAsync();
                logger.LogInformation("Consumed message: Key = {Key}, Value = {Value}", cr.Message.Key,
                    cr.Message.Value);
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error");
            }

            await Task.Delay(100, stoppingToken); // optional delay
        }

        consumer.Close();
    }
}