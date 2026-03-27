using ChatRumi.Chat.Application.Projections.ExistingChat;
using ChatRumi.Chat.Domain.Events;
using Marten;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ChatRumi.Chat.Application.Commands;

public static class ModifyChatParticipant
{
    public sealed record Command(
        Guid ParticipantId,
        string UserName,
        string FirstName,
        string LastName,
        string? PublicKey
    ) : IRequest;

    public sealed class Handler(
        IDocumentStore store,
        ILogger<Handler> logger
    ) : IRequestHandler<Command>
    {
        public async ValueTask<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var session = store.LightweightSession();

            var participantHash = string.Join("|", [request.ParticipantId]);

            var chatIds = await session.Query<ExistingChatProjection>()
                .Where(p => p.ParticipantsHash.Contains(participantHash))
                .Select(a => a.Id)
                .ToListAsync(token: cancellationToken);

            foreach (var chatId in chatIds.Chunk(100).SelectMany(a => a))
            {
                session.Events.Append(
                     chatId,
                     new ParticipantModifiedEvent(
                         request.ParticipantId,
                         request.FirstName,
                         request.LastName,
                         request.UserName,
                         request.PublicKey
                     )
                 );
                logger.LogInformation("Successfully updated {Chat} for participant {Id}", chatId, request.ParticipantId);
            }

            await session.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}