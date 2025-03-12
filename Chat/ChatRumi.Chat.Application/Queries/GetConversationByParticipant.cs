using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Projections;
using ChatRumi.Chat.Domain.Aggregates;
using MediatR;
using ErrorOr;
using Marten;

namespace ChatRumi.Chat.Application.Queries;

public class GetConversationByParticipant
{
    public record Query(
        Guid ParticipantId1,
        Guid ParticipantId2
    ) : IRequest<ErrorOr<GetConversationResponse>>;

    public record GetConversationResponse(
        Guid ConversationId,
        MessageResponse[] Messages
    );

    public class Handler(IDocumentStore store) : IRequestHandler<Query, ErrorOr<GetConversationResponse>>
    {
        public async Task<ErrorOr<GetConversationResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var existing =
                await session.TryGetExistingConversation(request.ParticipantId1, request.ParticipantId2,
                    cancellationToken);
            if (existing is null)
            {
                return Error.NotFound("Conversation not found.", "Conversation not found.");
            }

            var conversation =
                await session.Events.AggregateStreamAsync<Conversation>(existing.Id,
                    token: cancellationToken);

            return new GetConversationResponse(
                conversation!.Id,
                conversation.Messages
                    .Select(m =>
                        new MessageResponse(
                            conversation.Id, m.Id,
                            m.LatestStatus(),
                            m.Content.Content,
                            m.ParticipantId,
                            m.ReplyOf?.Id
                        )
                    )
                    .ToArray()
            );
        }
    }
}