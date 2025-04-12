using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Projections;
using MediatR;
using ErrorOr;
using Marten;

namespace ChatRumi.Chat.Application.Queries;

public class GetTop10LatestConversation
{
    public record Query(Guid ParticipantId, Guid[] ResponderParticipantIds)
        : IRequest<ErrorOr<LatestConversationResponse[]>>;

    public class Handler(IDocumentStore store) : IRequestHandler<Query, ErrorOr<LatestConversationResponse[]>>
    {
        public async Task<ErrorOr<LatestConversationResponse[]>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var conversations =
                await session.TryGetTop10LatestConversationsAsync(request.ParticipantId,
                    request.ResponderParticipantIds,
                    cancellationToken);


            return conversations.Select(conversation => new LatestConversationResponse(conversation.Id,
                conversation.LatestMessage is not null
                    ? LatestMessageResponse.From(conversation.Id, conversation.LatestMessage)
                    : null
            )).ToArray();
        }
    }
}