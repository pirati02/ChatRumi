using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Services;
using ErrorOr;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public static class GetChatById
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public record Query(Guid ChatId) : IRequest<ErrorOr<ChatResponse>>;

    public sealed class Handler(
        IDocumentStore store,
        IAccountPublicKeyProvider publicKeyProvider
    ) : IRequestHandler<Query, ErrorOr<ChatResponse>>
    {
        public async ValueTask<ErrorOr<ChatResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                request.ChatId,
                token: cancellationToken
            );

            if (chat is null)
            {
                return Error.NotFound("Chat not found.");
            }

            var dto = chat.ToDto();
            var lookup = await publicKeyProvider.GetPublicKeysAsync(
                ParticipantPublicKeyEnrichment.CollectAccountIds(dto),
                cancellationToken);
            return dto.EnrichPublicKeys(lookup);
        }
    }
}