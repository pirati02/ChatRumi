using ChatRum.InterCommunication;
using ChatRumi.Account.Application.Documents;
using Marten;
using Microsoft.Extensions.Options;

namespace ChatRumi.Account.Api;

public sealed class AccountOutboxRelayBackgroundService(
    IDocumentStore store,
    IDispatcher dispatcher,
    IOptions<OutboxRelayOptions> options,
    ILogger<AccountOutboxRelayBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var relayOptions = options.Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var session = store.LightweightSession();
                var now = DateTimeOffset.UtcNow;
                var pending = await session.Query<AccountOutboxMessage>()
                    .Where(x => x.ProcessedOnUtc == null && x.NextAttemptAtUtc <= now)
                    .OrderBy(x => x.CreatedOnUtc)
                    .Take(relayOptions.BatchSize)
                    .ToListAsync(stoppingToken);

                foreach (var message in pending)
                {
                    if (message.AttemptCount >= relayOptions.MaxAttempts)
                    {
                        message.ProcessedOnUtc = DateTimeOffset.UtcNow;
                        logger.LogError(
                            "Account outbox message {MessageId} reached max attempts ({MaxAttempts}) and will not be retried.",
                            message.Id,
                            relayOptions.MaxAttempts);
                        continue;
                    }

                    try
                    {
                        await dispatcher.ProduceAsync(
                            message.Topic,
                            message.Key,
                            message.Payload,
                            stoppingToken);

                        message.ProcessedOnUtc = DateTimeOffset.UtcNow;
                        message.LastError = null;
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        message.AttemptCount++;
                        message.LastError = ex.Message;
                        var delay = CalculateDelayMs(message.AttemptCount, relayOptions);
                        message.NextAttemptAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(delay);
                        logger.LogWarning(
                            ex,
                            "Account outbox dispatch failed for message {MessageId} (attempt {Attempt})",
                            message.Id,
                            message.AttemptCount);
                    }
                }

                if (pending.Count > 0)
                {
                    await session.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account outbox polling failed.");
            }

            await Task.Delay(relayOptions.PollIntervalMs, stoppingToken);
        }
    }

    private static int CalculateDelayMs(int attemptCount, OutboxRelayOptions options)
    {
        var exp = (int)Math.Pow(2, Math.Clamp(attemptCount - 1, 0, 15));
        var candidate = options.InitialRetryDelayMs * exp;
        return Math.Min(candidate, options.MaxRetryDelayMs);
    }
}
