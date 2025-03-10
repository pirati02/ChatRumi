using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto.Request;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationHub(IServiceProvider serviceProvider) : Hub<IConversationClient>
{
    public override Task OnConnectedAsync()
    {
        if (!Context.GetHttpContext().Request.Query.TryGetValue("conversationId", out var queryValue))
            return Task.CompletedTask;

        if (Guid.TryParse(queryValue, out var conversationId)
            && Context.Items.TryGetValue(conversationId, out var clientIdsObject))
        {
            var clientIds = clientIdsObject as List<string>;
            clientIds!.Add(Context.ConnectionId);
            return Task.CompletedTask;
        }

        Context.Items[conversationId] = new List<string>()
        {
            Context.ConnectionId
        };
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

        conversationId = conversationStartResult.Value;
        if (Context.Items.TryGetValue(conversationId.Value, out var clientIdsObjects))
        {
            var clientIds = clientIdsObjects as List<string>;
            clientIds!.Add(Context.ConnectionId);
            Context.Items[conversationId.Value] = clientIds;
            return;
        }

        {
            Context.Items[conversationId.Value] = new List<string> { Context.ConnectionId };
            await Clients.Clients(Context.ConnectionId).ConversationStarted(conversationId.Value);
        }
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
        
        if (Context.Items.TryGetValue(conversationId, out var clientIdsObjects))
        {
            var clientIds = clientIdsObjects as List<string>;
            await Clients.Clients(clientIds).MessageSent(conversationId, result.Value);
        }
    }
}