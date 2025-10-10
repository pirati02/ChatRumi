using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Application.Hubs;

public interface IChatHubContextProxy
{
    IHubClients<IChatClient> Clients { get; }
}