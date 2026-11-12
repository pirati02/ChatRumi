using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ChatRumi.Kernel.Results;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public static class AppendMessage
{
    public sealed record Command(Guid ChatId, MessageRequest Request) : IRequest<Response<MessageResponse>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, Response<MessageResponse>>
    {
        public async ValueTask<Response<MessageResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                request.ChatId,
                token: cancellationToken
            );

            if (chat is null)
            {
                return ResponseError.NotFound("Chat not found.", $"Chat by '{request.ChatId}' not found.");
            }

            var @event = new MessageSentEvent(
                chat.Id,
                request.Request.Sender.ToDomain(),
                request.Request.Content,
                request.Request.ReplyOf
            );
            chat.Fire(@event);

            session.Events.Append(chat.Id, chat.Events);
            await session.SaveChangesAsync(cancellationToken);

            return new MessageResponse(
                chat.Id,
                @event.Id,
                MessageStatus.Sent,
                request.Request.Content,
                request.Request.Sender,
                request.Request.ReplyOf,
                []
            );
        }
    }
}
