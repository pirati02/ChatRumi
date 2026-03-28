using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Projections.LatestChat;
using ChatRumi.Chat.Application.Services;
using ErrorOr;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public static class GetTop10LatestChat
{
    public sealed record Query(Guid ParticipantId)
        : IRequest<ErrorOr<LatestChatResponse[]>>;

    public sealed class Handler(
        IDocumentStore store,
        IAccountPublicKeyProvider publicKeyProvider
    ) : IRequestHandler<Query, ErrorOr<LatestChatResponse[]>>
    {
        public async ValueTask<ErrorOr<LatestChatResponse[]>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var chats = await session.TryGetTop10LatestChatsAsync(
                request.ParticipantId,
                cancellationToken
            );

            var responses = chats.Select(chat => new LatestChatResponse(
                chat.Id,
                chat.IsGroupChat,
                chat.LatestMessage is not null
                    ? LatestMessageResponse.From(chat.Id, chat.LatestMessage)
                    : null,
                chat.Participants.Select(p => p.ToDto()).ToList()
            )).ToArray();

            var allIds = responses.SelectMany(ParticipantPublicKeyEnrichment.CollectAccountIds).Distinct();
            var lookup = await publicKeyProvider.GetPublicKeysAsync(allIds, cancellationToken);

            return responses.Select(r => r.EnrichPublicKeys(lookup)).ToArray();
        }
    }
}