using ChatRumi.Feed.Domain.Aggregates;

namespace ChatRumi.Feed.Application.Dtos;

public static class PostExtensions
{
    public static PostDocument ToDocument(this Post post)
    {
        return new PostDocument
        {
            PostId = post.Id,
            Description = post.Description,
            Title = post.Title,
            Creator = post.Creator,
            Attachments = [..post.Attachments.Select(a => a.Id)],
            CreationDate = post.CreationDate,
            Reactions = post.Reactions,
            Shares = post.Shares
        };
    }
}