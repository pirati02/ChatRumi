namespace ChatRumi.Chat.Application.Services;

/// <summary>
/// Resolves E2E public keys from the Account service (same source as GET /api/account/{id}/public-key).
/// </summary>
public interface IAccountPublicKeyProvider
{
    /// <summary>
    /// Returns a map of account id to public key. Missing ids indicate lookup failure or 404 (use aggregate/client fallback).
    /// Values may be null when the account exists but has not registered a key (200 with null body).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string?>> GetPublicKeysAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken);
}
