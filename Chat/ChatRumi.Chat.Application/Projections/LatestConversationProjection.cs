using ChatRumi.Chat.Domain.Aggregates;
using ChatRumi.Chat.Domain.Events;
using Marten;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections;

public sealed record LatestConversationProjection
{
    public Guid Id { get; set; }
    public Guid ParticipantId1 { get; set; }
    public Guid ParticipantId2 { get; set; }
    public LatestMessage? LatestMessage { get; set; }
}

public class LatestConversationProjectionTransform : SingleStreamProjection<LatestConversationProjection>
{
    public LatestConversationProjectionTransform()
    {
        ProjectEvent<ConversationStartedEvent>((projection, @event) =>
        {
            projection.Id = @event.Id;
            projection.ParticipantId1 = @event.ParticipantId1;
            projection.ParticipantId2 = @event.ParticipantId2;
        });

        ProjectEvent<MessageSentEvent>((projection, @event) =>
        {
            projection.LatestMessage = @event.AsLatestMessage();
        });
    }
}

public static class LatestConversationProjectionExtensions
{
    public static Task<IReadOnlyList<LatestConversationProjection>> TryGetTop10LatestConversationsAsync(
        this IDocumentSession session,
        Guid participantId,
        Guid[] otherParticipantIds,
        CancellationToken cancellationToken
    )
    {
        
        return session.Query<LatestConversationProjection>()
            .Where(
                p => (p.ParticipantId1 == participantId && otherParticipantIds.Contains(p.ParticipantId2)) ||
                     p.ParticipantId2 == participantId && otherParticipantIds.Contains(p.ParticipantId1)
            )
            .ToListAsync(token: cancellationToken);
    }
}