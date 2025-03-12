using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.ValueObject;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationHub(
    IServiceProvider serviceProvider,
    ConversationConnectionManager conversationConnectionManager) : Hub<IConversationClient>
{
    public override Task OnConnectedAsync()
    {
        if (!Context.GetHttpContext().Request.Query.TryGetValue("conversationId", out var queryValue))
            return Task.CompletedTask;
        if (!Guid.TryParse(queryValue, out var conversationId)) return Task.CompletedTask;

        conversationConnectionManager.SetConversation(conversationId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task StartConversation(
        Guid? conversationId,
        Guid participantId1,
        Guid participantId2,
        MessageRequest initialMessage
    )
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var conversationStartResult = await mediator.Send(
            new StartConversation.Command(
                participantId1,
                participantId2,
                initialMessage
            )
        );

        if (conversationStartResult.IsError)
        {
            return;
        }

        conversationConnectionManager.SetConversation(conversationStartResult.Value, Context.ConnectionId);
        await Clients.Clients(Context.ConnectionId).ConversationStarted(conversationStartResult.Value);
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

        var clientIds = conversationConnectionManager.GetConversationConnections(conversationId);
        await Clients.Clients(clientIds).MessageSent(conversationId, result.Value); 
    }

    public async Task UpdateMessageState(Guid conversationId, ExistingMessageRequest message,
        MessageStatus messageStatus)
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

        var clientIds = conversationConnectionManager.GetConversationConnections(conversationId);
        var (id, status) = result.Value;
        await Clients.Clients(clientIds).MessageStateUpdated(conversationId, id, status);
    }
}