namespace ChatRumi.Chat.Domain.Aggregates;

public record Participant
{
    public required Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? NickName { get; set; }
}