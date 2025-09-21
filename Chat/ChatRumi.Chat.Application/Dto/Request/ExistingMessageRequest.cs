namespace ChatRumi.Chat.Application.Dto.Request;

public record ExistingMessageRequest(Guid MessageId, Guid SenderId, Guid ReceiverId, string Content, Guid? ReplyOf) : MessageRequest(SenderId, ReceiverId, Content, ReplyOf);