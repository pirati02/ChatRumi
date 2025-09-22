using ChatRumi.Chat.Application.Dto.Request;

namespace ChatRumi.Chat.Application.Dto.Response;

public record LatestChatResponse(
    Guid ChatId,
    LatestMessageResponse? Message,
    List<ParticipantDto> Participants
);