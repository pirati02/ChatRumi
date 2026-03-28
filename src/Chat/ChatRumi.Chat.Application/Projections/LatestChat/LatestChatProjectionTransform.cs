using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections.LatestChat;

public class LatestChatProjectionTransform : SingleStreamProjection<LatestChatProjection, Guid>
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

        ProjectEvent<ParticipantModifiedEvent>((projection, @event) =>
        {
            var existing = projection.Participants.FirstOrDefault(p => p.Id == @event.ParticipantId);
            if (existing is null)
                return;

            var updated = existing with
            {
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                NickName = @event.UserName
            };

            var index = projection.Participants.IndexOf(existing);
            if (index >= 0)
            {
                projection.Participants[index] = updated;
            }
        });
    }
}