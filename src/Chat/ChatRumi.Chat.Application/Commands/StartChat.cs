using ChatRumi.Chat.Application.Dto;
using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Kernel.Results;
using Marten;
using Mediator; 

namespace ChatRumi.Chat.Application.Commands;

// ReSharper disable once ClassNeverInstantiated.Global
public static class StartChat
{
    public sealed record Command(
        bool OverrideExisting,
        string ChatName,
        ParticipantDto Creator,
        ParticipantDto[] Participants
    ) : IRequest<Response<Guid>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, Response<Guid>>
    {
        public async ValueTask<Response<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            if (request is { OverrideExisting: false })
            {
                var existing = await session.TryGetExistingChat(
                    request.Participants,
                    cancellationToken
                );

                if (existing is not null)
                {
                    return existing.Id;
                }
            }

            var chat = new Domain.Aggregates.Chat(
                request.ChatName,
                request.Creator.ToDomain(),
                request.Participants.Select(p => p.ToDomain()).ToList()
            );

            session.Events.StartStream<Domain.Aggregates.Chat>(chat.Id, chat.Events);
            await session.SaveChangesAsync(cancellationToken);
            return chat.Id;
        }
    }
}