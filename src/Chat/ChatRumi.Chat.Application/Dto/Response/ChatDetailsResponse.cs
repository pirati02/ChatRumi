using ChatRumi.Chat.Application.Dto.Request;

namespace ChatRumi.Chat.Application.Dto.Response;

public record ChatDetailsResponse(
    Guid ChatId,
    List<ParticipantDto> Participants
);