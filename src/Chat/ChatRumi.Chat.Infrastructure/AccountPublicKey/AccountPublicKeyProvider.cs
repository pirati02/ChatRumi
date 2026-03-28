using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using ChatRumi.Chat.Application.Services;
using Microsoft.Extensions.Logging;

namespace ChatRumi.Chat.Infrastructure.AccountPublicKey;

public sealed class AccountPublicKeyProvider(
    HttpClient httpClient,
    ILogger<AccountPublicKeyProvider> logger
) : IAccountPublicKeyProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyDictionary<Guid, string?>> GetPublicKeysAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        var distinct = accountIds.Distinct().ToList();
        if (distinct.Count == 0)
            return new Dictionary<Guid, string?>();

        if (httpClient.BaseAddress is null)
        {
            logger.LogWarning("Account service base URL is not configured; skipping public key lookup.");
            return new Dictionary<Guid, string?>();
        }

        var results = new ConcurrentDictionary<Guid, string?>();

        await Task.WhenAll(distinct.Select(id => FetchOneAsync(id, results, cancellationToken)));

        return results;
    }

    private async Task FetchOneAsync(
        Guid id,
        ConcurrentDictionary<Guid, string?> results,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response =
                await httpClient.GetAsync($"api/account/{id}/public-key", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return;

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GET public-key for {AccountId} returned {StatusCode}", id,
                    (int)response.StatusCode);
                return;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<PublicKeyPayload>(stream, JsonOptions,
                cancellationToken);

            results[id] = payload?.PublicKey;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch public key for account {AccountId}", id);
        }
    }

    private sealed class PublicKeyPayload
    {
        public string? PublicKey { get; init; }
    }
}
