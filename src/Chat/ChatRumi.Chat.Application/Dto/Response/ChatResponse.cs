namespace ChatRumi.Chat.Application.Dto.Response;

public sealed record ChatResponse(
    Guid ChatId,
    ParticipantDto[] Participants,
    MessageResponse[] Messages,
    ParticipantDto Creator,
    DateTimeOffset CreatedDate
);