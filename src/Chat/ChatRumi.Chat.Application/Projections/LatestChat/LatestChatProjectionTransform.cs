using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections.LatestChat;

public class LatestChatProjectionTransform : SingleStreamProjection<LatestChatProjection, Guid>
{
    public LatestChatProjection Create(ChatStartedEvent @event) => new()
    {
        Id = @event.Id,
        Participants = [.. @event.Participants],
        IsGroupChat = @event.IsGroupChat
    };

    public void Apply(MessageSentEvent @event, LatestChatProjection projection)
    {
        projection.LatestMessage = @event.AsLatestMessage();
    }

    public void Apply(ParticipantModifiedEvent @event, LatestChatProjection projection)
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
    }
}