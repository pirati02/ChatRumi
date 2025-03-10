using ChatRumi.Chat.Domain.Events;
using Marten;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections;

public sealed record ExistingConversationProjection
{
    public Guid Id { get; set; }
    public Guid ParticipantId1 { get; set; }
    public Guid ParticipantId2 { get; set; }
}

public class ExistingConversationProjectionTransform : SingleStreamProjection<ExistingConversationProjection>
{
    public ExistingConversationProjectionTransform()
    {
        ProjectEvent<ConversationStartedEvent>((conversationProjection, @event) =>
        {
            conversationProjection.Id = @event.Id;
            conversationProjection.ParticipantId1 = @event.ParticipantId1;
            conversationProjection.ParticipantId2 = @event.ParticipantId2;
        });
    }
}

public static class ExistingConversationProjectionExtensions
{
    public static Task<ExistingConversationProjection?> TryGetExistingConversation(
        this IDocumentSession session,
        Guid participantId1,
        Guid participantId2,
        CancellationToken cancellationToken
    )
    {
        return session.Query<ExistingConversationProjection>()
            .FirstOrDefaultAsync(
                p => (p.ParticipantId1 == participantId1 && p.ParticipantId2 == participantId2) ||
                     (p.ParticipantId1 == participantId2 && p.ParticipantId2 == participantId1),
                token: cancellationToken
            );
    }
}