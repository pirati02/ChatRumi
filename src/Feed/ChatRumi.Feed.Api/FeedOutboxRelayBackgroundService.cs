using ChatRum.InterCommunication;
using ChatRumi.Feed.Application;
using ChatRumi.Feed.Application.Outbox;
using Microsoft.Extensions.Options;
using Nest;

namespace ChatRumi.Feed.Api;

public sealed class FeedOutboxRelayBackgroundService(
    IElasticClient client,
    IDispatcher dispatcher,
    IOptions<OutboxRelayOptions> options,
    ILogger<FeedOutboxRelayBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var relayOptions = options.Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var response = await client.SearchAsync<FeedOutboxMessage>(s => s
                    .Index(PostIndexes.Outbox)
                    .Size(relayOptions.BatchSize)
                    .Query(q => q.Bool(b => b.Must(
                        m => m.DateRange(r => r.Field(f => f.NextAttemptAtUtc).LessThanOrEquals(DateMath.Anchored(now.UtcDateTime))),
                        m => m.Bool(bb => bb.MustNot(mn => mn.Exists(e => e.Field(f => f.ProcessedOnUtc))))
                    )))
                    .Sort(so => so.Ascending(x => x.CreatedOnUtc)),
                    stoppingToken);

                if (!response.IsValid || response.Documents.Count == 0)
                {
                    await Task.Delay(relayOptions.PollIntervalMs, stoppingToken);
                    continue;
                }

                foreach (var message in response.Documents)
                {
                    if (message.AttemptCount >= relayOptions.MaxAttempts)
                    {
                        await MarkAsProcessedAsync(message.Id, "Reached max attempts.", stoppingToken);
                        continue;
                    }

                    try
                    {
                        await dispatcher.ProduceAsync(
                            message.Topic,
                            message.Key,
                            message.Payload,
                            stoppingToken);

                        await MarkAsProcessedAsync(message.Id, null, stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        var attempt = message.AttemptCount + 1;
                        var delay = CalculateDelayMs(attempt, relayOptions);
                        await client.UpdateAsync<FeedOutboxMessage, object>(message.Id, u => u
                            .Index(PostIndexes.Outbox)
                            .Doc(new
                            {
                                AttemptCount = attempt,
                                NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(delay),
                                LastError = ex.Message
                            })
                            .Refresh(Elasticsearch.Net.Refresh.False),
                            stoppingToken);
                        logger.LogWarning(ex, "Feed outbox dispatch failed for message {MessageId}", message.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Feed outbox polling failed.");
            }

            await Task.Delay(relayOptions.PollIntervalMs, stoppingToken);
        }
    }

    private async Task MarkAsProcessedAsync(Guid id, string? reason, CancellationToken cancellationToken)
    {
        await client.UpdateAsync<FeedOutboxMessage, object>(id, u => u
            .Index(PostIndexes.Outbox)
            .Doc(new
            {
                ProcessedOnUtc = DateTimeOffset.UtcNow,
                LastError = reason
            })
            .Refresh(Elasticsearch.Net.Refresh.False),
            cancellationToken);
    }

    private static int CalculateDelayMs(int attemptCount, OutboxRelayOptions options)
    {
        var exp = (int)Math.Pow(2, Math.Clamp(attemptCount - 1, 0, 15));
        var candidate = options.InitialRetryDelayMs * exp;
        return Math.Min(candidate, options.MaxRetryDelayMs);
    }
}
