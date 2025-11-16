using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public static class MarkChatRead
{
    public sealed record Command(Guid ChatId, Guid[] MessageIds) : IRequest<ErrorOr<bool>>;

    public sealed class Handler(
        IDocumentStore store,
        IChatHubContextProxy hubContext,
        AccountConnectionManager connectionManager
    ) : IRequestHandler<Command, ErrorOr<bool>>
    {
        public async Task<ErrorOr<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                request.ChatId,
                token: cancellationToken
            );
            if (chat is null)
            {
                return Error.NotFound("Chat not found.");
            }

            chat.Fire(new MarkChatReadEvent
            {
                MessageIds = request.MessageIds
            });
            session.Events.Append(chat.Id, chat.Events);
            await session.SaveChangesAsync(cancellationToken);

            var senders = chat.Messages
                .Where(a => a.LatestStatus() != MessageStatus.Seen)
                .Where(m =>
                    connectionManager.TryGetConnection(m.Participant.Id, out _)
                )
                .Select(m =>
                {
                    connectionManager.TryGetConnection(m.Participant.Id, out var connectionIds);
                    return (m.Id, m.LatestStatus(), connectionid: connectionIds);
                })
                .ToArray();

            foreach (var (messageId, _, connectionIds) in senders)
            {
                await hubContext.MessageStateUpdated(connectionIds, messageId, MessageStatus.Seen);
            }

            return true;
        }
    }
}