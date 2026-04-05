using System.Text.Json;
using ChatRum.InterCommunication;
using ChatRumi.Friendship.Application.Dto.Request;
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
            await KafkaTopicBootstrap.EnsureInterCommunicationTopicsAsync(
                options.Value.ConnectionString,
                stoppingToken).ConfigureAwait(false);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = options.Value.ConnectionString,
                GroupId = "account-create-service-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(Topics.AccountCreatedTopic);

            logger.LogInformation("Kafka Consumer started...");

            var errorBackoffMs = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);
                    errorBackoffMs = 0;

                    if (!string.IsNullOrWhiteSpace(cr.Message.Value))
                    {
                        var @event = JsonSerializer.Deserialize<AccountCreated>(cr.Message.Value, SerializerOptions);
                        if (@event is null)
                        {
                            logger.LogWarning("Skipped message with null deserialized payload. Key = {Key}", cr.Message.Key);
                            continue;
                        }

                        await using var scope = serviceProvider.CreateAsyncScope();
                        var peerConnectionManager = scope.ServiceProvider.GetRequiredService<IPeerConnectionManager>();

                        await peerConnectionManager.CreatePeerAsync(new PeerDto(@event.AccountId, @event.UserName))
                            .ConfigureAwait(false);
                    }

                    logger.LogInformation("Consumed message: Key = {Key}, Value = {Value}", cr.Message.Key,
                        cr.Message.Value);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error");
                    errorBackoffMs = errorBackoffMs == 0 ? 200 : Math.Min(errorBackoffMs * 2, 10_000);
                    await Task.Delay(errorBackoffMs, stoppingToken).ConfigureAwait(false);
                }
            }

            consumer.Close();
        }, stoppingToken);
    }
}
