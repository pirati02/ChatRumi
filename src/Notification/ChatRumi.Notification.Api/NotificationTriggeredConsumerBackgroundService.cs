using System.Text.Json;
using ChatRum.InterCommunication;
using ChatRumi.Notification.Application;
using Confluent.Kafka;

namespace ChatRumi.Notification.Api;

public class NotificationTriggeredConsumerBackgroundService(
    ILogger<NotificationTriggeredConsumerBackgroundService> logger,
    Microsoft.Extensions.Options.IOptions<KafkaOptions> options,
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
            if (string.IsNullOrWhiteSpace(options.Value.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"Missing required Kafka setting: {KafkaOptions.Name}.ConnectionString");
            }

            await KafkaTopicBootstrap.EnsureInterCommunicationTopicsAsync(options.Value.ConnectionString, stoppingToken)
                .ConfigureAwait(false);

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = options.Value.ConnectionString,
                GroupId = "notification-service-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(Topics.NotificationTriggeredTopic);
            logger.LogInformation("Notification consumer started");

            var errorBackoffMs = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var record = consumer.Consume(stoppingToken);
                    errorBackoffMs = 0;

                    if (string.IsNullOrWhiteSpace(record.Message.Value))
                    {
                        continue;
                    }

                    var @event = JsonSerializer.Deserialize<NotificationTriggered>(record.Message.Value, SerializerOptions);
                    if (@event is null)
                    {
                        continue;
                    }

                    await using var scope = serviceProvider.CreateAsyncScope();
                    var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notifications.CreateFromEventAsync(@event, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Notification kafka consume error");
                    errorBackoffMs = errorBackoffMs == 0 ? 200 : Math.Min(errorBackoffMs * 2, 10_000);
                    await Task.Delay(errorBackoffMs, stoppingToken).ConfigureAwait(false);
                }
            }

            consumer.Close();
        }, stoppingToken);
    }
}
