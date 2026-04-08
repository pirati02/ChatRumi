namespace ChatRumi.Notification.Api.Hub;

public sealed class NotificationConnectionManager
{
    private readonly Dictionary<Guid, HashSet<string>> _connections = [];
    private readonly object _lock = new();

    public void AddAccount(Guid accountId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(accountId, out var connectionIds))
            {
                connectionIds = [];
                _connections[accountId] = connectionIds;
            }

            connectionIds.Add(connectionId);
        }
    }

    public void RemoveConnection(Guid accountId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(accountId, out var connectionIds))
            {
                return;
            }

            connectionIds.Remove(connectionId);
            if (connectionIds.Count == 0)
            {
                _connections.Remove(accountId);
            }
        }
    }

    public IReadOnlyCollection<string> GetConnections(Guid accountId)
    {
        lock (_lock)
        {
            return _connections.TryGetValue(accountId, out var connectionIds)
                ? connectionIds.ToArray()
                : [];
        }
    }
}
