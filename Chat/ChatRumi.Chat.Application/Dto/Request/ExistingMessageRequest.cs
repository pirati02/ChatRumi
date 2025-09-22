namespace ChatRumi.Chat.Application.Dto.Request;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ExistingMessageRequest(
    Guid MessageId,
    ParticipantDto Sender,
    string Content,
    Guid? ReplyOf
) : MessageRequest(Sender, Content, ReplyOf);