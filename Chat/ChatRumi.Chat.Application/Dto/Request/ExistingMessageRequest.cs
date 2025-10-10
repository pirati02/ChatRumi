using ChatRumi.Chat.Domain.ValueObject;

namespace ChatRumi.Chat.Application.Dto.Request;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed record ExistingMessageRequest(
    Guid MessageId,
    ParticipantDto Sender,
    MessageContent Content,
    Guid? ReplyOf
) : MessageRequest(Sender, Content, ReplyOf);