namespace ChatRumi.Feed.Application.Dtos;

public sealed class PostDetailsDocument
{
    public required PostDocument Post { get; init; }
    public required List<CommentThreadDocument> Comments { get; init; }
}

public sealed class CommentThreadDocument
{
    public required CommentDocument Comment { get; init; }
    public required List<CommentThreadDocument> Replies { get; init; }
}
