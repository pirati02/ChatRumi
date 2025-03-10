using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections;

public sealed record ExistingConversationProjection
{
    public Guid ConversationId { get; set; }
    public Guid[] ParticipantIds { get; set; } = [];

    public bool FindParticipants(Guid[] participantIds)
    {
        return ParticipantIds.Except(participantIds).Count() == 1;
    }
}

public class ExistingConversationProjectionTransform : SingleStreamProjection<ExistingConversationProjection>
{
    public ExistingConversationProjectionTransform()
    {
        ProjectEvent<ConversationStartedEvent>((conversationProjection, @event) =>
        {
            conversationProjection.ConversationId = @event.Id;
            conversationProjection.ParticipantIds = @event.ParticipantIds;
        });
    }
}