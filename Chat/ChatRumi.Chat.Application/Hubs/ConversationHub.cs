using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.ValueObject;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationHub(
    IServiceProvider serviceProvider,
    ConversationConnectionManager connectionManager) : Hub<IConversationClient>
{
    public override Task OnConnectedAsync()
    {
        if (!Context.GetHttpContext().Request.Query.TryGetValue("conversationId", out var conversationIdQueryValue))
            return Task.CompletedTask;
        if (!Guid.TryParse(conversationIdQueryValue, out var conversationId)) return Task.CompletedTask;
        
        if (!Context.GetHttpContext().Request.Query.TryGetValue("accountId", out var accountIdQueryValue))
            return Task.CompletedTask;
        if (!Guid.TryParse(accountIdQueryValue, out var accountId)) return Task.CompletedTask;

        connectionManager.SetConversation(conversationId, accountId, Context.ConnectionId);
        return Task.CompletedTask;
    }
    
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (!Context.GetHttpContext().Request.Query.TryGetValue("conversationId", out var queryValue))
            return Task.CompletedTask;
        if (!Guid.TryParse(queryValue, out var conversationId)) return Task.CompletedTask;
        
        if (!Context.GetHttpContext().Request.Query.TryGetValue("accountId", out var accountIdQueryValue))
            return Task.CompletedTask;
        if (!Guid.TryParse(accountIdQueryValue, out var accountId)) return Task.CompletedTask;
        
        connectionManager.RemoveConnection(conversationId, accountId, Context.ConnectionId); 
        return base.OnDisconnectedAsync(exception);
    }

    public async Task StartConversation(
        Guid? conversationId,
        Guid sender,
        Guid receiver,
        MessageRequest initialMessage
    )
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var conversationStartResult = await mediator.Send(
            new StartConversation.Command(
                sender,
                receiver,
                initialMessage
            )
        );

        if (conversationStartResult.IsError)
        {
            return;
        }

        connectionManager.SetConversation(conversationStartResult.Value, sender, Context.ConnectionId);
        await Clients.Clients(Context.ConnectionId).ConversationStarted(conversationStartResult.Value);
        
        await SendMessage(conversationStartResult.Value, initialMessage);
    }

    public async Task SendMessage(Guid conversationId, MessageRequest message)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AppendMesage.Command(conversationId, message));
        if (result.IsError)
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }

        var receiver = connectionManager.GetConversationConnections(conversationId)
            .FirstOrDefault(a => a.accountId != message.SenderId);
        
        var sender = connectionManager.GetConversationConnections(conversationId)
            .FirstOrDefault(a => a.accountId == message.SenderId);
        await Clients.Clients(sender.connectionId).MessageSent(conversationId, result.Value, false); 
        await Clients.Clients(receiver.connectionId).MessageSent(conversationId, result.Value, true); 
    }

    public async Task UpdateMessageState(Guid conversationId, ExistingMessageRequest message, MessageStatus messageStatus)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateMessageState.Command(conversationId, message, messageStatus));
        if (result.IsError)
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }

        var sender = connectionManager.GetConversationConnections(conversationId)
            .FirstOrDefault(a => a.accountId == message.SenderId);
        var (id, status) = result.Value;
        await Clients.Clients(sender.connectionId).MessageStateUpdated(conversationId, id, status);
    }
}