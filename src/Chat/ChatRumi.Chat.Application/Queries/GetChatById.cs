using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Response;
using ErrorOr;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public static class GetChatById
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public record Query(Guid ChatId, Guid RequestingUserId) : IRequest<ErrorOr<ChatResponse>>;

    public sealed class Handler(
        IDocumentStore store
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

            if (!chat.Participants.Any(p => p.Id == request.RequestingUserId))
            {
                return Error.Forbidden("Chat.AccessDenied", "You do not have access to this chat.");
            }

            return chat.ToDto();
        }
    }
}
