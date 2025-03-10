using ChatRumi.Kernel;

namespace ChatRumi.Chat.Domain.Events;

public record MessageSentEvent(
    Guid ConversationId,
    Guid SenderId,
    string Content,
    Guid? ReplyOf
) : DomainEvent;