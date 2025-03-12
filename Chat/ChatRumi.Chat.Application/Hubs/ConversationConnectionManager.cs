using System.Collections.Concurrent;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationConnectionManager
{
    private readonly ConcurrentDictionary<Guid, List<string>> _connections = new();

    public void SetConversation(Guid conversationId, string connectionId)
    {
        if (_connections.TryGetValue(conversationId, out var clientIds))
        {
            _connections[conversationId] = [.. clientIds, connectionId];
            return;
        }

        _connections.TryAdd(conversationId, [connectionId]);
    }

    public IReadOnlyList<string> GetConversationConnections(Guid conversationId)
    {
        return _connections.TryGetValue(conversationId, out var connections) ? connections : [];
    }

    public void RemoveConnection(Guid conversationId, string connectionId)
    {
        if (!_connections.TryGetValue(conversationId, out var clientIds)) return;
        
        clientIds.Remove(connectionId);
        _connections[conversationId] = clientIds;
    }
}