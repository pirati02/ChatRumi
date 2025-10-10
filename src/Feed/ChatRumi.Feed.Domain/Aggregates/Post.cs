using ChatRumi.Feed.Domain.ValueObject;
using ChatRumi.Kernel;

namespace ChatRumi.Feed.Domain.Aggregates;

public class Post : Aggregate
{
    public Participant Creator { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

    public List<Reaction> Reactions { get; private set; } = [];
    public List<Share> Shares { get; private set; } = [];
    
    public required string Title { get; set; }
    public required string Description { get; set; }
    
    public List<Attachment> Attachments { get; private set; } = [];

    public static Post Create(Participant creator, string title, string description, IEnumerable<Attachment> attachments)
    {
        return new Post
        {
            Creator = creator,
            Title = title,
            Description = description,
            Attachments = [..attachments]
        };
    }
}