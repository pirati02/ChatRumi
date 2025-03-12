using System.Collections.Concurrent;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationConnectionManager
{
    private readonly ConcurrentDictionary<Guid, string[]> _connections = new();
 
    public void SetConversation(Guid conversationId, string connectionId)
    {
        if (_connections.TryGetValue(conversationId, out var clientIds))
        {
            _connections[conversationId] = [.. clientIds, connectionId];
            return;
        }

        _connections.TryAdd(conversationId, [connectionId]);
    }

    public string[] GetConversationConnections(Guid conversationId)
    {
        return _connections.TryGetValue(conversationId, out var connections) ? connections : [];
    }
}