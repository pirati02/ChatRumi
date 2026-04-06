using ChatRumi.Feed.Application.Dtos;
using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using Xunit;

namespace ChatRumi.Feed.Application.Tests;

public class PostExtensionsTests
{
    [Fact]
    public void ToDocument_MapsAggregateToPostDocument()
    {
        var creator = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            NickName = "ab"
        };
        var attachmentId = Guid.NewGuid();
        var post = Post.Create(
            creator,
            "Description",
            [new Attachment { Id = new AttachmentId(attachmentId) }]);

        PostDocument doc = post.ToDocument();

        Assert.Equal(post.Id, doc.Id);
        Assert.Equal("Description", doc.Description);
        Assert.Equal(creator, doc.Creator);
        Assert.Single(doc.Attachments);
        Assert.Equal(attachmentId, doc.Attachments[0].Guid);
        Assert.Equal(post.CreationDate, doc.CreationDate);
    }
}
