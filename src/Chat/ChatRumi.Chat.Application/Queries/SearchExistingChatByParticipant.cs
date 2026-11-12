using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Kernel.Results;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public static class SearchExistingChatByParticipant
{
    public sealed record Query(
        ParticipantDto[] Participants,
        Guid RequestingUserId
    ) : IRequest<Response<ChatResponse>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Query, Response<ChatResponse>>
    {
        public async ValueTask<Response<ChatResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!request.Participants.Any(p => p.Id == request.RequestingUserId))
            {
                return ResponseError.Forbidden("Chat.AccessDenied", "You must be a participant in the chat search.");
            }

            await using var session = store.LightweightSession();

            var existing = await session.TryGetExistingChat(
                request.Participants,
                cancellationToken
            );
            if (existing is null)
            {
                return ResponseError.NotFound("Chat not found.", "Chat not found.");
            }

            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                existing.Id,
                token: cancellationToken
            );

            if (chat is null)
                return ResponseError.NotFound("Chat not found.", "Chat not found.");

            return chat.ToDto();
        }
    }
}
