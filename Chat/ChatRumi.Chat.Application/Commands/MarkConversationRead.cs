using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ErrorOr;
using Marten;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Application.Commands;

public class MarkConversationRead
{
    public record Command(Guid ConversationId, Guid[] MessageIds) : IRequest<ErrorOr<bool>>;

    public class Handler(
        IDocumentStore store,
        IHubContext<ConversationHub, IConversationClient> context,
        AccountConnectionManager connectionManager
    ) : IRequestHandler<Command, ErrorOr<bool>>
    {
        public async Task<ErrorOr<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var conversation =
                await session.Events.AggregateStreamAsync<Conversation>(request.ConversationId,
                    token: cancellationToken);
            if (conversation is null)
            {
                return Error.NotFound("Conversation not found.");
            }

            conversation.Fire(new MarkConversationReadEvent
            {
                MessageIds = request.MessageIds
            });
            session.Events.Append(conversation.Id, conversation.Events);
            await session.SaveChangesAsync(cancellationToken);

            var senders = conversation.Messages
                .Where(a => a.LatestStatus() != MessageStatus.Seen)
                .Where(m =>
                    connectionManager.TryGetConnection(m.ParticipantId, out _)
                )
                .Select(m =>
                {
                    connectionManager.TryGetConnection(m.ParticipantId, out var connectionIds);
                    return (m.Id, m.LatestStatus(), connectionid: connectionIds);
                })
                .ToArray();

            foreach (var (messageId, _, connectionIds) in senders)
            {
                await context.Clients.Clients(connectionIds).MessageStateUpdated(messageId, MessageStatus.Seen);
            }

            return true;
        }
    }
}