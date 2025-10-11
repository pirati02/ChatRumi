using System.Text.Json;
using ChatRum.InterCommunication;
using ChatRumi.Friendship.Application.IntegrationEvents;
using ChatRumi.Friendship.Application.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace ChatRumi.Friendship.AccountSync;

public class AccountCreatedConsumerBackgroundService(
    ILogger<AccountCreatedConsumerBackgroundService> logger,
    IOptions<KafkaOptions> options,
    IServiceProvider serviceProvider
) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
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
            consumer.Subscribe(Topics.AccountCreatedTopic);

            logger.LogInformation("Kafka Consumer started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    if (!string.IsNullOrWhiteSpace(cr.Message.Value))
                    {
                        var @event = JsonSerializer.Deserialize<AccountCreated>(cr.Message.Value, SerializerOptions);
                        if (@event is null)
                        {
                            return;
                        }

                        await peerConnectionManager.CreatePeerAsync(@event.AccountId, @event.UserName, DateTime.UtcNow);
                    }

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
        }, stoppingToken);
    }
}