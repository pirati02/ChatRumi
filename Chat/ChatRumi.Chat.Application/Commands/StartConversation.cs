using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Domain.Aggregates;
using ErrorOr;
using Marten;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ChatRumi.Chat.Application.Commands;

public class StartConversation
{
    public record Command(
        Guid[] ParticipantIds,
        Message InitialMessage
    ) : IRequest<ErrorOr<Guid>>
    {
        public bool IsBetweenTwoPerson()
        {
            return ParticipantIds.Length == 2;
        }
    }

    public class Handler(
        IDocumentStore store,
        IHubContext<ConversationHub, IConversationClient> conversationHub
    ) : IRequestHandler<Command, ErrorOr<Guid>>
    {
        public async Task<ErrorOr<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            if (request.IsBetweenTwoPerson())
            {
                var existing = await session.Query<ExistingConversationProjection>()
                    .FirstOrDefaultAsync(a => a.FindParticipants(request.ParticipantIds), token: cancellationToken);

                if (existing is not null)
                {
                    return existing.ConversationId;
                }
            }

            var conversation = Conversation.Begin(
                request.InitialMessage,
                withParticipants: request.ParticipantIds
            );
            session.Events.StartStream<Conversation>(conversation.Id, conversation.Events);
            return conversation.Id;
        }
    }
}