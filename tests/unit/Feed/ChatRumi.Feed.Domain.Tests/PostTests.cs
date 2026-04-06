using ChatRumi.Feed.Domain.Aggregates;
using ChatRumi.Feed.Domain.ValueObject;
using Xunit;

namespace ChatRumi.Feed.Domain.Tests;

public class PostTests
{
    [Fact]
    public void Create_AssignsCreatorDescriptionAndAttachments()
    {
        var creator = new Participant
        {
            Id = Guid.NewGuid(),
            FirstName = "A",
            LastName = "B",
            NickName = "ab"
        };
        var attachment = new Attachment { Id = new AttachmentId(Guid.NewGuid()) };

        var post = Post.Create(creator, "Body", [attachment]);

        Assert.Equal(creator, post.Creator);
        Assert.Equal("Body", post.Description);
        Assert.Single(post.Attachments);
        Assert.Equal(attachment.Id, post.Attachments[0].Id);
    }
}
