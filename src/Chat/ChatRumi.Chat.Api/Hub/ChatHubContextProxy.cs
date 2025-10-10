using ChatRumi.Chat.Application.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Api.Hub;

public class ChatHubContextProxy(IHubContext<ChatHub, IChatClient> hubContext) : IChatHubContextProxy
{
    public IHubClients<IChatClient> Clients => hubContext.Clients;
}