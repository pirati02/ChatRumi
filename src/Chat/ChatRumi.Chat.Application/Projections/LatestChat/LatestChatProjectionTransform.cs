using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections.LatestChat;

public class LatestChatProjectionTransform : SingleStreamProjection<LatestChatProjection>
{
    public LatestChatProjectionTransform()
    {
        ProjectEvent<ChatStartedEvent>((projection, @event) =>
        {
            projection.Id = @event.Id;
            projection.Participants = @event.Participants;
            projection.IsGroupChat = @event.IsGroupChat;
        });

        ProjectEvent<MessageSentEvent>((projection, @event) =>
        {
            projection.LatestMessage = @event.AsLatestMessage();
        });
    }
}