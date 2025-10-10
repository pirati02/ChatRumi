namespace ChatRumi.Feed.Domain.ValueObject;

public sealed class Reaction
{
    public Participant Actor { get; set; } = null!;
    public ReactionType ReactionType { get; set; }
}

public enum ReactionType
{
    Like,
    Heart,
    Laugh,
    Wow,
    Sad,
    Angry,
    Care
}

