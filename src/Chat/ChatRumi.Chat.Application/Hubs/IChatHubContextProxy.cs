using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Hubs;

public interface IChatHubContextProxy
{
    Task MessageStateUpdated(IEnumerable<string> connectionIds, Guid messageId, MessageStatus messageStatus);
}