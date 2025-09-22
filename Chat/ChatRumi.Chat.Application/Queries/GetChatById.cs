using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Response;
using ErrorOr;
using Marten;
using MediatR;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class GetChatById
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public record Query(Guid ChatId) : IRequest<ErrorOr<ChatDetailsResponse>>;

    public class Handler(IDocumentStore store) : IRequestHandler<Query, ErrorOr<ChatDetailsResponse>>
    {
        public async Task<ErrorOr<ChatDetailsResponse>> Handle(Query request, CancellationToken cancellationToken)
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

            return new ChatDetailsResponse(
                chat.Id,
                chat.Participants.Select(p => p.ToDto()).ToList()
            );
        }
    }
}