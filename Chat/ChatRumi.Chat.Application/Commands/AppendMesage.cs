using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

public class AppendMesage
{
    public record Command(Guid ConversationId, MessageRequest Request) : IRequest<ErrorOr<Guid>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();
            var conversation =
                await session.Events.AggregateStreamAsync<Conversation>(request.ConversationId,
                    token: cancellationToken);
            if (conversation is null)
            {
                return Error.NotFound("Conversation not found.",
                    $"Conversation by '{request.ConversationId}' not found.");
            }

            var @event = new MessageSentEvent(
                conversation.Id,
                request.Request.SenderId,
                request.Request.Content,
                request.Request.ReplyOf
            );
            conversation.Fire(@event);

            session.Events.Append(conversation.Id, conversation.Events);
            await session.SaveChangesAsync(cancellationToken);

            return @event.Id;
        }
    }
}