using ChatRumi.Chat.Application.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.AccountSync;

public class ChatHubContextProxy : IChatHubContextProxy
{
    public IHubClients<IChatClient> Clients => null;
}