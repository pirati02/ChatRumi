using ChatRumi.Chat.Application.Commands;
using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Queries;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Infrastructure;
using ChatRumi.Kernel.Results;
using Mediator;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Api.Hub;

public class ChatHub(
    IServiceProvider serviceProvider,
    AccountConnectionManager accountConnectionManager,
    ILogger<ChatHub> logger) : Hub<IChatClient>
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
        if (!TryGetAccount(out var callerId) || creator.Id != callerId)
        {
            return;
        }

        var participantIds = participants.Select(p => p.Id).ToArray();
        var connections = accountConnectionManager.GetConnections(participantIds);

        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var chatStartResult = await mediator.Send(
            new StartChat.Command(overrideExisting, chatName, creator, participants)
        );

        if (chatStartResult is not Guid chatId)
        {
            return;
        }

        await Clients.Clients(connections).ChatStarted(chatId);
    }

    public async Task SendMessage(
        Guid chatId,
        MessageRequest message
    )
    {
        if (!TryGetAccount(out var callerId) || message.Sender.Id != callerId)
        {
            return;
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new AppendMessage.Command(chatId, message));

            var senderConnection = accountConnectionManager.GetConnections([message.Sender.Id]);
            if (result is IEnumerable<ResponseError> errors)
            {
                logger.LogWarning(
                    "SendMessage failed for chatId {ChatId}: {Errors}",
                    chatId,
                    string.Join("; ", errors.Select(e => $"{e.Code}: {e.Description}")));

                // The message was never persisted, so echo the request back with no id for the sender to mark as failed.
                var failedMessage = new MessageResponse(
                    chatId, Guid.Empty, MessageStatus.Sent, message.Content, message.Sender, message.ReplyOf, []);
                await Clients.Clients(senderConnection).MessageFailed(failedMessage);
                return;
            }

            if (result is not MessageResponse sentMessage)
            {
                return;
            }

            var chat = await mediator.Send(new GetChatById.Query(chatId, callerId));
            if (chat is not ChatResponse chatResponse)
            {
                return;
            }

            var receivers = chatResponse.Receivers(message);
            var connections = accountConnectionManager.GetConnections(receivers);
            await Clients.Clients(connections).MessageSent(sentMessage, true);
            await Clients.Clients(senderConnection).MessageSent(sentMessage, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SendMessage for chatId {ChatId}", chatId);
            throw;
        }
    }

    public async Task UpdateMessageState(
        Guid chatId,
        ExistingMessageRequest message,
        MessageStatus messageStatus
    )
    {
        if (!TryGetAccount(out var callerId) || message.Sender.Id != callerId)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateMessageState.Command(chatId, message, messageStatus));
        if (result is not ValueTuple<Guid, MessageStatus>(var id, var status))
        {
            //Todo: fire failed message to sent error
            //or retry to send automatically
            return;
        }

        if (accountConnectionManager.TryGetConnection(message.Sender.Id, out var senderConnectionIds))
        {
            await Clients.Clients(senderConnectionIds).MessageStateUpdated(id, status);
        }
    }

    public async Task UpdateMessageReaction(
        Guid chatId,
        MessageReactionRequest reaction
    )
    {
        if (!TryGetAccount(out var callerId) || reaction.Actor.Id != callerId)
        {
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdateMessageReaction.Command(chatId, reaction));
        if (result is not ValueTuple<Guid, MessageReactionResponse[]>(var messageId, var reactions))
        {
            return;
        }

        var chat = await mediator.Send(new GetChatById.Query(chatId, callerId));
        if (chat is not ChatResponse chatResponse)
        {
            return;
        }

        var connections = accountConnectionManager.GetConnections(chatResponse.Participants.Select(p => p.Id).ToArray());
        await Clients.Clients(connections).MessageReactionUpdated(messageId, reactions);
    }

    private bool TryGetAccount(out Guid accountId)
    {
        return Context.User.TryGetAccountId(out accountId);
    }
}