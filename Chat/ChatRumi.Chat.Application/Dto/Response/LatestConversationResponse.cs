namespace ChatRumi.Chat.Application.Dto.Response;

public record LatestConversationResponse(
    Guid ConversationId,
    LatestMessageResponse? Message,
    Guid[] ParticipantIds
);