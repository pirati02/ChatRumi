using ChatRumi.Feed.Domain.ValueObject;

namespace ChatRumi.Feed.Application.Dtos;

public sealed class CommentDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PostId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public Participant Creator { get; init; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UtcNow;
    public List<Reaction> Reactions { get; set; } = [];
    public DateTimeOffset? LastEditedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
