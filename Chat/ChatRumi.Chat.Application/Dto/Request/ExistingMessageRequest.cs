namespace ChatRumi.Chat.Application.Dto.Request;

public record ExistingMessageRequest(Guid MessageId, Guid SenderId, string Content, Guid? ReplyOf) : MessageRequest(SenderId, Content, ReplyOf);