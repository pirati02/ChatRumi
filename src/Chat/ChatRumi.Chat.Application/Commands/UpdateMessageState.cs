using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class UpdateMessageState
{
    public sealed record Command(
        Guid ChatId,
        ExistingMessageRequest Message,
        MessageStatus Status
    ) : IRequest<ErrorOr<(Guid messageId, MessageStatus status)>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, ErrorOr<(Guid messageId, MessageStatus status)>>
    {
        public async Task<ErrorOr<(Guid messageId, MessageStatus status)>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                request.ChatId,
                token: cancellationToken
            );
            if (chat is null)
            {
                return Error.NotFound("Chat not found.", $"Chat by '{request.ChatId}' not found.");
            }

            chat.Fire(new MessageStatusChangeEvent
            {
                MessageId = request.Message.MessageId,
                SenderId = request.Message.Sender.ToDomain(),
                Status = request.Status
            });
            session.Events.Append(chat.Id, chat.Events);
            await session.SaveChangesAsync(cancellationToken);
            return (request.Message.MessageId, request.Status);
        }
    }
}