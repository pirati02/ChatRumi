using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections.ExistingChat;

public class ExistingChatProjectionTransform : SingleStreamProjection<ExistingChatProjection, Guid>
{
    public ExistingChatProjection Create(ChatStartedEvent @event) => new()
    {
        Id = @event.Id,
        ParticipantsHash = string.Join("|", @event.Participants
            .OrderBy(a => a.Id)
            .Select(a => a.Id.ToString("N"))),
        IsGroupChat = @event.IsGroupChat
    };
}