using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Commands;

public sealed class StartConversation
{
    public sealed record Command(
        Guid ParticipantId1,
        Guid ParticipantId2,
        MessageRequest? InitialMessage
    ) : IRequest<ErrorOr<Guid>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var existing =
                await session.TryGetExistingConversation(request.ParticipantId1, request.ParticipantId2,
                    cancellationToken);
            if (existing is not null)
            {
                var existingConversation =
                    await session.Events.AggregateStreamAsync<Conversation>(existing.Id, token: cancellationToken);

                if (request.InitialMessage is not null)
                {
                    existingConversation!.Apply(new MessageSentEvent(
                            existingConversation.Id,
                            request.InitialMessage.SenderId,
                            request.InitialMessage.Content,
                            request.InitialMessage.ReplyOf
                        )
                    );
                }
                
                session.Events.Append(existingConversation!.Id, existingConversation.Events);
                await session.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }

            var conversation = new Conversation();
            conversation.Fire(new ConversationStartedEvent
            {
                Id = conversation.Id,
                ParticipantId1 = request.ParticipantId1,
                ParticipantId2 = request.ParticipantId2
            });
            session.Events.StartStream<Conversation>(conversation.Id, conversation.Events);
            await session.SaveChangesAsync(cancellationToken);
            return conversation.Id;
        }
    }
}