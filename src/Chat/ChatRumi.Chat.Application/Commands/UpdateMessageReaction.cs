using ChatRumi.Chat.Application.Dto.Extensions;
using ChatRumi.Chat.Application.Dto.Request;
using ChatRumi.Chat.Application.Dto.Response;
using ChatRumi.Chat.Domain.Events;
using ErrorOr;
using Marten;
using Mediator;

namespace ChatRumi.Chat.Application.Commands;

public static class UpdateMessageReaction
{
    private static readonly HashSet<string> AllowedEmojis =
    [
        "👍",
        "❤️",
        "😂",
        "😮",
        "😢",
        "🙏"
    ];

    public sealed record Command(
        Guid ChatId,
        MessageReactionRequest Reaction
    ) : IRequest<ErrorOr<(Guid messageId, MessageReactionResponse[] reactions)>>;

    public sealed class Handler(
        IDocumentStore store
    ) : IRequestHandler<Command, ErrorOr<(Guid messageId, MessageReactionResponse[] reactions)>>
    {
        public async ValueTask<ErrorOr<(Guid messageId, MessageReactionResponse[] reactions)>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reaction.Emoji) || !AllowedEmojis.Contains(request.Reaction.Emoji))
            {
                return Error.Validation("reaction.invalid_emoji", "Emoji is not allowed.");
            }

            await using var session = store.LightweightSession();
            var chat = await session.Events.AggregateStreamAsync<Domain.Aggregates.Chat>(
                request.ChatId,
                token: cancellationToken
            );
            if (chat is null)
            {
                return Error.NotFound("Chat not found.", $"Chat by '{request.ChatId}' not found.");
            }

            var actorExists = chat.Participants.Any(p => p.Id == request.Reaction.Actor.Id);
            if (!actorExists)
            {
                return Error.Forbidden("reaction.actor_not_in_chat", "Actor is not part of this chat.");
            }

            var message = chat.Messages.FirstOrDefault(m => m.Id == request.Reaction.MessageId);
            if (message is null)
            {
                return Error.NotFound("Message not found.", $"Message by '{request.Reaction.MessageId}' not found.");
            }

            var @event = new MessageReactionUpdatedEvent(
                request.ChatId,
                request.Reaction.MessageId,
                request.Reaction.Actor.ToDomain(),
                request.Reaction.Emoji
            );

            chat.Fire(@event);
            await session.Events.AppendExclusive(request.ChatId, cancellationToken, @event);
            await session.SaveChangesAsync(cancellationToken);

            var updated = chat.Messages.First(m => m.Id == request.Reaction.MessageId);
            var reactions = updated.Reactions
                .Select(r => new MessageReactionResponse(r.ActorId, r.Emoji))
                .ToArray();
            return (request.Reaction.MessageId, reactions);
        }
    }
}
