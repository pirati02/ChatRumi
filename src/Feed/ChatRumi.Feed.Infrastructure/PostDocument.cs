using ChatRumi.Feed.Domain;
using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Infrastructure;

public class PostDocument
{
    public Participant Creator { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

    public List<Reaction> Likes { get; private set; } = [];
    public List<Share> Shares { get; private set; } = [];
    
    public required string Title { get; set; }
    public required string Description { get; set; }
    
    public List<AttachmentId> Attachments { get; private set; } = [];
}