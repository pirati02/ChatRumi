using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using MediatR;
using ErrorOr;
using Marten;

namespace ChatRumi.Chat.Application.Queries;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SearchExistingChatByParticipant
{
    public sealed record Query(
        ParticipantDto[] Participants
    ) : IRequest<ErrorOr<GetChatResponse>>;

    public sealed record GetChatResponse(
        Guid ChatId,
        MessageResponse[] Messages
    );

    public sealed class Handler(IDocumentStore store) : IRequestHandler<Query, ErrorOr<GetChatResponse>>
    {
        public async Task<ErrorOr<GetChatResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var existing = await session.TryGetExistingChat(
                request.Participants,
                cancellationToken
            );
            if (existing is null)
            {
                return Error.NotFound("Chat not found.", "Chat not found.");
            }

            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                existing.Id,
                token: cancellationToken
            );

            return new GetChatResponse(
                chat!.Id,
                chat.Messages
                    .Select(MessageResponse.From)
                    .ToArray()
            );
        }
    }
}