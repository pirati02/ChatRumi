using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Application.Dtos;

public sealed class PostDocument
{
    public Guid Id { get; set; }
    public Participant Creator { get; set; } = null!;
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

    public List<Reaction> Reactions { get; set; } = [];
    public List<Share> Shares { get; set; } = [];
    
    public required string Title { get; set; }
    public required string Description { get; set; }
    
    public List<AttachmentId> Attachments { get;  set; } = [];
}