using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Domain.ValueObject;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Api.Hub;

public class ChatHub(
    IServiceProvider serviceProvider,
    AccountConnectionManager accountConnectionManager) : Hub<IChatClient>
{
    public override Task OnConnectedAsync()
    {
        if (TryGetAccount(out var accountId))
        {
            accountConnectionManager.AddAccount(accountId, Context.ConnectionId);
        }

        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (TryGetAccount(out var accountId))
        {
            accountConnectionManager.RemoveConnection(accountId, Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task StartChat(
        ParticipantDto[] participants,
        ParticipantDto creator,
        string chatName,
        bool overrideExisting
    )
    {
        var participantIds = participants.Select(p => p.Id).ToArray();
        var connections = accountConnectionManager.GetConnections(participantIds);

        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var chatStartResult = await mediator.Send(
            new StartChat.Command(overrideExisting, chatName, creator, participants)
        );

        if (chatStartResult.IsError)
        {
            return;
        }

        await Clients.Clients(connections).ChatStarted(chatStartResult.Value);
    }

    public async Task SendMessage(
        Guid chatId,
        MessageRequest message
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AppendMessage.Command(chatId, message));
        if (result.IsError)
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }

        var chat = await mediator.Send(new GetChatById.Query(chatId));
        var participantIds = chat.Value.Participants.Select(p => p.Id).ToArray();
        var connections = accountConnectionManager.GetConnections(participantIds);

        await Clients.Clients(connections).MessageSent(result.Value, false);
    }

    public async Task UpdateMessageState(
        Guid chatId,
        ExistingMessageRequest message,
        MessageStatus messageStatus
    )
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateMessageState.Command(chatId, message, messageStatus));
        if (result.IsError)
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }

        if (accountConnectionManager.TryGetConnection(message.Sender.Id, out var senderConnectionIds))
        {
            var (id, status) = result.Value;
            await Clients.Clients(senderConnectionIds).MessageStateUpdated(id, status);
        }
    }

    private bool TryGetAccount(out Guid accountId)
    {
        accountId = Guid.Empty;

        return Context.GetHttpContext()?.Request.Query.TryGetValue("accountId", out var values) == true &&
               Guid.TryParse(values, out accountId);
    }
}