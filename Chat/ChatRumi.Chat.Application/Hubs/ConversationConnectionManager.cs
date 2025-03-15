using System.Collections.Concurrent;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationConnectionManager
{
    private readonly ConcurrentDictionary<Guid, List<(Guid accountId, string connectionId)>> _connections = new();

    public void SetConversation(Guid conversationId, Guid accountId, string connectionId)
    {
        if (conversationId == Guid.Empty)
            return;
        
        if (_connections.TryGetValue(conversationId, out var clientIds))
        {
            _connections[conversationId] = [.. clientIds, (accountId, connectionId)];
            return;
        }

        _connections.TryAdd(conversationId, [(accountId, connectionId)]);
    }

    public IReadOnlyList<(Guid accountId, string connectionId)> GetConversationConnections(Guid conversationId)
    {
        return _connections.TryGetValue(conversationId, out var connections) ? connections : [];
    }

    public void RemoveConnection(Guid conversationId, Guid accountId, string connectionId)
    {
        if (!_connections.TryGetValue(conversationId, out var clientIds)) return;

        clientIds.Remove((accountId, connectionId));
        _connections[conversationId] = clientIds;
    }
}