using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Domain.ValueObject;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Api.Hub;

public class ChatHubContextProxy(IHubContext<ChatHub, IChatClient> hubContext) : IChatHubContextProxy
{
    public Task MessageStateUpdated(IEnumerable<string> connectionIds, Guid messageId, MessageStatus messageStatus)
    {
        return hubContext.Clients.Clients(connectionIds).MessageStateUpdated(messageId, messageStatus);
    }
}