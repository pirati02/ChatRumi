using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

public class MarkConversationRead
{
    public record Command(Guid ConversationId, Guid[] MessageIds) : IRequest<ErrorOr<bool>>;

    public class Handler(IDocumentStore store) : IRequestHandler<Command, ErrorOr<bool>>
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
            return true;
        }
    }
}