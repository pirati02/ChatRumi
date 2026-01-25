using System.Collections.Concurrent;

namespace ChatRumi.Friendship.Application.Services;

public sealed class FriendshipConnectionManager
{
    //Account ID -> List of Connection IDs to support multiple connections per account
    private readonly ConcurrentDictionary<Guid, List<string>> _connections = new();

    public void AddAccount(Guid accountId, string connectionId)
    {
        if (_connections.TryGetValue(accountId, out var clientIds))
        {
            _connections[accountId] = [.. clientIds, connectionId];
            return;
        }

        _connections.TryAdd(accountId, [connectionId]);
    }

    public void RemoveConnection(Guid accountId, string connectionId)
    {
        if (!_connections.TryGetValue(accountId, out var clientIds))
            return;

        if (clientIds.Contains(connectionId) && !clientIds.Remove(connectionId))
            return;

        if (clientIds.Count == 0)
        {
            _connections.TryRemove(accountId, out _);
        }
    }

    public bool TryGetConnection(Guid accountId, out List<string> connectionIds)
    {
        return _connections.TryGetValue(accountId, out connectionIds!);
    }


    public List<string> GetConnections(Guid[] accountIds)
    {
        return accountIds.Select(accountId =>
            {
                var result = TryGetConnection(accountId, out var connectionIdsPerAccount);
                return result ? connectionIdsPerAccount : [];
            })
            .SelectMany(connections => connections)
            .ToList();
    }
}