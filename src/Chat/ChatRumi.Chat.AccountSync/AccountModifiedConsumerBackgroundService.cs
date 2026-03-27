using System.Text.Json;
using ChatRum.InterCommunication;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.IntegrationEvents;
using Confluent.Kafka;
using Mediator;
using Microsoft.Extensions.Options;

namespace ChatRumi.Chat.AccountSync;

public class AccountModifiedConsumerBackgroundService(
    ILogger<AccountModifiedConsumerBackgroundService> logger,
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
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = options.Value.ConnectionString,
                GroupId = "chat-service-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(Topics.AccountUpdatedTopic);

            logger.LogInformation("Kafka Consumer started...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(stoppingToken);

                    if (!string.IsNullOrWhiteSpace(cr.Message.Value))
                    {
                        var @event = JsonSerializer.Deserialize<AccountModified>(cr.Message.Value, SerializerOptions);
                        if (@event is null)
                        {
                            return;
                        }

                        await mediator.Send(
                            new ModifyChatParticipant.Command(
                                @event.AccountId,
                                @event.UserName,
                                @event.FirstName,
                                @event.LastName,
                                @event.PublicKey
                            ), stoppingToken);
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