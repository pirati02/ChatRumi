namespace ChatRumi.Feed.Domain.ValueObject;

public sealed class Share
{
    public Participant Actor { get; set; } = null!;
    public required string ResharedTitle { get; set; }
}