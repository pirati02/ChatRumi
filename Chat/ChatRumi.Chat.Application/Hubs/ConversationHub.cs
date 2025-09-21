using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.ValueObject;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationHub(
    IServiceProvider serviceProvider,
    AccountConnectionManager accountConnectionManager) : Hub<IConversationClient>
{
    public override Task OnConnectedAsync()
    {
        if (!TryGetAccount(out var accountId))
        {
            accountConnectionManager.AddAccount(accountId, Context.ConnectionId);
        }
        
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (!TryGetAccount(out var accountId))
        {
            accountConnectionManager.RemoveConnection(accountId, Context.ConnectionId);
        }
        
        return base.OnDisconnectedAsync(exception);
    }

    public async Task StartConversation(
        Guid sender,
        Guid receiver,
        MessageRequest? initialMessage
    )
    {
        var connections = accountConnectionManager.GetConnections([sender, receiver]);
        
        await using var scope = serviceProvider.CreateAsyncScope();
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
 
        await Clients.Clients(connections).ConversationStarted(conversationStartResult.Value);

        if (initialMessage is not null)
        {
            await SendMessage(conversationStartResult.Value, initialMessage);
        }
    }

    public async Task SendMessage(
        Guid conversationId,
        MessageRequest message
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AppendMesage.Command(conversationId, message));
        if (result.IsError)
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }
        
        if (accountConnectionManager.TryGetConnection(message.SenderId, out var senderConnectionIds))
        {
            await Clients.Clients(senderConnectionIds).MessageSent(result.Value, false);
        }
        
        if (accountConnectionManager.TryGetConnection(message.ReceiverId, out var receiverConnectionIds))
        {
            await Clients.Clients(receiverConnectionIds).MessageSent(result.Value, true);
        }
    }

    public async Task UpdateMessageState(
        Guid conversationId,
        ExistingMessageRequest message,
        MessageStatus messageStatus
    )
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

        if (accountConnectionManager.TryGetConnection(message.SenderId, out var senderConnectionIds))
        { 
            var (id, status) = result.Value;
            await Clients.Clients(senderConnectionIds).MessageStateUpdated(id, status);
        }
    }

    private bool TryGetConversation(out Guid conversationId)
    {
        conversationId = Guid.Empty;
        return !Context.GetHttpContext().Request.Query.TryGetValue("conversationId", out var conversationIdQueryValue)
               || !Guid.TryParse(conversationIdQueryValue, out conversationId);
    }

    private bool TryGetAccount(out Guid accountId)
    {
        accountId = Guid.Empty;
        return !Context.GetHttpContext().Request.Query.TryGetValue("accountId", out var accountIdQueryValue)
               || !Guid.TryParse(accountIdQueryValue, out accountId);
    }
}