using ChatRumi.Chat.Domain.Events;
using Marten.Events.Aggregation;

namespace ChatRumi.Chat.Application.Projections.ExistingChat;

public class ExistingChatProjectionTransform : SingleStreamProjection<ExistingChatProjection, Guid>
{
    public ExistingChatProjectionTransform()
    {
        ProjectEvent<ChatStartedEvent>((chatProjection, @event) =>
        {
            chatProjection.Id = @event.Id;
            chatProjection.ParticipantsHash = string.Join("|", @event.Participants
                .OrderBy(a => a.Id)
                .Select(a => a.Id.ToString("N")));
            chatProjection.IsGroupChat = @event.IsGroupChat;
        });
    }
}