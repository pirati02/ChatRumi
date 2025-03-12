using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ChatRumi.Chat.Domain.ValueObject;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

public class UpdateMessageState
{
    public record Command(Guid ConversationId, ExistingMessageRequest Message, MessageStatus Status) : IRequest<ErrorOr<(Guid idd, MessageStatus status)>>;

    public class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, ErrorOr<(Guid idd, MessageStatus status)>>
    {
        public async Task<ErrorOr<(Guid idd, MessageStatus status)>> Handle(Command request, CancellationToken cancellationToken)
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
            
            conversation.Fire(new MessageStatusChangeEvent
            {
                MessageId = request.Message.Id,
                SenderId = request.Message.SenderId,
                Status = request.Status
            });
            session.Events.Append(conversation.Id, conversation.Events);
            await session.SaveChangesAsync(cancellationToken);
            return (request.Message.Id, request.Status);
        }
    }
}