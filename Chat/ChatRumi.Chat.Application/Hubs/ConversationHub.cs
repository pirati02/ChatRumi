using System.Collections.Concurrent;
using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Aggregates;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Application.Hubs;

public class ConversationHub(IMediator mediator) : Hub<IConversationClient>
{
    private readonly ConcurrentDictionary<Guid, List<string>> _conversations = new();

    public override Task OnConnectedAsync()
    {
        if (Guid.TryParse(Context.Items["conversationId"]?.ToString(), out var conversationId) ||
            !_conversations.TryGetValue(conversationId, out var clientIds)) return Task.CompletedTask;

        clientIds.Add(Context.ConnectionId);
        return Task.CompletedTask;
    }

    public async Task StartConversation(
        Guid[] participantIds,
        Message initialMessage
    )
    {
        var conversationStartResult = await mediator.Send(
            new StartConversation.Command(
                participantIds,
                initialMessage
            )
        );

        if (conversationStartResult.IsError)
        {
            return;
        }

        var conversationId = conversationStartResult.Value;
        if (_conversations.TryGetValue(conversationId, out var clientIds))
        {
            clientIds.Add(Context.ConnectionId);
            return;
        }

        _conversations.TryAdd(conversationId, [Context.ConnectionId]);
    }

    public async Task AddMessageToConversation(Guid conversationId, MessageRequest message)
    {
        if (_conversations.TryGetValue(conversationId, out var clientIds))
        {
            var result = await mediator.Send(new AppendMesage.Command(conversationId, message));
            if (result.IsError)
            {
                //Todo: fire failed message to sent error
                //or retry to send automatically
                return;
            }

            Clients.Clients(clientIds).MessageSent(conversationId, result.Value);
        }
    }
}