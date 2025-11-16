using ChatRumi.Chat.Application.Hubs;
using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.AccountSync;

public class ChatHubContextProxy : IChatHubContextProxy
{
    public Task MessageStateUpdated(IEnumerable<string> connectionIds, Guid messageId, MessageStatus messageStatus)
    {
        //throw new NotImplementedException();
        return Task.CompletedTask;
    }
}