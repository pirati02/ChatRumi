namespace ChatRumi.Chat.Domain.Aggregates;

public class LatestMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ConversationId { get; set; }
    public Guid ParticipantId { get; set; }
    public string? Content { get; set; }
}